
namespace SS14.Shared.GameObjects.System
{
    public abstract class EntityProcessingSystem : EntitySystem
    {
        public EntityProcessingSystem(EntityManager em, EntitySystemManager esm):base(em, esm)
        {}

        public override void Update(float frameTime)
        {}
    }
}