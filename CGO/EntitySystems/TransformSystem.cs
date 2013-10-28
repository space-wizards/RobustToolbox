using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ClientInterfaces.Configuration;
using ClientInterfaces.GameTimer;
using GameObject;
using GameObject.System;
using GorgonLibrary;
using SS13.IoC;
using SS13_Shared;
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
            //Interp constant -- determines how far back in time to interpolate from
            var interpolation = IoCManager.Resolve<IConfigurationManager>().GetInterpolation();
            foreach (var entity in entities)
            {
                var transform = entity.GetComponent<TransformComponent>(ComponentFamily.Transform);
                bool haskbMover = entity.GetComponent<KeyBindingMoverComponent>(ComponentFamily.Mover) != null;
                var currentTime = IoCManager.Resolve<IGameTimer>().CurrentTime - interpolation;
                Vector2D newPosition;
                if (transform.lerpStateTo == transform.lerpStateFrom || currentTime > transform.lerpStateTo.ReceivedTime || currentTime < transform.lerpStateFrom.ReceivedTime)
                {
                    newPosition = new Vector2D(transform.lerpStateTo.X, transform.lerpStateTo.Y);
                }
                else
                {
                    var p1 = new Vector2D(transform.lerpStateFrom.X, transform.lerpStateTo.Y);
                    var p2 = new Vector2D(transform.lerpStateTo.X, transform.lerpStateTo.Y);
                    var t1 = transform.lerpStateFrom.ReceivedTime;
                    var t2 = transform.lerpStateTo.ReceivedTime;
                    var lerp = (currentTime - t1)/(t2 - t1);
                    newPosition = Interpolate(p1, p2, lerp, false);
                    if(haskbMover)
                        newPosition = EaseExponential(currentTime - t1, transform.Position, newPosition, t2 - t1);
                }
                if ((newPosition - transform.Position).Length > 0.01f &&
                    (!haskbMover || (newPosition - transform.Position).Length > 3 * interpolation * KeyBindingMoverComponent.FastMoveSpeed))
                {
                    transform.TranslateTo(newPosition);
                    if(haskbMover)
                    {
                        entity.GetComponent<KeyBindingMoverComponent>(ComponentFamily.Mover).SendPositionUpdate(newPosition);
                    }
                }

            }
        }

        private Vector2D EaseExponential(float time, Vector2D v1, Vector2D v2, float duration)
        {
            var dx = (v2.X - v1.X);
            var x = EaseExponential(time, v1.X, dx, duration);
            
            var dy = (v2.Y - v1.Y);
            var y = EaseExponential(time, v1.Y, dy, duration);
            return new Vector2D(x,y);
        }

        private float EaseExponential(float t, float b, float c, float d)
        {
            return c * ((float)-Math.Pow(2, -10 * t / d) + 1) + b;
        }

        private Vector2D Interpolate(Vector2D v1, Vector2D v2, float control, bool allowExtrapolation)
        {
            if (!allowExtrapolation && (control > 1 || control < 0))
            {
                // Error message includes information about the actual value of the argument
                throw new ArgumentOutOfRangeException
                    (
                    "control",
                    control,
                    "Control parameter must be a value between 0 & 1\nThe argument provided has a value of " + control
                    );
            }
            else
            {
                return
                    (
                        new Vector2D
                            (
                            v1.X * (1 - control) + v2.X * control,
                            v1.Y * (1 - control) + v2.Y * control
                            )
                    );
            }
        }
    }
}
