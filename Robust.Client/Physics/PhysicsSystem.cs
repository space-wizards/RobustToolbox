using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.Physics
{
    [UsedImplicitly]
    public class PhysicsSystem: EntitySystem
    {
        private float _lastServerMsg;

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery<PhysicsComponent>();
        }

        public override void Update(float frameTime)
        {

        }
    }
}
