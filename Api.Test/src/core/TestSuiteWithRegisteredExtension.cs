using GdUnit4.Core.Execution;
using GdUnit4.Core.Execution.TestExtensions;

namespace GdUnit4.Tests.Core;

using static Assertions;

[TestSuite]
[RegisterGdUnitExtension(typeof(TestClassLevelExtension))]
public class TestSuiteWithRegisteredExtension
{
    private bool beforeCallbackExecuted;
    
    private bool beforeTestClassLevelCallbackExecuted;
    
    private bool beforeTestMethodLevelCallbackExecuted;
    
    private bool afterCallbackExecuted;
    
    private bool afterTestClassLevelCallbackExecuted;
    
    [Before]
    public void Before() =>
        AssertBool(beforeCallbackExecuted)
            .IsTrue();


    [BeforeTest]
    public void BeforeTest() =>
        AssertBool(beforeTestClassLevelCallbackExecuted)
            .IsTrue();


    [TestCase]
    public void TestMethodWithNoMethodLevelExtension()
    {
        AssertBool(beforeCallbackExecuted)
            .IsTrue();

        AssertBool(beforeTestClassLevelCallbackExecuted)
            .IsTrue();

        AssertBool(beforeTestMethodLevelCallbackExecuted)
            .IsFalse();
    }
    
    [TestCase]
    [RegisterGdUnitExtension(typeof(TestMethodLevelExtension))]
    public void TestMethodWithMethodLevelExtension()
    {
        AssertBool(beforeCallbackExecuted)
            .IsTrue();

        AssertBool(beforeTestClassLevelCallbackExecuted)
            .IsTrue();

        AssertBool(beforeTestMethodLevelCallbackExecuted)
            .IsTrue();
    }
    
    [AfterTest]
    public void AfterTest() =>
        AssertBool(afterTestClassLevelCallbackExecuted)
            .IsTrue();
    
    [After]
    public void After() =>
        AssertBool(afterCallbackExecuted)
            .IsTrue();
    
    private class TestClassLevelExtension : IBeforeCallback, IBeforeTestCallback, IAfterCallback, IAfterTestCallback
    {
        public void Before(ExecutionContext context)
        {
            AssertString(context.TestSuite.Name)
                .IsEqual("TestSuiteWithRegisteredExtension");
            
            AssertString(context.TestCaseName)
                .IsEmpty();
            
            if (context.TestSuite.Instance is TestSuiteWithRegisteredExtension instance) 
                instance.beforeCallbackExecuted = true;
        }


        public void BeforeTest(ExecutionContext context)
        {
            AssertString(context.TestSuite.Name)
                .IsEqual("TestSuiteWithRegisteredExtension");

            AssertString(context.TestCaseName)
                .IsNotEmpty();
            
            if (context.TestSuite.Instance is TestSuiteWithRegisteredExtension instance) 
                instance.beforeTestClassLevelCallbackExecuted = true;
        }


        public void After(ExecutionContext context)
        {
            AssertString(context.TestSuite.Name)
                .IsEqual("TestSuiteWithRegisteredExtension");

            if (context.TestSuite.Instance is TestSuiteWithRegisteredExtension instance) 
                instance.afterCallbackExecuted = true;
        }


        public void AfterTest(ExecutionContext context)
        {
            AssertString(context.TestSuite.Name)
                .IsEqual("TestSuiteWithRegisteredExtension");

            AssertString(context.TestCaseName)
                .IsNotEmpty();
            
            if (context.TestSuite.Instance is TestSuiteWithRegisteredExtension instance) 
                instance.afterTestClassLevelCallbackExecuted = true;
        }
    }
    
    private class TestMethodLevelExtension : IBeforeTestCallback, IAfterTestCallback
    {
        public void BeforeTest(ExecutionContext context)
        {
            AssertString(context.TestSuite.Name)
                .IsEqual("TestSuiteWithRegisteredExtension");

            AssertString(context.TestCaseName)
                .IsNotEmpty();
            
            if (context.TestSuite.Instance is TestSuiteWithRegisteredExtension instance) 
                instance.beforeTestMethodLevelCallbackExecuted = true;
        }


        public void AfterTest(ExecutionContext context)
        {
            AssertString(context.TestSuite.Name)
                .IsEqual("TestSuiteWithRegisteredExtension");

            AssertString(context.TestCaseName)
                .IsNotEmpty();
        }
    }
}
