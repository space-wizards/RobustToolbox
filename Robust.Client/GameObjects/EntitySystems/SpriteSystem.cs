using JetBrains.Annotations;
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
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            var renderTreeSystem = EntitySystemManager.GetEntitySystem<RenderingTreeSystem>();

            // So we could calculate the correct size of the entities based on the contents of their sprite...
            // Or we can just assume that no entity is larger than 10x10 and get a stupid easy check.
            var pvsBounds = _eyeManager.GetWorldViewport().Enlarged(5);

            var mapTree = renderTreeSystem.GetSpriteTreeForMap(_eyeManager.CurrentMap);

            var pvsEntities = mapTree.Query(pvsBounds, true);

            foreach (var sprite in pvsEntities)
            {
                if (sprite.IsInert)
                {
                    continue;
                }

                sprite.FrameUpdate(frameTime);
            }
        }
    }
}
