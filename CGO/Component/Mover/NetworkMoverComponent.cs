using System;
using GameObject;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Mover;

namespace CGO
{
    /// <summary>
    /// Recieves movement data from the server and updates the entity's position accordingly.
    /// </summary>
    public class NetworkMoverComponent : Component
    {
        private bool interpolating;
        private float movedtime; // Amount of time we've been moving since the last update packet.
        private float movetime = 0.05f; // Milliseconds it should take to move.
        private Vector2D startPosition;
        private Vector2D targetPosition;

        public NetworkMoverComponent()
        {
            Family = ComponentFamily.Mover;
        }

        public override Type StateType
        {
            get { return typeof (MoverComponentState); }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (interpolating)
            {
                movedtime = movedtime + frameTime;
                if (movedtime >= movetime)
                {
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = targetPosition;
                    startPosition = targetPosition;
                    interpolating = false;
                }
                else
                {
                    float X = Ease(movedtime, startPosition.X, targetPosition.X, movetime);
                    float Y = Ease(movedtime, startPosition.Y, targetPosition.Y, movetime);
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = new Vector2D(X, Y);
                }

            }
        }

        /// <summary>
        /// Returns a float position eased from a start position to an end position.
        /// </summary>
        /// <param name="time">elapsed time since the start of the easing</param>
        /// <param name="start">start position</param>
        /// <param name="end">end position</param>
        /// <param name="duration">duration of the movement</param>
        /// <returns>current position</returns>
        private float Ease(float time, float start, float end, float duration = 1) // duration is in ms.
        {
            time = time/duration; // - 1;
            return time*(end - start) + start;
        }
    }
}