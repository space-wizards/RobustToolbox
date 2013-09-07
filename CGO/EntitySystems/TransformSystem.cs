using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ClientInterfaces.GameTimer;
using GameObject;
using GameObject.System;
using SS13.IoC;
using SS13_Shared.GO;

namespace CGO.EntitySystems
{
    internal class TransformSystem : EntitySystem
    {
        public TransformSystem(EntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.AllSet.Add(typeof(TransformComponent));
            EntityQuery.Exclusionset.Add(typeof(SlaveMoverComponent));
        }

        public override void Update(float frametime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
            {
                var transform = entity.GetComponent<TransformComponent>(ComponentFamily.Transform);
                var currentTime = IoCManager.Resolve<IGameTimer>().CurrentTime;
                var deltaTime = currentTime - transform.LerpTime;
                if (transform.ToTime - transform.LerpTime <= 0.001f) // If there is no lerping to be done for whatever reason
                {
                    var diff = (transform.ToPosition - transform.Position).Length;
                    if (entity.GetComponent<KeyBindingMoverComponent>(ComponentFamily.Mover) != null)
                    {
                        if (diff > deltaTime * KeyBindingMoverComponent.FastMoveSpeed * 2) //If we're really off
                            transform.TranslateTo(transform.ToPosition);
                    }
                    else if (diff > 0.1f) 
                    {
                            transform.TranslateTo(transform.ToPosition);
                    }
                }
                else
                {
                    var lerpVelocity = (transform.ToPosition - transform.LerpPosition)/
                                       (transform.ToTime - transform.LerpTime);
                    var lerpedPosition = transform.LerpPosition + lerpVelocity*deltaTime;
                    //Calculate lerped position
                    /*var lerpVelocity = (transform.ToPosition - transform.LerpPosition)/transform.LerpTime;
                    var lerpPosition = transform.LerpPosition + lerpVelocity*transform.LerpClock;*/
                    var diff = (lerpedPosition - transform.Position).Length;
                    if(entity.GetComponent<KeyBindingMoverComponent>(ComponentFamily.Mover) != null)
                    {
                        if (diff > deltaTime * KeyBindingMoverComponent.FastMoveSpeed * 2) //If we're really off
                            transform.TranslateTo(lerpedPosition);
                    }
                    else if(diff > 0.1f)
                    {
                        transform.TranslateTo(lerpedPosition);
                    }
                }

            }
        }
    }
}
