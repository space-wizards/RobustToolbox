using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;

namespace SS14.Server.GameObjects.EntitySystems
{
    class InputSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery  = new TypeEntityQuery(typeof(PlayerInputMoverComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<PlayerInputMoverComponent>();
                comp.OnUpdate();
            }
        }
    }
}
