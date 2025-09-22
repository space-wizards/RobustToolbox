using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
[TestOf(typeof(Transform))]
public sealed class TransformTest
{
    private static (Vector2 V, Transform T, Vector2 Exp)[] _vecMulData =
    [
        (Vector2.Zero, new  Transform(Vector2.Zero, 0), Exp: Vector2.Zero),
        (Vector2.Zero, new  Transform(Vector2.One, 0), Exp: Vector2.One),
        (Vector2.Zero, new Transform(Vector2.One, Angle.FromDegrees(45)), Vector2.One),
        (Vector2.One, new Transform(Vector2.Zero, Angle.FromDegrees(45)), new(0, MathF.Sqrt(2))),
        (Vector2.One, new Transform(Vector2.One, Angle.FromDegrees(45)), new(1, 1+MathF.Sqrt(2))),
        (new Vector2(2f, 1f), new Transform(Vector2.One, Angle.FromDegrees(45)), new(1.707107f, 3.12132f)),
    ];

    [Test]
    public void TestVectorMul([ValueSource(nameof(_vecMulData))] (Vector2 V, Transform T, Vector2 Exp) dat)
    {
        var result = Transform.Mul(dat.T, dat.V);
        Assert.That(result, Is.Approximately(dat.Exp, 0.001f));
    }

    [Test]
    public void TestVectorMulSimd([ValueSource(nameof(_vecMulData))] (Vector2 V, Transform T, Vector2 Exp) dat)
    {
        var x = Vector128.Create(dat.V.X);
        var y = Vector128.Create(dat.V.Y);
        Transform.MulSimd(dat.T, x, y, out var xOut, out var yOut);
        Assert.That(xOut, Is.Approximately(Vector128.Create(dat.Exp[0]), 0.001f));
        Assert.That(yOut, Is.Approximately(Vector128.Create(dat.Exp[1]), 0.001f));
    }
}
