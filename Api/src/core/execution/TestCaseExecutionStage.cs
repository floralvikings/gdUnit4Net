// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Reflection;
using System.Threading.Tasks;

using Asserts;

using Reporting;

using TestExtensions;

using static Api.ReportType;

internal sealed class TestCaseExecutionStage : ExecutionStage<TestCaseAttribute>
{
    public TestCaseExecutionStage(string name, TestCase testCase, TestCaseAttribute stageAttribute)
        : base(name, testCase.MethodInfo, stageAttribute)
    {
    }

    public override async Task Execute(ExecutionContext context)
    {
        context.MemoryPool.SetActive(StageName, true);

        await base
            .Execute(context)
            .ConfigureAwait(true);

        await context.MemoryPool
            .Gc()
            .ConfigureAwait(true);
        if (context.MemoryPool.OrphanCount > 0)
            context.ReportCollector.PushFront(new TestReport(Warning, context.CurrentTestCase?.Line ?? 0, ReportOrphans(context)));
    }

    protected override async Task ExecuteStage(ExecutionContext context)
    {
        var classLevelCallbacks = context.TestSuite.Instance.GetType().GetCustomAttributes<RegisterGdUnitExtensionAttribute>()
            .Where(attr => typeof(ITestCaseCallback).IsAssignableFrom(attr.ExtensionType))
            .Select(attr => (ITestCaseCallback)Activator.CreateInstance(attr.ExtensionType)!);
        var methodLevelCallbacks = context.CurrentTestCase?.MethodInfo.GetCustomAttributes<RegisterGdUnitExtensionAttribute>()
            .Where(attr => typeof(ITestCaseCallback).IsAssignableFrom(attr.ExtensionType))
            .Select(attr => (ITestCaseCallback)Activator.CreateInstance(attr.ExtensionType)!) ?? [];
        var callbacks = classLevelCallbacks.Concat(methodLevelCallbacks);

        foreach (var callback in callbacks)
            callback.Execute(context);

        await base.ExecuteStage(context).ConfigureAwait(true);
    }

    private static string ReportOrphans(ExecutionContext context) =>
        $"""
         {AssertFailures.FormatValue("WARNING:", AssertFailures.WARN_COLOR, false)}
             Detected <{context.MemoryPool.OrphanCount}> orphan nodes during test execution!
         """;
}
