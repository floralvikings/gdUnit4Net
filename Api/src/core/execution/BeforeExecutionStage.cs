// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Reflection;
using System.Threading.Tasks;

using TestExtensions;

internal class BeforeExecutionStage : ExecutionStage<BeforeAttribute>
{
    public BeforeExecutionStage(TestSuite testSuite)
        : base("Before", testSuite.Instance.GetType())
    {
    }

    public override async Task Execute(ExecutionContext context)
    {
        context.MemoryPool.SetActive(StageName, true);
        var callbacks = context.TestSuite.Instance.GetType()
            .GetCustomAttributes<RegisterGdUnitExtensionAttribute>()
            .Where(attr => typeof(IBeforeCallback).IsAssignableFrom(attr.ExtensionType))
            .Select(attr => (IBeforeCallback)Activator.CreateInstance(attr.ExtensionType)!);
        foreach (var callback in callbacks)
            callback.Before(context);

        await base
            .Execute(context)
            .ConfigureAwait(true);
        context.FireBeforeEvent();
        context.ReportCollector.Clear();
        context.MemoryPool.StopMonitoring();
    }
}
