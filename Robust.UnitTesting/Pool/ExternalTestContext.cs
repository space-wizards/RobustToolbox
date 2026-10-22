using System.IO;

namespace Robust.UnitTesting.Pool;

/// <summary>
/// Generic implementation of <see cref="ITestContextLike"/> for usage outside of actual tests.
/// </summary>
public sealed class ExternalTestContext(string name, TextWriter writer) : ITestContextLike
{
    public string FullName => name;
    public TextWriter Out => writer;
}
