﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using EnsureThat;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Common;

/// <summary>
/// Represents a Dicom server for end-to-end testing.
/// </summary>
public abstract class TestDicomWebServer : IDisposable
{
    protected TestDicomWebServer(Uri baseAddress)
        => BaseAddress = EnsureArg.IsNotNull(baseAddress, nameof(baseAddress));

    public Uri BaseAddress { get; }

    public abstract HttpMessageHandler CreateMessageHandler();

    protected virtual void Dispose(bool disposing)
    { }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
