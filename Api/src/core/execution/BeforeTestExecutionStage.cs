// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Reflection;
using System.Threading.Tasks;

using TestExtensions;

internal class BeforeTestExecutionStage : ExecutionStage<BeforeTestAttribute>
{
    public BeforeTestExecutionStage(TestSuite testSuite)
        : base("BeforeTest", testSuite.Instance.GetType())
    {
    }

    public override async Task Execute(ExecutionContext context)
    {
        var classExtensionCallbacks = context.TestSuite.Instance.GetType()
            .GetCustomAttributes<RegisterGdUnitExtensionAttribute>()
            .Where(attr => typeof(IBeforeTestCallback).IsAssignableFrom(attr.ExtensionType))
            .Select(attr => (IBeforeTestCallback)Activator.CreateInstance(attr.ExtensionType)!);
        var methodExtensionCallbacks = context.CurrentTestCase?.MethodInfo.GetCustomAttributes<RegisterGdUnitExtensionAttribute>()
            .Where(attr => typeof(IBeforeTestCallback).IsAssignableFrom(attr.ExtensionType))
            .Select(attr => (IBeforeTestCallback)Activator.CreateInstance(attr.ExtensionType)!) ?? [];
        var callbacks = classExtensionCallbacks.Concat(methodExtensionCallbacks);

        context.FireBeforeTestEvent();
        if (!context.IsSkipped)
        {
            context.MemoryPool.SetActive(StageName, true);
            foreach (var callback in callbacks)
                callback.BeforeTest(context);
            await base
                .Execute(context)
                .ConfigureAwait(true);
            context.MemoryPool.StopMonitoring();
        }
    }
}
