﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.DicomCast.Core.Features.Worker;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.DicomCast.Core.UnitTests.Features.Worker;

public class DicomCastWorkerTests
{
    private const int DefaultNumberOfInvocations = 2;

    private readonly DicomCastWorkerConfiguration _dicomCastWorkerConfiguration = new DicomCastWorkerConfiguration();
    private readonly IChangeFeedProcessor _changeFeedProcessor;
    private readonly IHostApplicationLifetime _hostApplication;
    private readonly DicomCastWorker _dicomCastWorker;
    private readonly IFhirService _fhirService;
    private readonly TelemetryClient _telemetryClient;

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly CancellationToken _cancellationToken;

    public DicomCastWorkerTests()
    {
        _cancellationToken = _cancellationTokenSource.Token;

        _dicomCastWorkerConfiguration.PollInterval = TimeSpan.Zero;

        _changeFeedProcessor = Substitute.For<IChangeFeedProcessor>();

        _hostApplication = Substitute.For<IHostApplicationLifetime>();

        _fhirService = Substitute.For<IFhirService>();

        _telemetryClient = new TelemetryClient(new TelemetryConfiguration()
        {
            TelemetryChannel = Substitute.For<ITelemetryChannel>(),
        });

        _dicomCastWorker = new DicomCastWorker(
            Options.Create(_dicomCastWorkerConfiguration),
            _changeFeedProcessor,
            NullLogger<DicomCastWorker>.Instance,
            _hostApplication,
            _fhirService,
            _telemetryClient);
    }

    [Fact]
    public async Task GivenWorkerIsBeingCanceled_WhenExecuting_ThenWorkerShouldBeCancelled()
    {
        int invocationCount = 0;

        _changeFeedProcessor.When(processor => processor.ProcessAsync(_dicomCastWorkerConfiguration.PollIntervalDuringCatchup, _cancellationToken))
            .Do(_ =>
            {
                if (invocationCount++ == DefaultNumberOfInvocations)
                {
                    _cancellationTokenSource.Cancel();

                    throw new TaskCanceledException();
                }
            });

        await _dicomCastWorker.ExecuteAsync(_cancellationToken);

        await _changeFeedProcessor.Received(invocationCount).ProcessAsync(_dicomCastWorkerConfiguration.PollIntervalDuringCatchup, _cancellationToken);
    }

    [Fact]
    public async Task GivenWorkerIsBeingCanceled_WhenExecutingAndFailed_ThenProperMetricsisLogged()
    {
        _changeFeedProcessor.When(processor => processor.ProcessAsync(_dicomCastWorkerConfiguration.PollIntervalDuringCatchup, _cancellationToken))
            .Do(_ =>
            {
                throw new TaskCanceledException();
            });

        await _dicomCastWorker.ExecuteAsync(_cancellationToken);

        Assert.True(_telemetryClient.GetMetric(Constants.CastingFailedForOtherReasons).SeriesCount == 1);
    }

    [Fact(Skip = "Flaky test, bug: https://microsofthealth.visualstudio.com/Health/_boards/board/t/Medical%20Imaging/Stories/?workitem=78349")]
    public async Task GivenWorker_WhenExecuting_ThenPollIntervalShouldBeHonored()
    {
        var pollInterval = TimeSpan.FromMilliseconds(50);

        _dicomCastWorkerConfiguration.PollInterval = pollInterval;

        int invocationCount = 0;

        var stopwatch = new Stopwatch();

        _changeFeedProcessor.When(processor => processor.ProcessAsync(_dicomCastWorkerConfiguration.PollIntervalDuringCatchup, _cancellationToken))
            .Do(_ =>
            {
                if (invocationCount++ == 0)
                {
                    stopwatch.Start();
                }
                else
                {
                    stopwatch.Stop();

                    _cancellationTokenSource.Cancel();
                }
            });

        await _dicomCastWorker.ExecuteAsync(_cancellationToken);

        Assert.True(stopwatch.ElapsedMilliseconds >= pollInterval.TotalMilliseconds);
    }
}
