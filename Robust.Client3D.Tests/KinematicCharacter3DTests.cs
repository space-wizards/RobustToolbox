using System.Numerics;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.Client3D.Tests;

[TestFixture]
public sealed class KinematicCharacter3DTests
{
    private const float FixedStep = 1f / 120f;
    private static readonly Box3 Floor = new(
        new Vector3(-20f, -20f, -1f),
        new Vector3(20f, 20f, 0f));

    [Test]
    public void CharacterFallsOntoFloorWithoutSinking()
    {
        var character = new KinematicCharacter3D(new Vector3(0f, 0f, 4f), new[] { Floor });

        Simulate(character, default, 240);

        Assert.Multiple(() =>
        {
            Assert.That(character.IsGrounded, Is.True);
            Assert.That(character.Position.Z, Is.EqualTo(KinematicCharacter3D.HalfExtents.Z).Within(0.001f));
            Assert.That(character.Velocity.Z, Is.Zero.Within(0.001f));
        });
    }

    [Test]
    public void CharacterStopsAtSolidObject()
    {
        var wall = new Box3(new Vector3(2f, -5f, 0f), new Vector3(2.5f, 5f, 3f));
        var character = new KinematicCharacter3D(
            new Vector3(0f, 0f, KinematicCharacter3D.HalfExtents.Z),
            new[] { Floor, wall });

        Simulate(character, new CharacterInput3D(Vector2.UnitX, false), 360);

        var expectedCenter = wall.Min.X - KinematicCharacter3D.HalfExtents.X;
        Assert.Multiple(() =>
        {
            Assert.That(character.Position.X, Is.EqualTo(expectedCenter).Within(0.001f));
            Assert.That(character.Bounds.Max.X, Is.LessThanOrEqualTo(wall.Min.X));
            Assert.That(character.Velocity.X, Is.Zero.Within(0.001f));
            Assert.That(character.IsGrounded, Is.True);
        });
    }

    [Test]
    public void CharacterJumpsAndReturnsToFloor()
    {
        var character = new KinematicCharacter3D(
            new Vector3(0f, 0f, KinematicCharacter3D.HalfExtents.Z),
            new[] { Floor });
        Simulate(character, default, 2);

        character.Step(new CharacterInput3D(Vector2.Zero, true), FixedStep);
        var maximumHeight = character.Position.Z;

        for (var i = 0; i < 360; i++)
        {
            character.Step(default, FixedStep);
            maximumHeight = MathF.Max(maximumHeight, character.Position.Z);
        }

        Assert.Multiple(() =>
        {
            Assert.That(maximumHeight, Is.GreaterThan(1.8f));
            Assert.That(character.IsGrounded, Is.True);
            Assert.That(character.Position.Z, Is.EqualTo(KinematicCharacter3D.HalfExtents.Z).Within(0.001f));
        });
    }

    [Test]
    public void FixedInputProducesDeterministicPosition()
    {
        var first = new KinematicCharacter3D(DemoWorld3D.SpawnPosition, DemoWorld3D.CollisionBounds);
        var second = new KinematicCharacter3D(DemoWorld3D.SpawnPosition, DemoWorld3D.CollisionBounds);
        var input = new CharacterInput3D(Vector2.Normalize(new Vector2(1f, 0.4f)), false);

        Simulate(first, input, 240);
        Simulate(second, input, 240);

        Assert.That(first.Position, Is.EqualTo(second.Position));
    }

    private static void Simulate(KinematicCharacter3D character, CharacterInput3D input, int steps)
    {
        for (var i = 0; i < steps; i++)
            character.Step(input, FixedStep);
    }
}
