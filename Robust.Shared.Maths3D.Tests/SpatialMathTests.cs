using System.Numerics;
using NUnit.Framework;

namespace Robust.Shared.Maths.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class SpatialMathTests
{
    [Test]
    public void PositiveYawRotatesEastTowardNorth()
    {
        var rotation = SpatialMath.FromYaw(new Angle(MathF.PI / 2f));
        var result = rotation.Rotate(Vector3.UnitX);

        Assert.That(result.X, Is.EqualTo(0f).Within(0.00001f));
        Assert.That(result.Y, Is.EqualTo(1f).Within(0.00001f));
        Assert.That(result.Z, Is.EqualTo(0f).Within(0.00001f));
    }

    [Test]
    public void TransformAndInverseRoundTripPoint()
    {
        var transform = new SpatialTransform(
            new Vector3(12f, -3f, 7f),
            Quaternion.CreateFromYawPitchRoll(0.7f, -0.2f, 0.4f),
            new Vector3(2f, 3f, 0.5f));
        var point = new Vector3(-4f, 8f, 1.5f);

        var roundTrip = transform.InverseTransformPoint(transform.TransformPoint(point));

        Assert.That(roundTrip.X, Is.EqualTo(point.X).Within(0.0001f));
        Assert.That(roundTrip.Y, Is.EqualTo(point.Y).Within(0.0001f));
        Assert.That(roundTrip.Z, Is.EqualTo(point.Z).Within(0.0001f));
    }

    [Test]
    public void ChildMatrixComposesWithParentMatrix()
    {
        var child = new SpatialTransform(
            new Vector3(1f, 0f, 2f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.4f),
            Vector3.One);
        var parent = new SpatialTransform(
            new Vector3(10f, -2f, 4f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 1.2f),
            Vector3.One);
        var point = new Vector3(2f, 3f, 4f);

        var composed = Vector3.Transform(point, child.Matrix * parent.Matrix);
        var sequential = parent.TransformPoint(child.TransformPoint(point));

        Assert.That(composed.X, Is.EqualTo(sequential.X).Within(0.0001f));
        Assert.That(composed.Y, Is.EqualTo(sequential.Y).Within(0.0001f));
        Assert.That(composed.Z, Is.EqualTo(sequential.Z).Within(0.0001f));
    }

    [Test]
    public void RelativeOrientationRecoversLocalOrientation()
    {
        var local = Quaternion.CreateFromYawPitchRoll(0.2f, -0.5f, 0.9f);
        var parent = Quaternion.CreateFromYawPitchRoll(-0.7f, 0.3f, 0.1f);
        var world = SpatialMath.Compose(local, parent);

        Assert.That(SpatialMath.RelativeTo(world, parent).EqualsApprox(local), Is.True);
    }

    [Test]
    public void TransformedBoxContainsEveryTransformedCorner()
    {
        var box = Box3.CenteredAround(Vector3.Zero, new Vector3(2f, 4f, 6f));
        var transform = SpatialMath.CreateTransform(
            new Vector3(7f, -1f, 3f),
            Quaternion.CreateFromYawPitchRoll(0.3f, 0.5f, 0.7f),
            Vector3.One);
        var bounds = box.TransformedBounds(transform);

        for (var corner = 0; corner < 8; corner++)
        {
            var point = new Vector3(
                (corner & 1) == 0 ? box.Min.X : box.Max.X,
                (corner & 2) == 0 ? box.Min.Y : box.Max.Y,
                (corner & 4) == 0 ? box.Min.Z : box.Max.Z);

            Assert.That(bounds.Contains(Vector3.Transform(point, transform)), Is.True);
        }
    }

    [Test]
    public void SpatialIndexTracksThreeDimensionalBounds()
    {
        var index = new LinearSpatialIndex3<string>();
        index.Add("floor", Box3.FromDimensions(Vector3.Zero, new Vector3(10f, 10f, 0.25f)));
        index.Add("ceiling", Box3.FromDimensions(new Vector3(0f, 0f, 4f), new Vector3(10f, 10f, 0.25f)));

        var results = new List<string>();
        index.Query(Box3.CenteredAround(new Vector3(5f, 5f, 4f), Vector3.One), results);

        Assert.That(results, Is.EquivalentTo(new[] { "ceiling" }));

        index.Update("floor", Box3.FromDimensions(new Vector3(0f, 0f, 3.75f), new Vector3(10f, 10f, 0.25f)));
        results.Clear();
        index.Query(Box3.CenteredAround(new Vector3(5f, 5f, 4f), Vector3.One), results);

        Assert.That(results, Is.EquivalentTo(new[] { "floor", "ceiling" }));
        Assert.That(index.Remove("ceiling"), Is.True);
        Assert.That(index.Count, Is.EqualTo(1));
    }

    [Test]
    public void RayIntersectsVolumeAlongVerticalAxis()
    {
        var ray = new Ray3(new Vector3(2f, 2f, 10f), -Vector3.UnitZ);
        var volume = Box3.FromDimensions(new Vector3(0f, 0f, 2f), new Vector3(4f, 4f, 3f));

        Assert.That(ray.TryIntersect(volume, out var distance), Is.True);
        Assert.That(distance, Is.EqualTo(5f).Within(0.00001f));
        Assert.That(ray.GetPoint(distance), Is.EqualTo(new Vector3(2f, 2f, 5f)));
    }

    [Test]
    public void RayParallelToVolumeCanMiss()
    {
        var ray = new Ray3(new Vector3(8f, 2f, 10f), -Vector3.UnitZ);
        var volume = Box3.FromDimensions(Vector3.Zero, new Vector3(4f, 4f, 4f));

        Assert.That(ray.TryIntersect(volume, out _), Is.False);
    }
}
