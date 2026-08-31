using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Shared3D;

public readonly record struct CharacterInput3D(Vector2 Movement, bool Jump);

/// <summary>
/// A deterministic kinematic character body shared by server simulation and client prediction.
/// </summary>
public sealed class KinematicCharacter3D
{
    public static readonly Vector3 HalfExtents = new(0.35f, 0.35f, 0.9f);

    private const float GroundSpeed = 4.5f;
    private const float GroundAcceleration = 32f;
    private const float AirAcceleration = 9f;
    private const float Gravity = 18f;
    private const float JumpSpeed = 6.4f;
    private const float CollisionEpsilon = 0.0001f;

    private readonly IReadOnlyList<Box3> _obstacles;

    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }
    public bool IsGrounded { get; private set; }
    public Box3 Bounds => Box3.CenteredAround(Position, HalfExtents * 2f);

    public KinematicCharacter3D(Vector3 spawnPosition, IReadOnlyList<Box3> obstacles)
    {
        Position = spawnPosition;
        _obstacles = obstacles;
    }

    public void ApplyAuthoritativeState(Vector3 position, Vector3 velocity, bool grounded)
    {
        Position = position;
        Velocity = velocity;
        IsGrounded = grounded;
    }

    public void Step(CharacterInput3D input, float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        var movement = input.Movement;
        if (movement.LengthSquared() > 1f)
            movement = Vector2.Normalize(movement);

        var desiredVelocity = movement * GroundSpeed;
        var acceleration = IsGrounded ? GroundAcceleration : AirAcceleration;
        Velocity = new Vector3(
            MoveTowards(Velocity.X, desiredVelocity.X, acceleration * deltaTime),
            MoveTowards(Velocity.Y, desiredVelocity.Y, acceleration * deltaTime),
            Velocity.Z);

        if (input.Jump && IsGrounded)
        {
            Velocity = Velocity with { Z = JumpSpeed };
            IsGrounded = false;
        }

        Velocity = Velocity with { Z = Velocity.Z - Gravity * deltaTime };
        MoveAxis(0, Velocity.X * deltaTime);
        MoveAxis(1, Velocity.Y * deltaTime);

        IsGrounded = false;
        MoveAxis(2, Velocity.Z * deltaTime);
    }

    private void MoveAxis(int axis, float distance)
    {
        if (MathF.Abs(distance) < float.Epsilon)
            return;

        var candidate = Position;
        SetAxis(ref candidate, axis, GetAxis(candidate, axis) + distance);

        foreach (var obstacle in _obstacles)
        {
            var candidateBounds = Box3.CenteredAround(candidate, HalfExtents * 2f);
            if (!OverlapsStrict(candidateBounds, obstacle))
                continue;

            var resolved = distance > 0f
                ? GetAxis(obstacle.Min, axis) - GetAxis(HalfExtents, axis) - CollisionEpsilon
                : GetAxis(obstacle.Max, axis) + GetAxis(HalfExtents, axis) + CollisionEpsilon;
            SetAxis(ref candidate, axis, resolved);
            SetVelocityAxis(axis, 0f);

            if (axis == 2 && distance < 0f)
                IsGrounded = true;
        }

        Position = candidate;
    }

    private static bool OverlapsStrict(Box3 left, Box3 right)
    {
        return left.Min.X < right.Max.X - CollisionEpsilon && left.Max.X > right.Min.X + CollisionEpsilon &&
               left.Min.Y < right.Max.Y - CollisionEpsilon && left.Max.Y > right.Min.Y + CollisionEpsilon &&
               left.Min.Z < right.Max.Z - CollisionEpsilon && left.Max.Z > right.Min.Z + CollisionEpsilon;
    }

    private void SetVelocityAxis(int axis, float value)
    {
        Velocity = axis switch
        {
            0 => Velocity with { X = value },
            1 => Velocity with { Y = value },
            2 => Velocity with { Z = value },
            _ => throw new ArgumentOutOfRangeException(nameof(axis)),
        };
    }

    private static float GetAxis(Vector3 vector, int axis)
    {
        return axis switch
        {
            0 => vector.X,
            1 => vector.Y,
            2 => vector.Z,
            _ => throw new ArgumentOutOfRangeException(nameof(axis)),
        };
    }

    private static void SetAxis(ref Vector3 vector, int axis, float value)
    {
        vector = axis switch
        {
            0 => vector with { X = value },
            1 => vector with { Y = value },
            2 => vector with { Z = value },
            _ => throw new ArgumentOutOfRangeException(nameof(axis)),
        };
    }

    private static float MoveTowards(float current, float target, float maximumDelta)
    {
        if (MathF.Abs(target - current) <= maximumDelta)
            return target;

        return current + MathF.CopySign(maximumDelta, target - current);
    }
}
