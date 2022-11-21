using System;
using System.Collections.Generic;
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

        private const double MinInterpolationAngle = Math.PI / 720;

        // 45 degrees.
        private const double MaxInterpolationAngle = Math.PI / 4;

        [Dependency] private readonly IGameTiming _gameTiming = default!;

        // Only keep track of transforms actively lerping.
        // Much faster than iterating 3000+ transforms every frame.
        [ViewVariables] private readonly List<TransformComponent> _lerpingTransforms = new();

        public void Reset()
        {
            foreach (var xform in _lerpingTransforms)
            {
                xform.ActivelyLerping = false;
                xform.NextPosition = null;
                xform.NextRotation = null;
                xform.LerpParent = EntityUid.Invalid;
            }
            _lerpingTransforms.Clear();
        }

        public override void ActivateLerp(TransformComponent xform)
        {
            if (xform.ActivelyLerping)
                return;

            xform.ActivelyLerping = true;
            _lerpingTransforms.Add(xform);
        }

        public override void DeactivateLerp(TransformComponent component)
        {
            // this should cause the lerp to do nothing
            component.NextPosition = null;
            component.NextRotation = null;
            component.LerpParent = EntityUid.Invalid;
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            var step = (float) (_gameTiming.TickRemainder.TotalSeconds / _gameTiming.TickPeriod.TotalSeconds);

            for (var i = 0; i < _lerpingTransforms.Count; i++)
            {
                var transform = _lerpingTransforms[i];
                var found = false;

                // Only lerp if parent didn't change.
                // E.g. entering lockers would do it.
                if (transform.LerpParent == transform.ParentUid
                    && transform.ParentUid.IsValid()
                    && !transform.Deleted)
                {
                    if (transform.NextPosition != null)
                    {
                        var lerpDest = transform.NextPosition.Value;
                        var lerpSource = transform.PrevPosition;
                        var distance = (lerpDest - lerpSource).LengthSquared;

                        if (distance is > MinInterpolationDistanceSquared and < MaxInterpolationDistanceSquared)
                        {
                            transform.LocalPosition = Vector2.Lerp(lerpSource, lerpDest, step);
                            // Setting LocalPosition clears LerpPosition so fix that.
                            transform.NextPosition = lerpDest;
                            found = true;
                        }
                    }

                    if (transform.NextRotation != null)
                    {
                        var lerpDest = transform.NextRotation.Value;
                        var lerpSource = transform.PrevRotation;
                        var distance = Math.Abs(Angle.ShortestDistance(lerpDest, lerpSource));

                        if (distance is > MinInterpolationAngle and < MaxInterpolationAngle)
                        {
                            transform.LocalRotation = Angle.Lerp(lerpSource, lerpDest, step);
                            // Setting LocalRotation clears LerpAngle so fix that.
                            transform.NextRotation = lerpDest;
                            found = true;
                        }
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
