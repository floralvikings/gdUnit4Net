// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Core.Execution.TestExtensions;

/// <summary>
/// Defines the API for <see cref="IGdUnitExtension">Extensions</see> that should be invoked after [BeforeTest] but before the test method is
/// invoked.
/// </summary>
public interface ITestCaseCallback : IGdUnitExtension
{
    /// <summary>
    /// Callback that is invoked once after [BeforeTest] but before the test method is invoked for each test with this extension registered.
    /// </summary>
    /// <param name="context">The current test execution context.</param>
    void Execute(ExecutionContext context);
}
