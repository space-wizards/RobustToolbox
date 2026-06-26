using System.Numerics;
using NUnit.Framework;
using Robust.Client.Graphics.Clyde;
using Robust.Shared.Maths;
using Robust.UnitTesting;

namespace Robust.Client.Tests.Sprite;

[TestFixture]
[TestOf(typeof(Clyde))]
public sealed class TransformCenteredBoxTest
{
    private static IEnumerable<(Box2 box, float angle, Vector2 offset, Vector2 scale)> _args =
    [
        (Box2.UnitCentered, 0f, new Vector2(0f,0f), new Vector2(1f,-1f)),
        (Box2.UnitCentered, MathF.PI / 7, new Vector2(0f, 0f), new Vector2(1f, -1f)),
        (Box2.UnitCentered, MathF.PI / 7, new Vector2(-1f, 2.3f), new Vector2(1f, -1f)),
        (Box2.UnitCentered, MathF.PI / 7, new Vector2(-1f, 2.3f), new Vector2(1.25f, -0.32f)),
        (new Box2(1,2,3,4), MathF.PI / 7, new Vector2(-1f, 2.3f), new Vector2(1.25f, -0.32f)),
    ];

    [Test]
    public void TestTransformCenteredBox([ValueSource(nameof(_args))]
        (Box2 box, float angle, Vector2 offset, Vector2 scale) args)
    {
        var result = Clyde.TransformCenteredBox(args.box, args.angle, args.offset, args.scale);
        var expected = Matrix3x2.CreateRotation(args.angle).TransformBox(args.box).Translated(args.offset);
        expected = new(
            expected.Left * args.scale.X,
            expected.Top * args.scale.Y,
            expected.Right * args.scale.X,
            expected.Bottom * args.scale.Y);
        Assert.That(result, Is.Approximately(expected));
    }
}
