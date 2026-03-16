// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Reflection;

using Api;

/// <summary>
///     Represents a discovered test suite and its execution metadata.
/// </summary>
/// <remarks>
///     Stores the suite instance, source resource path, and <see cref="TestCase"/> values
///     used by the execution pipeline.
/// </remarks>
public sealed class TestSuite : IDisposable
{
    private readonly Lazy<IEnumerable<TestCase>> testCases;

    internal TestSuite(TestSuiteNode suite)
        : this(
            FindTypeOnAssembly(suite.AssemblyPath, suite.ManagedType),
            suite.Tests,
            suite.SourceFile)
    {
    }

    internal TestSuite(Type type, List<TestCaseNode> tests, string sourceFile)
    {
        Instance = Activator.CreateInstance(type)
                   ?? throw new InvalidOperationException($"Cannot create an instance of '{type.FullName}' because it does not have a public parameterless constructor.");

        Name = type.Name;
        ResourcePath = sourceFile;

        // we do lazy loading to only load test case one times
        testCases = new Lazy<IEnumerable<TestCase>>(() => LoadTestCases(type, tests));
    }

    /// <summary>
    ///     Gets the number of test cases contained in the suite.
    /// </summary>
    public int TestCaseCount => TestCases.Count();

    /// <summary>
    ///     Gets the test cases discovered for the suite.
    /// </summary>
    public IEnumerable<TestCase> TestCases => testCases.Value;

    /// <summary>
    ///     Gets the source resource path of the test suite.
    /// </summary>
    public string ResourcePath { get; internal set; }

    /// <summary>
    ///     Gets the display name of the test suite.
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    ///     Gets the fully qualified name of the suite instance type.
    /// </summary>
    public string? FullName => Instance.GetType().FullName;

    /// <summary>
    ///     Gets the instantiated test suite object.
    /// </summary>
    public object Instance { get; internal set; }

    /// <summary>
    ///     Gets a value indicating whether test filtering is disabled for this suite.
    /// </summary>
    public bool FilterDisabled { get; internal set; }

    /// <summary>
    ///     Disposes the test suite instance when it implements <see cref="IDisposable"/>.
    ///     Not meant to be called directly.
    /// </summary>
    public void Dispose()
    {
        if (Instance is IDisposable disposable)
            disposable.Dispose();
    }

    private static List<TestCase> LoadTestCases(Type type, List<TestCaseNode> includedTests)
        =>
        [
            .. type.GetMethods()
                .Where(m => m.IsDefined(typeof(TestCaseAttribute)))
                .Join(
                    includedTests,
                    m => m.Name,
                    test => test.ManagedMethod,
                    (mi, test) => new TestCase(test.Id, mi, test.LineNumber, test.AttributeIndex))
        ];

    private static Type FindTypeOnAssembly(string assemblyPath, string clazz)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // if (assembly.Location != assemblyName)
            //    continue;
            var type = assembly.GetType(clazz);
            if (type != null)
                return type;
        }

        try
        {
            var assembly = Assembly.Load(AssemblyName.GetAssemblyName(assemblyPath));
            return assembly.GetType(clazz)!;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to resolve type '{clazz}': {ex.Message}");
            throw new InvalidOperationException($"Could not find type {clazz} on assembly {assemblyPath}");
        }
    }
}
