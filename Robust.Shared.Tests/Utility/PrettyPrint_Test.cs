using NUnit.Framework;
using Robust.Shared.Utility;

// ReSharper disable once CheckNamespace
namespace Robust.Shared.TestPrettyPrint;

public sealed class Foo
{
    override public string ToString() { return "ACustomFooRep"; }
}

public sealed class Bar {}

[TestFixture]
[Parallelizable(ParallelScope.Fixtures | ParallelScope.All)]
[TestOf(typeof(PrettyPrint))]
internal sealed class PrettyPrint_Test
{
    private static IEnumerable<(object val, string expectedRep, string expectedTypeRep)> TestCases { get; } = new (object, string, string)[]
    {
        (new Foo(), "ACustomFooRep", "R.Sh.TestPrettyPrint.Foo"),
        (new Robust.Shared.TestPrettyPrint.Bar(), "R.Sh.TestPrettyPrint.Bar", ""),
    };

    [Test]
    public void Test([ValueSource(nameof(TestCases))] (object value, string expectedRep, string expectedTypeRep) data)
    {
        Assert.That(PrettyPrint.PrintUserFacingWithType(data.value, out var typeRep), Is.EqualTo(data.expectedRep));
        Assert.That(typeRep, Is.EqualTo(data.expectedTypeRep));
    }
}
