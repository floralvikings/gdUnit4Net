// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Core.Execution.TestExtensions;

/// <summary>
/// Defines the API for <see cref="IGdUnitExtension">Extensions</see> that should be
/// invoked before the execution of a test suite.
/// </summary>
public interface IBeforeCallback : IGdUnitExtension
{
    /// <summary>
    /// Callback that is invoked once before all tests in the current test suite.
    /// </summary>
    /// <param name="context">The current test execution context.</param>
    void Before(ExecutionContext context);
}
