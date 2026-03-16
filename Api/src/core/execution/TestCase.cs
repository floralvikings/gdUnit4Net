// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Reflection;

using Extensions;

/// <summary>
///     Represents a discovered test case and its execution metadata.
/// </summary>
/// <remarks>
///     Stores the reflected test method, selected <see cref="TestCaseAttribute"/>, resolved arguments,
///     and source location information used during test execution.
/// </remarks>
public sealed class TestCase
{
    internal TestCase(Guid id, MethodInfo methodInfo, int lineNumber, int attributeIndex)
    {
        Id = id;
        MethodInfo = methodInfo;
        Line = lineNumber;
        Parameters = InitialParameters();
        TestCaseAttribute = TestCaseAttributes[attributeIndex];
    }

    /// <summary>
    ///     Gets the name of the test case.
    /// </summary>
    public string Name => MethodInfo.Name;

    /// <summary>
    ///     Gets the unique identifier of the test case.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    ///     Gets the source line number of the test case.
    /// </summary>
    public int Line { get; private set; }

    /// <summary>
    ///     Gets all <see cref="TestCaseAttribute"/> instances declared on the test method.
    /// </summary>
#pragma warning disable CA1002
    public List<TestCaseAttribute> TestCaseAttributes
#pragma warning restore CA1002
        => [.. MethodInfo.GetCustomAttributes<TestCaseAttribute>().Where(TestParametersFilter)];

    /// <summary>
    ///     Gets the active <see cref="TestCaseAttribute"/> for this test case instance.
    /// </summary>
    public TestCaseAttribute TestCaseAttribute { get; internal init; }

    /// <summary>
    ///     Gets metadata about the reflected test method.
    /// </summary>
    public MethodInfo MethodInfo { get; internal set; }

    /// <summary>
    ///     Gets the resolved arguments used to invoke the test case.
    /// </summary>
    /// <remarks>
    ///     For parameterized tests, this returns the arguments from <see cref="TestCaseAttribute"/>.
    ///     Otherwise, it returns values resolved from configured parameter providers.
    /// </remarks>
#pragma warning disable CA1819
    public object?[] Arguments => IsParameterized ? TestCaseAttribute.Arguments : [.. Parameters.SelectMany(ResolveParam)];
#pragma warning restore CA1819

    /// <summary>
    ///     Gets a value indicating whether the test case is skipped.
    /// </summary>
    public bool IsSkipped => IgnoreUntilAttribute.IsSkipped(MethodInfo);

    /// <summary>
    ///     Gets a value indicating whether the test case uses parameterized arguments.
    /// </summary>
    public bool IsParameterized => TestCaseAttributes.Any(p => p.Arguments.Length > 0);

    /// <summary>
    /// Gets the resolved parameters for this test case.
    /// </summary>
    public IEnumerable<object> Parameters { get; }

    internal bool HasDataPoint => DataPoint != null;

    internal DataPointAttribute? DataPoint => MethodInfo.GetCustomAttribute<DataPointAttribute>();

    private Func<TestCaseAttribute, bool> TestParametersFilter { get; } = _ => true;

    internal static string BuildDisplayName(string testName, TestCaseAttribute attribute, int attributeIndex = -1)
    {
        var name = attribute.TestName ?? testName;
        if (attributeIndex == -1)
            return name;

        var parameters = string
            .Join(", ", attribute.Arguments.Select(GdUnitExtensions.Formatted))
            .Replace(".", ",", StringComparison.Ordinal);
        return $"{name}:{attributeIndex} ({parameters})";
    }

    internal static string BuildFullyQualifiedName(string classNameSpace, string testName, TestCaseAttribute attr)
    {
        if (attr.Arguments.Length == 0)
            return $"{classNameSpace}.{attr.TestName ?? testName}";
        var parameterizedTestName = BuildDisplayName(testName, attr);
        return $"{classNameSpace}.{testName}.{parameterizedTestName}";
    }

    private IEnumerable<object> ResolveParam(object input)
    {
        if (input is IValueProvider provider)
            return provider.GetValues();
        return [input];
    }

    private List<object> InitialParameters()
        =>
        [
            .. MethodInfo.GetParameters()
                .SelectMany(pi => pi.GetCustomAttributesData()
                    .Where(attr => attr.AttributeType == typeof(FuzzerAttribute))
                    .Select(attr =>
                    {
                        var arguments = attr.ConstructorArguments.Select(arg => arg.Value).ToArray();
                        return attr.Constructor.Invoke(arguments);
                    }))
        ];
}
