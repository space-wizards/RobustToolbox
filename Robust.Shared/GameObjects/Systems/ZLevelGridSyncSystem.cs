using System;
using System.Numerics;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Keeps explicitly linked grids in their recorded relative pose across separate map physics islands.
/// </summary>
/// Alternatively we allow cross-map joints but that might be a spicier change so we just do this for now.
public sealed partial class ZLevelGridSyncSystem : VirtualController
{
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private ZLevelSystem _zLevels = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_configManager, CVars.ZLevelGridSync, SetEnabled, true);
    }

    private void SetEnabled(bool value) => _enabled = value;

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);
        if (!_enabled)
            return;

        var query = EntityQueryEnumerator<ZLevelGridComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var link, out var xform))
        {
            if (!link.SyncLinkedGrids || link.GridAbove is not { } above)
                continue;

            SyncPair((uid, link, xform), above, frameTime);
        }
    }

    private void SyncPair(Entity<ZLevelGridComponent, TransformComponent> lower, EntityUid upperUid, float frameTime)
    {
        if (!_xformQuery.TryComp(upperUid, out var upperXform) ||
            !_physicsQuery.TryComp(lower.Owner, out var lowerBody) ||
            !_physicsQuery.TryComp(upperUid, out var upperBody))
            return;

        var lowerCanMove = CanMove(lowerBody);
        var upperCanMove = CanMove(upperBody);
        if (!lowerCanMove && !upperCanMove)
            return;

        var (lowerPosition, lowerRotation) = TransformSystem.GetWorldPositionRotation(lower.Comp2);
        var (upperPosition, upperRotation) = TransformSystem.GetWorldPositionRotation(upperXform);
        var targetUpperPosition = lowerPosition + lowerRotation.RotateVec(lower.Comp1.GridAboveOffset);
        var targetUpperRotation = lowerRotation + lower.Comp1.GridAboveRotation;
        var positionError = targetUpperPosition - upperPosition;
        var rotationError = Angle.ShortestDistance(upperRotation, targetUpperRotation).Theta;

        if (ShouldSnap(lower.Comp1, positionError, rotationError) &&
            TrySnapPair(
                lower,
                upperUid,
                lowerPosition,
                lowerRotation,
                upperPosition,
                upperRotation,
                targetUpperPosition,
                targetUpperRotation,
                lowerCanMove,
                upperCanMove))
            return;

        var linearRate = CorrectionRate(lower.Comp1.SyncLinearFrequency, frameTime);
        var angularRate = CorrectionRate(lower.Comp1.SyncAngularFrequency, frameTime);
        var relativeVelocity = upperBody.LinearVelocity - lowerBody.LinearVelocity;
        var linearCorrection = positionError * linearRate - relativeVelocity * lower.Comp1.SyncLinearDamping;
        linearCorrection = ClampLength(linearCorrection, lower.Comp1.SyncMaxLinearCorrection);
        var relativeAngularVelocity = upperBody.AngularVelocity - lowerBody.AngularVelocity;
        var angularCorrection = rotationError * angularRate - relativeAngularVelocity * lower.Comp1.SyncAngularDamping;
        angularCorrection = Math.Clamp(
            angularCorrection,
            -lower.Comp1.SyncMaxAngularCorrection,
            lower.Comp1.SyncMaxAngularCorrection);

        ApplyCorrection(
            lower.Owner,
            lowerBody,
            upperUid,
            upperBody,
            lowerCanMove,
            upperCanMove,
            CanRotate(lowerBody),
            CanRotate(upperBody),
            linearCorrection,
            angularCorrection);
    }

    private bool TrySnapPair(
        Entity<ZLevelGridComponent, TransformComponent> lower,
        EntityUid upper,
        Vector2 lowerPosition,
        Angle lowerRotation,
        Vector2 upperPosition,
        Angle upperRotation,
        Vector2 targetUpperPosition,
        Angle targetUpperRotation,
        bool lowerCanMove,
        bool upperCanMove)
    {
        if (upperCanMove)
            return _zLevels.TryMoveLinkedGrids(upper, targetUpperPosition - upperPosition, targetUpperRotation - upperRotation);
        if (!lowerCanMove)
            return false;

        var targetLowerRotation = upperRotation - lower.Comp1.GridAboveRotation;
        var targetLowerPosition = upperPosition - targetLowerRotation.RotateVec(lower.Comp1.GridAboveOffset);
        return _zLevels.TryMoveLinkedGrids(
            lower.Owner,
            targetLowerPosition - lowerPosition,
            targetLowerRotation - lowerRotation);
    }

    private void ApplyCorrection(
        EntityUid lowerUid,
        PhysicsComponent lowerBody,
        EntityUid upperUid,
        PhysicsComponent upperBody,
        bool lowerCanMove,
        bool upperCanMove,
        bool lowerCanRotate,
        bool upperCanRotate,
        Vector2 linearCorrection,
        double angularCorrection)
    {
        // Lighter grids take more of the correction.
        GetCorrectionWeights(lowerBody.InvMass, upperBody.InvMass, lowerCanMove, upperCanMove, out var lowerWeight, out var upperWeight);
        GetCorrectionWeights(lowerBody.InvI, upperBody.InvI, lowerCanRotate, upperCanRotate, out var lowerAngularWeight, out var upperAngularWeight);

        if (lowerCanMove)
            PhysicsSystem.SetLinearVelocity(lowerUid, lowerBody.LinearVelocity - linearCorrection * lowerWeight, body: lowerBody);
        if (upperCanMove)
            PhysicsSystem.SetLinearVelocity(upperUid, upperBody.LinearVelocity + linearCorrection * upperWeight, body: upperBody);
        if (lowerCanRotate)
            PhysicsSystem.SetAngularVelocity(lowerUid, lowerBody.AngularVelocity - (float) angularCorrection * lowerAngularWeight, body: lowerBody);
        if (upperCanRotate)
            PhysicsSystem.SetAngularVelocity(upperUid, upperBody.AngularVelocity + (float) angularCorrection * upperAngularWeight, body: upperBody);
    }

    private static bool CanMove(PhysicsComponent body)
        => (body.BodyType & (BodyType.Dynamic | BodyType.KinematicController)) != 0;

    private static bool CanRotate(PhysicsComponent body)
        => CanMove(body) && !body.FixedRotation;

    private static Vector2 ClampLength(Vector2 vector, float maximum)
        => maximum <= 0f || vector.LengthSquared() <= maximum * maximum
            ? vector
            : Vector2.Normalize(vector) * maximum;

    private static float CorrectionRate(float rate, float frameTime)
        => rate <= 0f || frameTime <= 0f ? 0f : Math.Min(rate, 1f / frameTime);

    private static bool ShouldSnap(ZLevelGridComponent link, Vector2 positionError, double rotationError)
        => link.SyncSnapDistance > 0f && positionError.LengthSquared() > link.SyncSnapDistance * link.SyncSnapDistance ||
           link.SyncSnapRotation.Theta > 0d && Math.Abs(rotationError) > link.SyncSnapRotation.Theta;

    private static void GetCorrectionWeights(
        float lowerInverseMass,
        float upperInverseMass,
        bool lowerCanMove,
        bool upperCanMove,
        out float lowerWeight,
        out float upperWeight)
    {
        lowerWeight = 0f;
        upperWeight = 0f;
        if (!lowerCanMove)
        {
            upperWeight = 1f;
            return;
        }
        if (!upperCanMove)
        {
            lowerWeight = 1f;
            return;
        }

        var total = lowerInverseMass + upperInverseMass;
        if (total > float.Epsilon)
        {
            lowerWeight = lowerInverseMass / total;
            upperWeight = upperInverseMass / total;
            return;
        }

        lowerWeight = 0.5f;
        upperWeight = 0.5f;
    }
}
