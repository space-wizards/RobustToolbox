using JetBrains.Annotations;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Updates the position of every Eye every frame, so that the camera follows the player around.
    /// </summary>
    [UsedImplicitly]
    public sealed class EyeUpdateSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            // Make sure this runs *after* entities have been moved by interpolation and movement.
            UpdatesAfter.Add(typeof(TransformSystem));
            UpdatesAfter.Add(typeof(PhysicsSystem));
        }

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            var query = AllEntityQuery<EyeComponent>();

            while (query.MoveNext(out var uid, out var eyeComponent))
            {
                if (eyeComponent._eye == null)
                    continue;

                if (!TryComp<TransformComponent>(eyeComponent.Target, out var xform))
                {
                    xform = Transform(uid);
                    eyeComponent.Target = null;
                }

                eyeComponent._eye.Position = xform.MapPosition;
            }
        }
    }
}
