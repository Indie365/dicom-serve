// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json.Serialization;
using EnsureThat;

namespace Microsoft.Health.Dicom.Functions.Export;

/// <summary>
/// Represents the progress made so far much an export operation.
/// </summary>
public readonly struct ExportProgress : IEquatable<ExportProgress>
{
    /// <summary>
    /// Gets the number of DICOM files that have successfully been exported so far.
    /// </summary>
    /// <value>The non-negative number of exported DICOM files.</value>
    public long Exported { get; }

    /// <summary>
    /// Gets the number of DICOM resources that have failed to be exported so far.
    /// </summary>
    /// <value>The non-negative number of DICOM resources that failed to be exported.</value>
    public long Skipped { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportProgress"/> structure based on the specified number of
    /// DICOM files processed by an export operation.
    /// </summary>
    /// <param name="exported">The number of files that were successfully exported.</param>
    /// <param name="skipped">The number of files that failed to be exported.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="exported"/> is less than <c>0</c>.</para>
    /// <para>-or-</para>
    /// <para><paramref name="skipped"/> is less than <c>0</c>.</para>
    /// </exception>
    [JsonConstructor]
    public ExportProgress(long exported, long skipped)
    {
        Exported = EnsureArg.IsGte(exported, 0, nameof(exported));
        Skipped = EnsureArg.IsGte(skipped, 0, nameof(skipped));
    }

    /// <summary>
    /// Returns a new <see cref="ExportProgress"/> that adds the value of the specified <see cref="ExportProgress"/>
    /// to the value of this instance.
    /// </summary>
    /// <param name="other">Another instance of the <see cref="ExportProgress"/> structure.</param>
    /// <returns>
    /// An object whose values are the sums of the <see cref="Exported"/> and <see cref="Skipped"/> properties
    /// represented by this instance and <paramref name="other"/>.
    /// </returns>
    public ExportProgress Add(ExportProgress other)
        => new ExportProgress(Exported + other.Exported, Skipped + other.Skipped);

    /// <summary>
    /// Returns a value indicating whether this instance is equal to a specified object.
    /// </summary>
    /// <param name="obj">The object to compare to this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is an instance of <see cref="ExportProgress"/> and
    /// equals the value of this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object obj)
        => obj is ExportProgress other && Equals(other);

    /// <summary>
    /// Returns a value indicating whether the value of this instance is equal to the value of the
    /// specified <see cref="ExportProgress"/> instance.
    /// </summary>
    /// <param name="other">The object to compare to this instance.</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="other"/> parameter equals the
    /// value of this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(ExportProgress other)
        => Exported == other.Exported && Skipped == other.Skipped;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
        => HashCode.Combine(Exported, Skipped);

    /// <summary>
    /// Returns a new <see cref="ExportProgress"/> that adds the value of the specified <see cref="ExportProgress"/> values.
    /// </summary>
    /// <param name="x">An instance of the <see cref="ExportProgress"/> structure.</param>
    /// <param name="y">Another instance of the <see cref="ExportProgress"/> structure.</param>
    /// <returns>
    /// An object whose values are the sums of the <see cref="Exported"/> and <see cref="Skipped"/> properties
    /// represented by the two parameters.
    /// </returns>
    public static ExportProgress operator +(ExportProgress x, ExportProgress y)
        => x.Add(y);

    /// <summary>
    /// Determines whether two specified instances of <see cref="ExportProgress"/> are equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/>
    /// represent the same <see cref="ExportProgress"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(ExportProgress left, ExportProgress right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two specified instances of <see cref="ExportProgress"/> are not equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left"/> and <paramref name="right"/>
    /// do not represent the same <see cref="ExportProgress"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(ExportProgress left, ExportProgress right)
        => !left.Equals(right);
}
