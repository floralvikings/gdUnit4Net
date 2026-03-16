using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GdUnit4.Core.Execution;
using GdUnit4.Core.Execution.TestExtensions;
using static GdUnit4.Assertions;

namespace GdUnit4.Tests.Core;

[TestSuite]
[RegisterGdUnitExtension(typeof(TestParameterInjectionExtension))]
public class TestSuiteWithParameterInjectionExtension
{
    [TestCase]
    [InjectString("Injected Value")]
    public void TestMethodWithParameterInjection(string injectedParameter)
    {
        AssertString(injectedParameter)
            .IsEqual("Injected Value");
    }


    [TestCase]
    [InjectString("First Injected Value")]
    [InjectString("Second Injected Value")]
    public void TestMethodWithMultipleParameterInjections(string first, string second)
    {
        AssertString(first)
            .IsEqual("First Injected Value");
        AssertString(second)
            .IsEqual("Second Injected Value");
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    private class InjectStringAttribute(string value) : Attribute
    {
        public string Value { get; } = value;
    }

    // This is a toy example, but it demonstrates how you could implement a custom extension
    // to inject parameters into test methods based on custom attributes.
    private class TestParameterInjectionExtension : ITestCaseCallback
    {
        public void Execute(ExecutionContext context)
        {
            var injectionAttributes = context.CurrentTestCase?.MethodInfo
                .GetCustomAttributes<InjectStringAttribute>()
                .ToList();

            var argumentsToInject = new List<string>();
            for (var i = 0; i < injectionAttributes?.Count; i++)
            {
                var attribute = injectionAttributes[i];
                argumentsToInject.Add(attribute.Value);
            }

            if (argumentsToInject.Count <= 0) return;
            
            if (context.CurrentTestCase != null)
            {
                context.MethodArguments = argumentsToInject.ToArray();
            }
        }
    }
}
