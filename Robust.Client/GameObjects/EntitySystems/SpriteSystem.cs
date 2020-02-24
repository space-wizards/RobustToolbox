using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects.EntitySystems
{
    public class SpriteSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IClyde _clyde;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public SpriteSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(ISpriteComponent));
        }

        public override void FrameUpdate(float frameTime)
        {
            var eye = _eyeManager.CurrentEye;

            var worldBounds = _eyeManager.GetWorldViewport().Enlarged(1f);

            var entities = EntityManager.GetEntitiesIntersecting(_eyeManager.CurrentMap, worldBounds, true);

            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent(out ISpriteComponent sprite))
                {
                    continue;
                }

                if (sprite.IsInert)
                {
                    continue;
                }

                sprite.FrameUpdate(frameTime);
            }
        }
    }
}
