using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class TestSystem : EntitySystem
    {
        public TestSystem()
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof(BasicItemComponent));
        }

        public override void Update(float frametime)
        {
        }
    }
}
