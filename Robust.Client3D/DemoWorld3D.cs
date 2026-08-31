using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Client3D;

public readonly record struct WorldObject3D(SpatialTransform Transform, bool Solid = true)
{
    private static readonly Box3 UnitCube = new(new Vector3(-0.5f), new Vector3(0.5f));

    public Box3 Bounds => UnitCube.TransformedBounds(Transform.Matrix);
}

public static class DemoWorld3D
{
    public static readonly Vector3 SpawnPosition = new(0f, -1.8f, 0.9f);

    public static readonly IReadOnlyList<WorldObject3D> Objects = new WorldObject3D[]
    {
        new(new SpatialTransform(
            new Vector3(0f, 0f, -0.3f),
            Quaternion.Identity,
            new Vector3(9f, 9f, 0.6f))),

        new(new SpatialTransform(
            new Vector3(0f, 4.6f, 2.2f),
            Quaternion.Identity,
            new Vector3(9f, 0.35f, 5f))),
        new(new SpatialTransform(
            new Vector3(0f, -4.6f, 2.2f),
            Quaternion.Identity,
            new Vector3(9f, 0.35f, 5f))),
        new(new SpatialTransform(
            new Vector3(-4.6f, 0f, 2.2f),
            Quaternion.Identity,
            new Vector3(0.35f, 9f, 5f))),
        new(new SpatialTransform(
            new Vector3(4.6f, 0f, 2.2f),
            Quaternion.Identity,
            new Vector3(0.35f, 9f, 5f))),

        new(new SpatialTransform(
            new Vector3(2.4f, 1.5f, 0.65f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.35f),
            new Vector3(2.3f, 1.4f, 1.3f))),
        new(new SpatialTransform(
            new Vector3(-2.3f, 2.1f, 1f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -0.25f),
            new Vector3(1.25f, 1.25f, 2f))),
        new(new SpatialTransform(
            new Vector3(2.8f, -2.4f, 1.5f),
            Quaternion.CreateFromYawPitchRoll(0.65f, 0.2f, 0f),
            new Vector3(0.7f, 0.7f, 3f))),
    };

    public static readonly IReadOnlyList<Box3> CollisionBounds = Objects
        .Where(static worldObject => worldObject.Solid)
        .Select(static worldObject => worldObject.Bounds)
        .ToArray();
}
