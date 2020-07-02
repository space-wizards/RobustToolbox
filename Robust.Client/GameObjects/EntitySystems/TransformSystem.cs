using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects.EntitySystems
{
    /// <summary>
    ///     Handles interpolation of transform positions.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TransformSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        // Only keep track of transforms actively lerping.
        // Much faster than iterating 3000+ transforms every frame.
        [ViewVariables]
        private readonly List<TransformComponent> _lerpingTransforms = new List<TransformComponent>();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TransformStartLerpMessage>(TransformStartLerpHandler);
        }

        private void TransformStartLerpHandler(TransformStartLerpMessage ev)
        {
            _lerpingTransforms.Add(ev.Transform);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            var step = (float) (_gameTiming.TickRemainder.TotalSeconds / _gameTiming.TickPeriod.TotalSeconds);

            for (var i = 0; i < _lerpingTransforms.Count; i++)
            {
                var transform = _lerpingTransforms[i];
                var found = false;

                if (transform.LerpDestination != null)
                {
                    var oldNext = transform.LerpDestination.Value;
                    transform.LocalPosition = Vector2.Lerp(transform.LerpSource, oldNext, step);
                    transform.LerpDestination = oldNext;
                    found = true;
                }

                if (transform.LerpAngle != null)
                {
                    var oldNext = transform.LerpAngle.Value;
                    transform.LocalRotation = Angle.Lerp(transform.LerpSourceAngle, oldNext, step);
                    transform.LerpAngle = oldNext;
                    found = true;
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
