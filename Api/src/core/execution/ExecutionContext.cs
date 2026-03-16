// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

using Api;

using Monitoring;

using Reporting;

/// <summary>
///     Provides the execution state for a test suite or test case run.
/// </summary>
/// <remarks>
///     This context stores the active suite and test case, execution timing, generated reports,
///     disposable resources, and event publishing state used throughout the test execution pipeline.
/// </remarks>
public sealed class ExecutionContext : IDisposable
{
    private int iteration;

    internal ExecutionContext(TestSuite testInstance, IEnumerable<ITestEventListener> eventListeners, bool reportOrphanNodesEnabled, bool isEngineMode)
    {
        Thread.SetData(Thread.GetNamedDataSlot("ExecutionContext"), this);
        MemoryPool = new MemoryPool(reportOrphanNodesEnabled && isEngineMode);
        Stopwatch = new Stopwatch();
        Stopwatch.Start();

        ReportOrphanNodesEnabled = reportOrphanNodesEnabled;
        FailureReporting = true;
        TestSuite = testInstance;
        EventListeners = eventListeners;
        ReportCollector = new TestReportCollector();
        SubExecutionContexts = [];
        Disposables = [];
        FullyQualifiedName = TestSuite.Instance.GetType().FullName!;
        IsEngineMode = isEngineMode;
    }

    internal ExecutionContext(ExecutionContext context, params object?[] methodArguments)
        : this(
            context.TestSuite,
            context.EventListeners,
            context.ReportOrphanNodesEnabled,
            context.IsEngineMode)
    {
        ReportCollector = context.ReportCollector;
        context.SubExecutionContexts.Add(this);
        TestCaseName = context.TestCaseName;
        CurrentTestCase = context.CurrentTestCase;
        MethodArguments = methodArguments;
        IsSkipped = CurrentTestCase?.IsSkipped ?? false;
        CurrentIteration = CurrentTestCase?.TestCaseAttributes.Count == 1
            ? CurrentTestCase?.TestCaseAttributes.ElementAt(0).Iterations ?? 0
            : 0;
        FullyQualifiedName = TestCase.BuildFullyQualifiedName(TestSuite.Instance.GetType().FullName!, TestCaseName, new TestCaseAttribute(methodArguments));
    }

    // used for dynamic datapoint tests
    internal ExecutionContext(ExecutionContext context, string displayName)
        : this(
            context.TestSuite,
            context.EventListeners,
            context.ReportOrphanNodesEnabled,
            context.IsEngineMode)
    {
        ReportCollector = context.ReportCollector;
        context.SubExecutionContexts.Add(this);
        CurrentTestCase = context.CurrentTestCase;
        IsSkipped = CurrentTestCase?.IsSkipped ?? false;
        CurrentIteration = CurrentTestCase?.TestCaseAttributes.Count == 1
            ? CurrentTestCase?.TestCaseAttributes.ElementAt(0).Iterations ?? 0
            : 0;
        TestCaseName = context.TestCaseName;
        FullyQualifiedName = TestCase.BuildFullyQualifiedName(TestSuite.Instance.GetType().FullName!, displayName, new TestCaseAttribute());
        DisplayName = displayName;
    }

    internal ExecutionContext(ExecutionContext context, TestCase testCase)
        : this(context.TestSuite, context.EventListeners, context.ReportOrphanNodesEnabled, context.IsEngineMode)
    {
        context.SubExecutionContexts.Add(this);
        CurrentTestCase = testCase;
        CurrentIteration = CurrentTestCase?.TestCaseAttributes.Count == 1
            ? CurrentTestCase?.TestCaseAttributes.ElementAt(0).Iterations ?? 0
            : 0;
        IsSkipped = CurrentTestCase?.IsSkipped ?? false;
        TestCaseName = TestCase.BuildDisplayName(testCase.Name, testCase.TestCaseAttribute);
        FullyQualifiedName = TestCase.BuildFullyQualifiedName(TestSuite.Instance.GetType().FullName!, testCase.Name, testCase.TestCaseAttribute);
    }

