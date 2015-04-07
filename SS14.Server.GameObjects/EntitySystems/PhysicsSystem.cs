using SS14.Server.Interfaces.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.GO;


namespace SS14.Server.GameObjects.EntitySystems
{
    internal class PhysicsSystem : EntitySystem
    {
        public PhysicsSystem(EntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.AllSet.Add(typeof(PhysicsComponent));
            EntityQuery.AllSet.Add(typeof(VelocityComponent));
            EntityQuery.AllSet.Add(typeof(TransformComponent));
            EntityQuery.Exclusionset.Add(typeof(SlaveMoverComponent));
            EntityQuery.Exclusionset.Add(typeof(PlayerInputMoverComponent));
        }

        public override void Update(float frametime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach(var entity in entities)
            {
                //GasEffect(entity, frametime);

                var transform = entity.GetComponent<TransformComponent>(ComponentFamily.Transform);
                var velocity = entity.GetComponent<VelocityComponent>(ComponentFamily.Velocity);

                if (velocity.Velocity.Magnitude < 0.001f)
                    continue;
                //Decelerate
                velocity.Velocity -= (velocity.Velocity * (frametime * 0.01f));

                var movement = velocity.Velocity * frametime;
                //Apply velocity
                transform.Position += movement;
            }
        }

        //private void GasEffect(Entity entity, float frameTime)
        //{
        //    var transform = entity.GetComponent<TransformComponent>(ComponentFamily.Transform);
        //    var physics = entity.GetComponent<PhysicsComponent>(ComponentFamily.Physics);
        //    ITile t =
        //        IoCManager.Resolve<IMapManager>().GetFloorAt(transform.Position);
        //    if (t == null)
        //        return;
        //    var gasVel = t.GasCell.GasVelocity;
        //    if (gasVel.Abs() > physics.Mass) // Stop tiny wobbles
        //    {
        //        transform.Position = new Vector2(transform.X + (gasVel.X * frameTime), transform.Y + (gasVel.Y * frameTime));
        //    }
        //}
    }
}
