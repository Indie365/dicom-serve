﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using DicomUploaderFunction.Configuration;
using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client.Authentication;
using Microsoft.Health.Client.Extensions;
using Microsoft.Health.Dicom.Client;

[assembly: FunctionsStartup(typeof(DicomUploaderFunction.Startup))]

namespace DicomUploaderFunction;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        EnsureArg.IsNotNull(builder, nameof(builder));

        IConfiguration configuration = builder.GetContext().Configuration;
        IConfigurationSection dicomWebConfigurationSection = configuration.GetSection(DicomOptions.SectionName);

        builder.Services
            .Configure<DicomOptions>(dicomWebConfigurationSection)
            .AddHttpClient<IDicomWebClient, DicomWebClient>(
                (sp, client) =>
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    client.BaseAddress = sp.GetRequiredService<IOptions<DicomOptions>>().Value.Endpoint;
                })
            .AddAuthenticationHandler(dicomWebConfigurationSection.GetSection(AuthenticationOptions.SectionName));
    }
}