    /// <summary>
    ///     Gets the execution context associated with the current thread.
    /// </summary>
    public static ExecutionContext? Current
        => Thread.GetData(Thread.GetNamedDataSlot("ExecutionContext")) as ExecutionContext;

    /// <summary>
    ///     Gets a value indicating whether execution is running inside the Godot engine.
    /// </summary>
    public bool IsEngineMode { get; internal set; }

    /// <summary>
    ///     Gets a value indicating whether standard output capture is enabled.
    /// </summary>
    public bool IsCaptureStdOut { get; internal set; } = true;

    /// <summary>
    ///     Gets the display name of the current test case.
    /// </summary>
    public string TestCaseName { get; internal set; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the current test case is skipped.
    /// </summary>
    public bool IsSkipped { get; }

    /// <summary>
    ///     Gets a value indicating whether this execution or any nested execution has failed.
    /// </summary>
    public bool IsFailed => ReportCollector.Failures.Any() || SubExecutionContexts.Any(context => context.IsFailed);

    /// <summary>
    ///     Gets a value indicating whether this execution or any nested execution has reported an error.
    /// </summary>
    public bool IsError => ReportCollector.Errors.Any() || SubExecutionContexts.Any(context => context.IsError);

    /// <summary>
    ///     Gets a value indicating whether this execution or any nested execution has reported a warning.
    /// </summary>
    public bool IsWarning => ReportCollector.Warnings.Any() || SubExecutionContexts.Any(context => context.IsWarning);

    /// <summary>
    ///     Gets the report collector used to aggregate execution reports.
    /// </summary>
    public TestReportCollector ReportCollector { get; }

    /// <summary>
    ///     Gets the active test suite for this execution.
    /// </summary>
    public TestSuite TestSuite { get; }

    /// <summary>
    ///     Gets or sets the current test case being executed.
    /// </summary>
    public TestCase? CurrentTestCase { get; set; }

    /// <summary>
    ///     Gets or sets the method arguments used for the current test invocation.
    /// </summary>
#pragma warning disable CA1819
    public object?[] MethodArguments { get; set; } = [];
#pragma warning restore CA1819

    /// <summary>
    ///     Gets or sets a value indicating whether failure reporting is enabled.
    /// </summary>
    internal bool FailureReporting { get; set; }

    /// <summary>
    ///     Gets the memory pool used to track orphan nodes during execution.
    /// </summary>
    internal MemoryPool MemoryPool { get; }

    /// <summary>
    ///     Gets or sets the current iteration count for repeated test execution.
    /// </summary>
    internal int CurrentIteration
    {
        get => iteration--;
        set => iteration = value;
    }

    private TimeSpan ExecutionTimeout { get; } = TimeSpan.FromSeconds(30);

    private bool ReportOrphanNodesEnabled { get; }

    private Stopwatch Stopwatch { get; }

    private List<IDisposable> Disposables { get; }

    private IEnumerable<ITestEventListener> EventListeners { get; }

    private List<ExecutionContext> SubExecutionContexts { get; }

    private long Duration => Stopwatch.ElapsedMilliseconds;

    private List<ITestReport> CollectReports => ReportCollector.Reports;

    private int SkippedCount => SubExecutionContexts.Count(context => context.IsSkipped);

    private int FailureCount => ReportCollector.Failures.Count();

    private int ErrorCount => ReportCollector.Errors.Count();

    private string FullyQualifiedName { get; }

    private string? DisplayName { get; }

    /// <summary>
    ///     Registers a disposable resource with the current execution context.
    /// </summary>
    /// <param name="disposable">The disposable resource to clean up when the context is disposed.</param>
    public static void RegisterDisposable(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        Current?.Disposables.Add(disposable);
    }

    /// <summary>
    ///     Disposes all registered resources and stops execution time tracking.
    ///     Not meant to be called directly.
    /// </summary>
    public void Dispose()
    {
        Disposables.ForEach(disposable =>
        {
            try
            {
                disposable.Dispose();
            }
            catch (ObjectDisposedException e)
            {
                _ = e;
            }
        });
        Stopwatch.Stop();
    }

    /// <summary>
    ///     Verifies whether the specified exception matches the expected exception declared on the method.
    /// </summary>
    /// <param name="exception">The exception raised during execution, if any.</param>
    /// <param name="mi">The method to inspect for a <see cref="ThrowsExceptionAttribute"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if the method expects the provided exception; otherwise, <see langword="false"/>.
    /// </returns>
    internal bool IsExpectingToFailWithException(Exception? exception, MethodInfo? mi)
    {
        var attribute = mi?.GetCustomAttribute<ThrowsExceptionAttribute>();
        if (attribute == null)
            return false;

        if (exception == null)
            attribute.ThrowExpectingExceptionExpected();

        return attribute.Verify(exception!);
    }

    /// <summary>
    ///     Publishes the before-suite event for this execution context.
    /// </summary>
    internal void FireBeforeEvent() =>
        FireTestEvent(
            TestEvent
                .Before(TestSuite.ResourcePath, TestSuite.Name, TestSuite.TestCaseCount, BuildStatistics(0), CollectReports)
                .WithFullyQualifiedName(FullyQualifiedName));

    /// <summary>
    ///     Publishes the after-suite event for this execution context.
    /// </summary>
    internal void FireAfterEvent() =>
        FireTestEvent(
            TestEvent
                .After(TestSuite.ResourcePath, TestSuite.Name, BuildStatistics(OrphanCount(false)), CollectReports)
                .WithFullyQualifiedName(FullyQualifiedName));

    /// <summary>
    ///     Publishes the before-test event for the current test case.
    /// </summary>
    internal void FireBeforeTestEvent() =>
        FireTestEvent(
            TestEvent
                .BeforeTest(CurrentTestCase!.Id, TestSuite.ResourcePath, TestSuite.Name, TestCaseName)
                .WithFullyQualifiedName(FullyQualifiedName)
                .WithDisplayName(DisplayName));

    /// <summary>
    ///     Publishes the after-test event for the current test case.
    /// </summary>
    internal void FireAfterTestEvent() =>
        FireTestEvent(
            TestEvent
                .AfterTest(CurrentTestCase!.Id, TestSuite.ResourcePath, TestSuite.Name, TestCaseName, BuildStatistics(OrphanCount(true)), CollectReports)
                .WithFullyQualifiedName(FullyQualifiedName)
                .WithDisplayName(DisplayName));

    /// <summary>
    ///     Resolves the execution timeout for the specified test case attribute.
    /// </summary>
    /// <param name="testAttribute">The test case attribute that may define a custom timeout.</param>
    /// <returns>The effective timeout for the test execution.</returns>
    internal TimeSpan GetExecutionTimeout(TestCaseAttribute testAttribute) =>
        testAttribute.Timeout == -1 ? ExecutionTimeout : TimeSpan.FromMilliseconds(testAttribute.Timeout);

    internal void PrintDebug(string name = "")
        => Console.WriteLine($"{name} test context {TestSuite.Name} {TestCaseName} error: {IsError} failed: {IsFailed} skipped: {IsSkipped}");

    private int OrphanCount(bool recursive)
    {
        var orphanCount = MemoryPool.OrphanCount;
        if (recursive)
            orphanCount += SubExecutionContexts.Sum(context => context.MemoryPool.OrphanCount);
        return orphanCount;
    }

    private IDictionary<TestEvent.StatisticKey, object> BuildStatistics(int orphanCount)
        => TestEvent.BuildStatistics(
            orphanCount,
            IsError,
            ErrorCount,
            IsFailed,
            FailureCount,
            IsWarning,
            IsSkipped,
            SkippedCount,
            Duration);

    private void FireTestEvent(TestEvent e) =>
        EventListeners.ToList().ForEach(l => l.PublishEvent(e));
}
