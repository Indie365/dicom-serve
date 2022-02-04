﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Dicom.Api.Extensions;
using Microsoft.Health.Dicom.Api.Features.Filters;
using Microsoft.Health.Dicom.Api.Features.Routing;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.Audit;
using Microsoft.Health.Dicom.Core.Web;

namespace Microsoft.Health.Dicom.Api.Controllers
{
    public partial class WorkitemController
    {
        /// <summary>
        /// RequestUPSCancellation
        /// This action requests the cancellation of a UPS Instance managed by the Origin-Server.
        /// It corresponds to the UPS DIMSE N-ACTION operation "Request UPS Cancel".
        /// </summary>
        /// <remarks>
        /// This resource records a request that the specified UPS Instance be canceled.
        /// 
        /// This transaction allows a user agent that does not own a Workitem to request that it be canceled.
        /// It corresponds to the UPS DIMSE N-ACTION operation "Request UPS Cancel". See Section CC.2.2 in PS3.4 .
        /// 
        /// To cancel a Workitem that the user agent owns, i.e., that is in the IN PROGRESS state,
        /// the user agent uses the Change Workitem State transaction as described in Section 11.7.
        /// 
        /// </remarks>
        /// <param name="workitemInstanceUid">The workitem Uid</param>
        /// <returns></returns>
        [AcceptContentFilter(new[] { KnownContentTypes.ApplicationJson }, allowSingle: true, allowMultiple: false)]
        [Produces(KnownContentTypes.ApplicationJson)]
        [Consumes(KnownContentTypes.ApplicationJson)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotAcceptable)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.UnsupportedMediaType)]
        [HttpPost]
        [VersionedPartitionRoute(KnownRoutes.CancelWorkitemInstancesRoute, Name = KnownRouteNames.VersionedPartitionCancelWorkitemInstance)]
        [PartitionRoute(KnownRoutes.CancelWorkitemInstancesRoute, Name = KnownRouteNames.PartitionedCancelWorkitemInstance)]
        [VersionedRoute(KnownRoutes.CancelWorkitemInstancesRoute, Name = KnownRouteNames.VersionedCancelWorkitemInstance)]
        [Route(KnownRoutes.CancelWorkitemInstancesRoute, Name = KnownRouteNames.CancelWorkitemInstance)]
        [AuditEventType(AuditEventSubType.CancelWorkitem)]
        public async Task<IActionResult> CancelAsync(string workitemInstanceUid)
        {
            long fileSize = Request.ContentLength ?? 0;

            _logger.LogInformation("DICOM Web Cancel Workitem Transaction request received, with Workitem instance UID {WorkitemInstanceUid}, and file size of {FileSize} bytes",
                workitemInstanceUid ?? string.Empty,
                fileSize);

            var response = await _mediator.CancelWorkitemAsync(
                    Request.Body,
                    Request.ContentType,
                    workitemInstanceUid,
                    HttpContext.RequestAborted)
                .ConfigureAwait(false);

            return StatusCode((int)response.Status.ToHttpStatusCodeForCancel(), response.Message);
        }
    }
}