// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using Azure.Identity;
using EnsureThat;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.DicomCast.Core.Configurations;
using Microsoft.Health.DicomCast.Core.Features.Fhir;
using Microsoft.Health.Fhir.Client;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.DicomCast.Core.Features.Worker;

/// <summary>
/// The worker for DicomCast.
/// </summary>
public class DicomCastWorker : IDicomCastWorker
{
    private static readonly Action<ILogger, Exception> LogWorkerStartingDelegate =
        LoggerMessage.Define(
            LogLevel.Information,
            default,
            $"{typeof(DicomCastWorker)} is starting.");

    private static readonly Action<ILogger, Exception> LogWorkerCancelRequestedDelegate =
        LoggerMessage.Define(
            LogLevel.Information,
            default,
            $"{typeof(DicomCastWorker)} is requested to be stopped.");

    private static readonly Action<ILogger, Exception> LogWorkerExitingDelegate =
        LoggerMessage.Define(
            LogLevel.Information,
            default,
            $"{typeof(DicomCastWorker)} is exiting.");

    private static readonly Action<ILogger, Exception> LogUnhandledExceptionDelegate =
       LoggerMessage.Define(
           LogLevel.Critical,
           default,
           "Unhandled exception.");

    private readonly DicomCastWorkerConfiguration _dicomCastWorkerConfiguration;
    private readonly IChangeFeedProcessor _changeFeedProcessor;
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IFhirService _fhirService;
    private readonly TelemetryClient _telemetryClient;

    public DicomCastWorker(
        IOptions<DicomCastWorkerConfiguration> dicomCastWorkerConfiguration,
        IChangeFeedProcessor changeFeedProcessor,
        ILogger<DicomCastWorker> logger,
        IHostApplicationLifetime hostApplicationLifetime,
        IFhirService fhirService,
        TelemetryClient telemetryClient)
    {
        EnsureArg.IsNotNull(dicomCastWorkerConfiguration?.Value, nameof(dicomCastWorkerConfiguration));
        EnsureArg.IsNotNull(changeFeedProcessor, nameof(changeFeedProcessor));
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(hostApplicationLifetime, nameof(hostApplicationLifetime));
        EnsureArg.IsNotNull(fhirService, nameof(fhirService));
        EnsureArg.IsNotNull(telemetryClient, nameof(telemetryClient));

        _dicomCastWorkerConfiguration = dicomCastWorkerConfiguration.Value;
        _changeFeedProcessor = changeFeedProcessor;
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
        _fhirService = fhirService;
        _telemetryClient = telemetryClient;
    }

    /// <inheritdoc/>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "App shuts down on any error.")]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _fhirService.CheckFhirServiceCapability(cancellationToken);
            LogWorkerStartingDelegate(_logger, null);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _changeFeedProcessor.ProcessAsync(_dicomCastWorkerConfiguration.PollIntervalDuringCatchup, cancellationToken);

                    await Task.Delay(_dicomCastWorkerConfiguration.PollInterval, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Cancel requested.
                    LogWorkerCancelRequestedDelegate(_logger, null);
                    break;
                }
            }

            LogWorkerExitingDelegate(_logger, null);
        }
        catch (Exception ex)
        {
            LogUnhandledExceptionDelegate(_logger, ex);

            if ((ex is FhirException) && ((FhirException)ex).StatusCode == HttpStatusCode.Forbidden)
            {
                _telemetryClient.GetMetric(Constants.CastToFhirForbidden).TrackValue(1);
            }
            else if ((ex is DicomWebException) && ((DicomWebException)ex).StatusCode == HttpStatusCode.Forbidden)
            {
                _telemetryClient.GetMetric(Constants.DicomToCastForbidden).TrackValue(1);
            }
            else if (ex is CredentialUnavailableException)
            {
                _telemetryClient.GetMetric(Constants.CastMIUnavailable).TrackValue(1);
            }
            else
            {
                _telemetryClient.GetMetric(Constants.CastingFailedForOtherReasons).TrackValue(1);
            }

            // Any exception in ExecuteAsync will not shutdown application, call hostApplicationLifetime.StopApplication() to force shutdown.
            // Please refer to .net core issue on github for more details: "Exceptions in BackgroundService ExecuteAsync are (sometimes) hidden" https://github.com/dotnet/extensions/issues/2363
            _hostApplicationLifetime.StopApplication();
        }
        finally
        {
            _telemetryClient.Flush();
        }
    }
}
