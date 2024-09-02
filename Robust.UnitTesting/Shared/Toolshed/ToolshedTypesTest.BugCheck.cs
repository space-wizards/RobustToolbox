using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Robust.UnitTesting.Shared.Toolshed;

public sealed partial class ToolshedTypesTest
{
    // Assert that T -> Nullable<T> holds and it's inverse does not.
    [Test]
    public void Bug_Nullables_08_21_2023()
    {
        Assert.That(Toolshed.IsTransformableTo(typeof(int), typeof(Nullable<int>)));
        Assert.That(!Toolshed.IsTransformableTo(typeof(Nullable<int>), typeof(int)));
    }
}
