// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Reporting;

using System.Collections.Generic;
using System.Linq;

using Api;

/// <summary>
///     Collects and categorizes test reports produced during execution.
/// </summary>
/// <remarks>
///     Stores all reports in insertion order and exposes filtered views for failures,
///     errors, and warnings used by the test execution pipeline.
/// </remarks>
public sealed class TestReportCollector
{
    /// <summary>
    ///     Gets the collected test reports in insertion order.
    /// </summary>
#pragma warning disable CA1002
    public List<ITestReport> Reports { get; } = [];
#pragma warning restore CA1002

    /// <summary>
    ///     Gets all collected reports marked as failures.
    /// </summary>
    public IEnumerable<ITestReport> Failures => Reports.Where(r => r.IsFailure);

    /// <summary>
    ///     Gets all collected reports marked as errors.
    /// </summary>
    public IEnumerable<ITestReport> Errors => Reports.Where(r => r.IsError);

    /// <summary>
    ///     Gets all collected reports marked as warnings.
    /// </summary>
    public IEnumerable<ITestReport> Warnings => Reports.Where(r => r.IsWarning);

    /// <summary>
    ///     Adds a report to the end of the collection.
    /// </summary>
    /// <param name="report">The report to add.</param>
    public void Consume(ITestReport report) => Reports.Add(report);

    /// <summary>
    ///     Inserts a report at the beginning of the collection.
    /// </summary>
    /// <param name="report">The report to insert.</param>
    public void PushFront(ITestReport report) => Reports.Insert(0, report);

    /// <summary>
    ///     Removes all collected reports.
    /// </summary>
    public void Clear() => Reports.Clear();
}
