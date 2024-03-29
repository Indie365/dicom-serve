﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Health.Dicom.Core.Features.Operations;

internal interface IDicomOperationsResourceStore
{
    IAsyncEnumerable<string> ResolveQueryTagKeysAsync(IReadOnlyCollection<int> keys, CancellationToken cancellationToken = default);
}
