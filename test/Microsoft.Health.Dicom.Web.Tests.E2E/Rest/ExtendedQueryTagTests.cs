﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;
using Microsoft.Health.Dicom.Client.Models;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Tests.Common;
using Microsoft.Health.Dicom.Web.Tests.E2E.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Web.Tests.E2E.Rest
{
    public class ExtendedQueryTagTests : IClassFixture<WebJobsIntegrationTestFixture<Startup>>, IAsyncLifetime
    {
        private const string ErroneousDicomAttributesHeader = "erroneous-dicom-attributes";
        private readonly IDicomWebClient _client;
        private readonly DicomTagsManager _tagManager;
        private readonly DicomInstancesManager _instanceManager;

        public ExtendedQueryTagTests(WebJobsIntegrationTestFixture<Startup> fixture)
        {
            _client = EnsureArg.IsNotNull(fixture, nameof(fixture)).GetDicomWebClient();
            _tagManager = new DicomTagsManager(_client);
            _instanceManager = new DicomInstancesManager(_client);
        }

        [Fact]
        [Trait("Category", "bvt")]
        public async Task GivenExtendedQueryTag_WhenReindexing_ThenShouldSucceed()
        {
            DicomTag ageTag = DicomTag.PatientAge;
            DicomTag filmTag = DicomTag.NumberOfFilms;

            // Try to delete these extended query tags.
            await CleanupExtendedQueryTag(ageTag);
            await CleanupExtendedQueryTag(filmTag);

            // Define DICOM files
            DicomDataset instance1 = Samples.CreateRandomInstanceDataset();
            instance1.Add(ageTag, "048Y");
            instance1.Add(filmTag, "12");

            DicomDataset instance2 = Samples.CreateRandomInstanceDataset();
            instance2.Add(ageTag, "010W");
            instance2.Add(filmTag, "03");

            // Upload files
            Assert.True((await _instanceManager.StoreAsync(new DicomFile(instance1))).IsSuccessStatusCode);
            Assert.True((await _instanceManager.StoreAsync(new DicomFile(instance2))).IsSuccessStatusCode);

            // Add extended query tag
            OperationStatus operation = await _tagManager.AddTagsAsync(
                new AddExtendedQueryTagEntry[]
                {
                    new AddExtendedQueryTagEntry { Path = ageTag.GetPath(), VR = ageTag.GetDefaultVR().Code, Level = QueryTagLevel.Study },
                    new AddExtendedQueryTagEntry { Path = filmTag.GetPath(), VR = filmTag.GetDefaultVR().Code, Level = QueryTagLevel.Study },
                });
            Assert.Equal(OperationRuntimeStatus.Completed, operation.Status);

            // Check specific tag
            DicomWebResponse<GetExtendedQueryTagEntry> getResponse;
            GetExtendedQueryTagEntry entry;

            getResponse = await _client.GetExtendedQueryTagAsync(ageTag.GetPath());
            entry = await getResponse.GetValueAsync();
            Assert.Null(entry.Errors);
            Assert.Equal(QueryStatus.Enabled, entry.QueryStatus);

            getResponse = await _client.GetExtendedQueryTagAsync(filmTag.GetPath());
            entry = await getResponse.GetValueAsync();
            Assert.Null(entry.Errors);
            Assert.Equal(QueryStatus.Enabled, entry.QueryStatus);

            // Query multiple tags
            // Note: We don't necessarily need to check the tags are the above ones, as another test may have added ones beforehand
            var multipleTags = await _tagManager.GetTagsAsync(2, 0);
            Assert.Equal(2, multipleTags.Count);

            Assert.Equal(multipleTags[0].Path, (await _tagManager.GetTagsAsync(1, 0)).Single().Path);
            Assert.Equal(multipleTags[1].Path, (await _tagManager.GetTagsAsync(1, 1)).Single().Path);

            // QIDO
            DicomWebAsyncEnumerableResponse<DicomDataset> queryResponse = await _client.QueryInstancesAsync($"{filmTag.GetPath()}=0003");
            DicomDataset[] instances = await queryResponse.ToArrayAsync();
            Assert.Contains(instances, instance => instance.ToInstanceIdentifier().Equals(instance2.ToInstanceIdentifier()));
        }

        [Fact]
        [Trait("Category", "bvt")]
        public async Task GivenExtendedQueryTagWithErrors_WhenReindexing_ThenShouldSucceedWithErrors()
        {
            // Define tags
            DicomTag tag = DicomTag.PatientAge;
            string tagValue = "053Y";

            // Try to delete this extended query tag if it exists.
            await CleanupExtendedQueryTag(tag);

            // Define DICOM files
            DicomDataset instance1 = Samples.CreateRandomInstanceDataset();
            DicomDataset instance2 = Samples.CreateRandomInstanceDataset();
            DicomDataset instance3 = Samples.CreateRandomInstanceDataset();

            // Annotate files
            // (Disable Auto-validate)
            instance1.NotValidated();
            instance2.NotValidated();

            instance1.Add(tag, "foobar");
            instance2.Add(tag, "invalid");
            instance3.Add(tag, tagValue);

            // Upload files (with a few errors)
            await _instanceManager.StoreAsync(new DicomFile(instance1));
            await _instanceManager.StoreAsync(new DicomFile(instance2));
            await _instanceManager.StoreAsync(new DicomFile(instance3));

            // Add extended query tags
            var operationStatus = await _tagManager.AddTagsAsync(
                new AddExtendedQueryTagEntry[]
                {
                    new AddExtendedQueryTagEntry { Path = tag.GetPath(), VR = tag.GetDefaultVR().Code, Level = QueryTagLevel.Instance },
                });
            Assert.Equal(OperationRuntimeStatus.Completed, operationStatus.Status);

            // Check specific tag
            GetExtendedQueryTagEntry actual = await _tagManager.GetTagAsync(tag.GetPath());
            Assert.Equal(tag.GetPath(), actual.Path);
            Assert.Equal(2, actual.Errors.Count);
            // It should be disabled by default
            Assert.Equal(QueryStatus.Disabled, actual.QueryStatus);

            // Verify Errors
            var errors = await _tagManager.GetTagErrorsAsync(tag.GetPath(), 2, 0);
            Assert.Equal(2, errors.Count);

            Assert.Equal(errors[0].ErrorMessage, (await _tagManager.GetTagErrorsAsync(tag.GetPath(), 1, 0)).Single().ErrorMessage);
            Assert.Equal(errors[1].ErrorMessage, (await _tagManager.GetTagErrorsAsync(tag.GetPath(), 1, 1)).Single().ErrorMessage);

            // Check that the reference API returns the same values
            var sameErrors = await _client.ResolveReferenceAsync(actual.Errors);
            Assert.Equal(errors.Select(x => x.ErrorMessage), (await sameErrors.GetValueAsync()).Select(x => x.ErrorMessage));

            var exception = await Assert.ThrowsAsync<DicomWebException>(() => _client.QueryInstancesAsync($"{tag.GetPath()}={tagValue}"));
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);

            // Enable QIDO on Tag
            actual = await _tagManager.UpdateExtendedQueryTagAsync(tag.GetPath(), new UpdateExtendedQueryTagEntry() { QueryStatus = QueryStatus.Enabled });
            Assert.Equal(QueryStatus.Enabled, actual.QueryStatus);

            var response = await _client.QueryInstancesAsync($"{tag.GetPath()}={tagValue}");

            Assert.True(response.ResponseHeaders.Contains(ErroneousDicomAttributesHeader));
            var values = response.ResponseHeaders.GetValues(ErroneousDicomAttributesHeader);
            Assert.Single(values);
            Assert.Equal(tag.GetPath(), values.First());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify result
            DicomDataset[] instances = await response.ToArrayAsync();
            Assert.Contains(instances, instance => instance.ToInstanceIdentifier().Equals(instance3.ToInstanceIdentifier()));
        }

        [Theory]
        [MemberData(nameof(GetRequestBodyWithMissingProperty))]
        public async Task GivenMissingPropertyInRequestBody_WhenCallingPostAsync_ThenShouldThrowException(string requestBody, string missingProperty)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{DicomApiVersions.Latest}/extendedquerytags");
            {
                request.Content = new StringContent(requestBody);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await _client.HttpClient.SendAsync(request, default(CancellationToken))
                .ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(string.Format("The field '[0].{0}' in request body is invalid: The Dicom Tag Property {0} must be specified and must not be null, empty or whitespace", missingProperty), response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public async Task GivenInvalidTagLevelInRequestBody_WhenCallingPostAync_ThenShouldThrowException()
        {
            string requestBody = "[{\"Path\":\"00100040\",\"Level\":\"Studys\"}]";
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{DicomApiVersions.Latest}/extendedquerytags");
            {
                request.Content = new StringContent(requestBody);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(DicomWebConstants.ApplicationJsonMediaType);
            }

            HttpResponseMessage response = await _client.HttpClient.SendAsync(request, default(CancellationToken)).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("The field '$[0].Level' in request body is invalid: Expected value 'Studys' to be one of the following values: ['Instance', 'Series', 'Study']", response.Content.ReadAsStringAsync().Result);
        }

        private async Task CleanupExtendedQueryTag(DicomTag tag)
        {
            // Try to delete this extended query tag.
            try
            {
                await _tagManager.DeleteExtendedQueryTagAsync(tag.GetPath());
            }
            catch (DicomWebException)
            {
            }
        }

        public static IEnumerable<object[]> GetRequestBodyWithMissingProperty
        {
            get
            {
                yield return new object[] { "[{\"Path\":\"00100040\"}]", "Level" };
                yield return new object[] { "[{\"Path\":\"\",\"Level\":\"Study\"}]", "Path" };
            }
        }

        public Task InitializeAsync()
            => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _tagManager.DisposeAsync();
            await _instanceManager.DisposeAsync();
        }
    }
}
