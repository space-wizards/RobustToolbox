using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Robust.UnitTesting.Shared.Toolshed;

[TestFixture]
public sealed class ToolshedTypesTest : ToolshedTest
{
    [Test]
    public async Task EnumerableAutoCast()
    {
        Assert.That(Toolshed.IsTransformableTo(typeof(int), typeof(IEnumerable<int>)));
        var l = Expression
            .Lambda<Func<IEnumerable<int>>>(
                Toolshed.GetTransformer(typeof(int), typeof(IEnumerable<int>), Expression.Constant(1))
            )
            .Compile();
        Assert.That(l(), NUnit.Framework.Is.Not.Empty);
    }

    [Test]
    public async Task EnumerableSubtypeAutocast()
    {
        Assert.That(Toolshed.IsTransformableTo(typeof(int), typeof(IEnumerable<IComparable>)));
        var l = Expression
            .Lambda<Func<IEnumerable<IComparable>>>(
                    Toolshed.GetTransformer(typeof(int), typeof(IEnumerable<IComparable>), Expression.Constant(1))
                )
            .Compile();
        Assert.That(l(), NUnit.Framework.Is.Not.Empty);
    }

    [Test]
    public async Task CastToObject()
    {
        Assert.That(Toolshed.IsTransformableTo(typeof(int), typeof(object)));
        var l = Expression
            .Lambda<Func<object>>(
                Toolshed.GetTransformer(typeof(int), typeof(object), Expression.Constant(1))
            )
            .Compile();
        Assert.That(l(), NUnit.Framework.Is.TypeOf<int>());
    }
}
