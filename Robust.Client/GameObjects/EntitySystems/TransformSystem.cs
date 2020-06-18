using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects.EntitySystems
{
    /// <summary>
    ///     Handles interpolation of transform positions.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TransformSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery<TransformComponent>();
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            var step = (float) (_gameTiming.TickRemainder.TotalSeconds / _gameTiming.TickPeriod.TotalSeconds);

            foreach (var entity in RelevantEntities)
            {
                var transform = (TransformComponent) entity.Transform;

                if (transform.LerpDestination != null)
                {
                    var oldNext = transform.LerpDestination.Value;
                    transform.LocalPosition = Vector2.Lerp(transform.LerpSource, oldNext, step);
                    transform.LerpDestination = oldNext;
                }

                if (transform.LerpAngle != null)
                {
                    var oldNext = transform.LerpAngle.Value;
                    transform.LocalRotation = Angle.Lerp(transform.LerpSourceAngle, oldNext, step);
                    transform.LerpAngle = oldNext;
                }
            }
        }
    }
}
