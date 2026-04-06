using System;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics;

internal sealed class PhysicsHull_Test
{
    private static readonly TestCaseData[] CollinearHulls = new TestCaseData[]
    {
        new TestCaseData(new Vector2[]
        {
            Vector2.Zero,
            Vector2.One,
            Vector2.UnitY,
        }, 3),
        // Same points
        new TestCaseData(new Vector2[]
        {
            Vector2.Zero,
            Vector2.One,
            Vector2.One,
            Vector2.UnitY,
        }, 3),
        new TestCaseData(new Vector2[]
        {
            Vector2.Zero,
            Vector2.UnitX / 2f,
            Vector2.UnitX,
            Vector2.UnitY,
        }, 3),
    };

    [Test, TestCaseSource(nameof(CollinearHulls))]
    public void CollinearTest(Vector2[] vertices, int count)
    {
        var hull = InternalPhysicsHull.ComputeHull(vertices.AsSpan(), vertices.Length);
        Assert.That(hull.Count, Is.EqualTo(count));
    }

    private static readonly TestCaseData[] ValidateHulls = new TestCaseData[]
    {
        new TestCaseData(Array.Empty<Vector2>(), false),
        new TestCaseData(new Vector2[]
        {
            Vector2.Zero,
            Vector2.One,
            Vector2.UnitY,
        }, true),
        new TestCaseData(new Vector2[]
        {
            Vector2.Zero,
            Vector2.UnitX,
            Vector2.One,
            Vector2.UnitY,
        }, true),
        // Same point
        new TestCaseData(new Vector2[]
        {
            Vector2.Zero,
            Vector2.One,
            Vector2.One,
            Vector2.UnitY,
        }, false),
        // Collinear point
        new TestCaseData(new Vector2[]
        {
            Vector2.Zero,
            Vector2.One / 2f,
            Vector2.One,
        }, false),
        // Too many verts
        new TestCaseData(new Vector2[]
        {
            Vector2.Zero,
            Vector2.UnitX,
            Vector2.One * 1f,
            Vector2.One * 2f,
            Vector2.One * 3f,
            Vector2.One * 4f,
            Vector2.One * 5f,
            Vector2.One * 6f,
            Vector2.One * 7f,
            Vector2.One * 8f,
        }, false),
    };

    [Test, TestCaseSource(nameof(ValidateHulls))]
    public void ValidationTest(Vector2[] vertices, bool result)
    {
        var hull = new InternalPhysicsHull(vertices.AsSpan(), vertices.Length);
        Assert.That(InternalPhysicsHull.ValidateHull(hull), Is.EqualTo(result));
    }
}
