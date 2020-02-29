using JetBrains.Annotations;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects.EntitySystems
{
    /// <summary>
    /// Updates the layer animation for every visible sprite.
    /// </summary>
    [UsedImplicitly]
    public class SpriteSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IEyeManager _eyeManager;
#pragma warning restore 649

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            // So we could calculate the correct size of the entities based on the contents of their sprite...
            // Or we can just assume that no entity is larger than 10x10 and get a stupid easy check.
            var pvsBounds = _eyeManager.GetWorldViewport().Enlarged(5);

            var pvsEntities = EntityManager.GetEntitiesIntersecting(_eyeManager.CurrentMap, pvsBounds, true);

            foreach (var entity in pvsEntities)
            {
                if (!entity.TryGetComponent(out ISpriteComponent sprite))
                    continue;

                if (sprite.IsInert)
                    continue;

                sprite.FrameUpdate(frameTime);
            }
        }
    }
}
