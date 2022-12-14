// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Dicom.Core.Features.Common;
using Xunit;

namespace Microsoft.Health.Dicom.Core.UnitTests.Features.Common;

public class ParallelEnumerableTests
{
    [Fact]
    public async Task GivenInvalidInput_WhenSelectingInParallel_ThenThrowArgumentException()
    {
        var input = new List<string> { "1", "2", "3" };

        await Assert.ThrowsAsync<ArgumentNullException>(() => ((List<string>)null).SelectParallel(ParseAsync, new ParallelEnumerationOptions()).ToListAsync().AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => input.SelectParallel((Func<string, CancellationToken, ValueTask<int>>)null, new ParallelEnumerationOptions()).ToListAsync().AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => input.SelectParallel(ParseAsync, null).ToListAsync().AsTask());
    }

    [Fact]
    public async Task GivenNoElements_WhenSelectingInParallel_ThenReturnEmpty()
    {
        var input = Enumerable.Empty<string>();
        Assert.Empty(await input.SelectParallel(ParseAsync, new ParallelEnumerationOptions()).ToListAsync());
    }

    [Fact]
    public async Task GivenOneElement_WhenSelectingInParallel_ThenReturnOneElement()
    {
        var input = new List<string> { "42" };
        Assert.Equal(42, await input.SelectParallel(ParseAsync, new ParallelEnumerationOptions()).SingleAsync());
    }

    [Fact]
    public async Task GivenFewerThenParallelism_WhenSelectingInParallel_ThenReturnAllResults()
    {
        var input = new List<string> { "1", "2", "3" };
        await AssertValuesAsync(
            input.SelectParallel(ParseAsync, new ParallelEnumerationOptions { MaxDegreeOfParallelism = input.Count + 1 }),
            1, 2, 3);
    }

    [Fact]
    public async Task GivenMoreThenParallelism_WhenSelectingInParallel_ThenReturnAllResults()
    {
        var input = new List<string> { "1", "2", "3", "4", "5" };
        await AssertValuesAsync(
            input.SelectParallel(ParseAsync, new ParallelEnumerationOptions { MaxDegreeOfParallelism = input.Count - 2 }),
            1, 2, 3, 4, 5);
    }

    [Fact]
    public async Task GivenSource_WhenEnumeratingMultipleTimes_ThenReturnSameishResults()
    {
        var input = new List<string> { "1", "2", "3", "4", "5" };
        for (int i = 0; i < 5; i++)
        {
            await AssertValuesAsync(
                input.SelectParallel(ParseAsync, new ParallelEnumerationOptions { MaxDegreeOfParallelism = input.Count - 2 }),
                1, 2, 3, 4, 5);
        }
    }

    [Fact]
    public async Task GivenError_WhenSelectingInParallel_ThenRethrowError()
    {
        var input = new List<string> { "1", "foo", "3" };
        await Assert.ThrowsAsync<FormatException>(
            () => input.SelectParallel(ParseAsync, new ParallelEnumerationOptions()).ToListAsync().AsTask());
    }

    [Fact]
    public async Task GivenCancelledToken_WhenSelectingInParallel_ThenThrowException()
    {
        using var tokenSource = new CancellationTokenSource();

        int count = 0;
        var input = new List<string> { "1", "2", "3", "4", "5" };
        await Assert.ThrowsAsync<TaskCanceledException>(
            () => input
                .SelectParallel(
                    (x, t) =>
                    {
                        if (Interlocked.Increment(ref count) == 4)
                            tokenSource.Cancel();

                        return ParseAsync(x, t);
                    },
                    new ParallelEnumerationOptions { MaxDegreeOfParallelism = 2 },
                    tokenSource.Token)
                .ToListAsync()
                .AsTask());
    }

    private static ValueTask<int> ParseAsync(string s, CancellationToken cancellationToken)
        => new ValueTask<int>(Task.Run(() => int.Parse(s, CultureInfo.InvariantCulture), cancellationToken));

    private static async ValueTask AssertValuesAsync<T>(IAsyncEnumerable<T> actual, params T[] expected)
    {
        HashSet<T> set = await actual.ToHashSetAsync();

        Assert.Equal(expected.Length, set.Count);
        foreach (T e in expected)
        {
            Assert.True(set.Remove(e));
        }
    }
}
