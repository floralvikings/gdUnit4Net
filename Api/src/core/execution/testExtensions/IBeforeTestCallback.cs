// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Core.Execution.TestExtensions;

/// <summary>
/// Defines the API for <see cref="IGdUnitExtension">Extensions</see> that should be
/// invoked before the execution of a test case.
/// </summary>
public interface IBeforeTestCallback : IGdUnitExtension
{
    /// <summary>
    /// Callback that is invoked once before each test with this extension registered.
    /// </summary>
    /// <param name="context">The current test execution context.</param>
    void BeforeTest(ExecutionContext context);
}
