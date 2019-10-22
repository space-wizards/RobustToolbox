using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects.EntitySystems
{
    public class SpriteSystem : EntitySystem
    {
        [Dependency] private readonly IClyde _clyde;
        [Dependency] private readonly IEyeManager _eyeManager;

        public SpriteSystem()
        {
            EntityQuery = new TypeEntityQuery(typeof(ISpriteComponent));
        }

        public override void FrameUpdate(float frameTime)
        {
            var eye = _eyeManager.CurrentEye;

            // So we could calculate the correct size of the entities based on the contents of their sprite...
            // Or we can just assume that no entity is larger than 10x10 and get a stupid easy check.
            // TODO: Make this check more accurate.
            var worldBounds = Box2.CenteredAround(eye.Position.Position,
                _clyde.ScreenSize / EyeManager.PIXELSPERMETER * eye.Zoom).Enlarged(5);

            foreach (var entity in EntityManager.GetEntities(EntityQuery))
            {
                var transform = entity.Transform;
                if (!worldBounds.Contains(transform.WorldPosition))
                {
                    continue;
                }

                // TODO: Don't call this on components without RSIs loaded.
                // Serious performance benefit here.
                entity.GetComponent<ISpriteComponent>().FrameUpdate(frameTime);
            }
        }
    }
}
