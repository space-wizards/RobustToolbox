using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CGO;
using ClientInterfaces.Map;
using GameObject;
using GameObject.System;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using EntityManager = GameObject.EntityManager;

namespace CGO.EntitySystems
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
        }

        public override void Update(float frametime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach(var entity in entities)
            {
                var transform = entity.GetComponent<TransformComponent>(ComponentFamily.Transform);
                var velocity = entity.GetComponent<VelocityComponent>(ComponentFamily.Velocity);
                
                //Decelerate
                velocity.Velocity -= (velocity.Velocity * (frametime * 0.01f));

                var movement = velocity.Velocity*frametime;

                var mover = entity.GetComponent<KeyBindingMoverComponent>(ComponentFamily.Mover);
                if(mover != null && movement.Length > 0.001f)
                {
                    mover.Translate(movement);
                } 
                /*else
                {
                    //Apply velocity
                    transform.Position += movement;
                }*/
            }
        }

    }
}
