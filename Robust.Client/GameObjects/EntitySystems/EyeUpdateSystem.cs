using System;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

#nullable enable

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
            foreach (var eyeComponent in EntityManager.EntityQuery<EyeComponent>(true))
            {
                eyeComponent.UpdateEyePosition();
            }
        }
    }
}
