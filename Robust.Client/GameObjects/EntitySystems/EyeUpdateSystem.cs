using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.GameObjects.EntitySystems
{
    /// <summary>
    /// Updates the position of every Eye every frame, so that the camera follows the player around.
    /// </summary>
    [UsedImplicitly]
    internal class EyeUpdateSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(EyeComponent));
        }

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var eyeComp = entity.GetComponent<EyeComponent>();
                eyeComp.UpdateEyePosition();
            }
        }
    }
}
