using SFML.System;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Receives movement data from the server and updates the entity's position accordingly.
    /// </summary>
    public class BasicMoverComponent : ClientComponent, IMoverComponent
    {
        public override string Name => "BasicMover";
        public override uint? NetID => NetIDs.BASIC_MOVER;
        public override bool NetworkSynchronizeExistence => true;

        private bool interpolating;
        private float movedtime; // Amount of time we've been moving since the last update packet.
        private const float movetime = 0.05f; // Milliseconds it should take to move.
        private Vector2f startPosition;
        private Vector2f targetPosition;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (interpolating)
            {
                movedtime = movedtime + frameTime;
                if (movedtime >= movetime)
                {
                    Owner.GetComponent<ITransformComponent>().Position = targetPosition;
                    startPosition = targetPosition;
                    interpolating = false;
                }
                else
                {
                    float X = Ease(movedtime, startPosition.X, targetPosition.X, movetime);
                    float Y = Ease(movedtime, startPosition.Y, targetPosition.Y, movetime);
                    Owner.GetComponent<ITransformComponent>().Position = new Vector2f(X, Y);
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
            time = time / duration; // - 1;
            return time * (end - start) + start;
        }
    }
}
