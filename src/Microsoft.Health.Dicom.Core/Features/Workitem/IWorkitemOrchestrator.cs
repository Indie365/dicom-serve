﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using FellowOakDicom;

namespace Microsoft.Health.Dicom.Core.Features.Workitem
{
    /// <summary>
    /// Provides functionality to orchestrate the adding of a UPS-RS workitem.
    /// </summary>
    public interface IWorkitemOrchestrator
    {
        /// <summary>
        /// Asynchronously orchestrate the adding of a UPS-RS workitem.
        /// </summary>
        /// <param name="dataset">The workitem dataset to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous orchestration of the adding operation.</returns>
        Task AddWorkitemAsync(DicomDataset dataset, CancellationToken cancellationToken);
    }
}