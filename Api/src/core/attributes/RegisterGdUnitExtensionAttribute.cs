// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using Core.Execution.TestExtensions;

/// <summary>
/// Use this attribute to register extension classes that implement <see cref="IGdUnitExtension"/>.
/// This allows you to extend the test lifecycle with custom behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class RegisterGdUnitExtensionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterGdUnitExtensionAttribute"/> class with the specified extension type.
    /// </summary>
    /// <param name="extensionType">The type of the extension to instantiate and invoke.</param>
    /// <exception cref="ArgumentException">If the extension type does not implement <see cref="IGdUnitExtension"/>.</exception>
    public RegisterGdUnitExtensionAttribute(Type extensionType)
    {
        if (extensionType == null)
            throw new ArgumentNullException(nameof(extensionType), "Extension type cannot be null.");
        if (!typeof(IGdUnitExtension).IsAssignableFrom(extensionType))
            throw new ArgumentException($"Type {extensionType.FullName} does not implement IGdUnitExtension.", nameof(extensionType));

        ExtensionType = extensionType;
    }

    /// <summary>
    /// Gets the type of the extension to instantiate and invoke.
    /// The specified type must implement the <see cref="IGdUnitExtension"/> interface and
    /// have a default constructor with zero parameters..
    /// </summary>
    public Type ExtensionType { get; }
}
