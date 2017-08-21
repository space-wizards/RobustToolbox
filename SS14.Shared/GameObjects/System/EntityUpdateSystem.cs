using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Shared.GameObjects.System
{
    /// <summary>
    /// Entity system that calls <see cref="IEntity.Update"> on every entity every frame.
    /// </summary>
    public class EntityUpdateSystem : EntitySystem
    {
        public EntityUpdateSystem()
        {
            EntityQuery = new AllEntityQuery();
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in EntityManager.GetEntities(EntityQuery))
            {
                entity.Update(frameTime);
            }
        }
    }
}
