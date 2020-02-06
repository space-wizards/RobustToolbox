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

            // So we could calculate the correct size of the entities based on the contents of their sprite...
            // Or we can just assume that no entity is larger than 10x10 and get a stupid easy check.
            // TODO: Make this check more accurate.
            var worldBounds = Box2.CenteredAround(eye.Position.Position,
                _clyde.ScreenSize / EyeManager.PIXELSPERMETER * eye.Zoom).Enlarged(5);

            var mapEntity = _mapManager.GetMapEntityId(eye.Position.MapId);

            var parentMatrix = Matrix3.Identity;
            RunUpdatesRecurse(frameTime, worldBounds, EntityManager.GetEntity(mapEntity), ref parentMatrix);
        }

        private void RunUpdatesRecurse(float frameTime, Box2 bounds, IEntity entity, ref Matrix3 parentMatrix)
        {
            var localMatrix = entity.Transform.GetLocalMatrix();
            Matrix3.Multiply(ref localMatrix, ref parentMatrix, out var matrix);

            foreach (var childUid in entity.Transform.ChildEntityUids)
            {
                var child = EntityManager.GetEntity(childUid);
                if (child.TryGetComponent(out ISpriteComponent sprite))
                {
                    var worldPosition = Matrix3.Transform(matrix, child.Transform.LocalPosition);

                    if (!sprite.IsInert && bounds.Contains(worldPosition))
                    {
                        sprite.FrameUpdate(frameTime);
                    }
                }

                if (child.Transform.ChildCount != 0)
                {
                    RunUpdatesRecurse(frameTime, bounds, child, ref matrix);
                }
            }
        }
    }
}
