using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects
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

            var currentMap = _eyeManager.CurrentMap;
            if (currentMap == MapId.Nullspace)
            {
                return;
            }

            var mapTree = renderTreeSystem.GetSpriteTreeForMap(currentMap);

            mapTree.QueryAabb(ref frameTime, (ref float state, in SpriteComponent value) =>
            {
                if (value.IsInert)
                {
                    return true;
                }

                value.FrameUpdate(state);
                return true;
            }, pvsBounds, approx: true);
        }
    }
}
