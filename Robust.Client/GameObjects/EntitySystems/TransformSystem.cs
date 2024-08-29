using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    /// <summary>
    ///     Handles interpolation of transform positions.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class TransformSystem : SharedTransformSystem
    {
        // Max distance per tick how far an entity can move before it is considered teleporting.
        // TODO: Make these values somehow dependent on server TPS.
        private const float MaxInterpolationDistance = 2.0f;
        private const float MaxInterpolationDistanceSquared = MaxInterpolationDistance * MaxInterpolationDistance;

        private const float MinInterpolationDistance = 0.001f;
        private const float MinInterpolationDistanceSquared = MinInterpolationDistance * MinInterpolationDistance;

        [Dependency] private readonly IGameTiming _gameTiming = default!;

        // Only keep track of transforms actively lerping.
        // Much faster than iterating 3000+ transforms every frame.
        [ViewVariables] private readonly List<Entity<TransformComponent>> _lerpingTransforms = new();

        public void Reset()
        {
            foreach (var (_, xform) in _lerpingTransforms)
            {
                xform.ActivelyLerping = false;
                xform.NextPosition = null;
                xform.NextRotation = null;
                xform.LerpParent = EntityUid.Invalid;
            }
            _lerpingTransforms.Clear();
        }

        public override void ActivateLerp(EntityUid uid, TransformComponent xform)
        {
            // This lerping logic is pretty convoluted and generally assumes that the client does not mispredict.
            // A more foolproof solution would be to just cache the coordinates at which any given entity was most
            // recently rendered and using that as the lerp origin. However that'd require enumerating over all entities
            // every tick which is pretty icky.

            // The general considerations are:
            // - If the client receives a server state for an entity moving from a->b and predicts nothing else, then it
            //   should show the entity lerping.
            // - If the client predicts an entity will move while already lerping due to a state-application, it should
            //   clear the state's lerp, under the assumption that the client predicted the state and already rendered
            //   the entity in the state's final position.
            // - If the client predicts that an entity moves, then we only lerp if this is the first time that the tick
            //   was predicted. I.e., we assume the entity was already rendered in the final position that was
            //   previously predicted.
            // - If the client predicts that an entity should lerp twice in the same tick, then we need to combine them.
            //   I.e. moving from a->b then b->c, the client should lerp from a->c.

            // If the client predicts an entity moves while already lerping, it should clear the
            // predict a->b, lerp a->b
            // predicted a->b, then predict b->c. Lerp b->c
            // predicted a->b, then predict b->c. Lerp b->c
            // predicted a->b, predicted b->c, then predict c->d. Lerp c->d
            // server state a->b, then predicted b->c, lerp b->c
            // server state a->b, then predicted b->c, then predict d, lerp b->c

            if (_gameTiming.ApplyingState)
            {
                if (xform.ActivelyLerping)
                {
                    // This should not happen, but can happen if some bad component state application code modifies an entity's coordinates.
                    Log.Error($"Entity {(ToPrettyString(uid))} tried to lerp twice while applying component states.");
                    return;
                }

                _lerpingTransforms.Add((uid, xform));
                xform.ActivelyLerping = true;
                xform.PredictedLerp = false;
                xform.LerpParent = xform.ParentUid;
                xform.PrevRotation = xform._localRotation;
                xform.PrevPosition = xform._localPosition;
                xform.LastLerp = _gameTiming.CurTick;
                return;
            }

            xform.LastLerp = _gameTiming.CurTick;
            if (!_gameTiming.IsFirstTimePredicted)
            {
                xform.ActivelyLerping = false;
                return;
            }

            if (!xform.ActivelyLerping)
            {
                _lerpingTransforms.Add((uid, xform));
                xform.ActivelyLerping = true;
                xform.PredictedLerp = true;
                xform.PrevRotation = xform._localRotation;
                xform.PrevPosition = xform._localPosition;
                xform.LerpParent = xform.ParentUid;
                return;
            }

            if (!xform.PredictedLerp || xform.LerpParent != xform.ParentUid)
            {
                // Existing lerp was not due to prediction, but due to state application. That lerp should already
                // have been rendered, so we will start a new lerp from the current position.
                xform.PrevRotation = xform._localRotation;
                xform.PrevPosition = xform._localPosition;
                xform.LerpParent = xform.ParentUid;
            }
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            var step = (float) (_gameTiming.TickRemainder.TotalSeconds / _gameTiming.TickPeriod.TotalSeconds);

            for (var i = 0; i < _lerpingTransforms.Count; i++)
            {
                var (uid, transform) = _lerpingTransforms[i];
                var found = false;

                // Only lerp if parent didn't change.
                // E.g. entering lockers would do it.
                if (transform.ActivelyLerping
                    && transform.LerpParent == transform.ParentUid
                    && transform.ParentUid.IsValid()
                    && !transform.Deleted)
                {
                    if (transform.NextPosition != null)
                    {
                        var lerpDest = transform.NextPosition.Value;
                        var lerpSource = transform.PrevPosition;
                        var distance = (lerpDest - lerpSource).LengthSquared();

                        if (distance is > MinInterpolationDistanceSquared and < MaxInterpolationDistanceSquared)
                        {
                            SetLocalPositionNoLerp(uid, Vector2.Lerp(lerpSource, lerpDest, step), transform);
                            transform.NextPosition = lerpDest;
                            found = true;
                        }
                    }

                    if (transform.NextRotation != null)
                    {
                        var lerpDest = transform.NextRotation.Value;
                        var lerpSource = transform.PrevRotation;
                        SetLocalRotationNoLerp(uid, Angle.Lerp(lerpSource, lerpDest, step), transform);
                        transform.NextRotation = lerpDest;
                        found = true;
                    }
                }

                // Transforms only get removed from the lerp list if they no longer are in here.
                // This is much easier than having the transform itself tell us to remove it.
                if (!found)
                {
                    // Transform is no longer lerping, remove.
                    transform.ActivelyLerping = false;
                    _lerpingTransforms.RemoveSwap(i);
                    i -= 1;
                }
            }
        }
    }
}
