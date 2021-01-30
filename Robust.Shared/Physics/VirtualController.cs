using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     The VirtualController allows dynamic changes in the properties of a physics component, usually to simulate a complex physical interaction (such as player movement).
    /// </summary>
    public abstract class VirtualController
    {
        private Vector2 _linearVelocity;

        /// <summary>
        ///     Current contribution to the linear velocity of the entity in meters per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Vector2 LinearVelocity
        {
            get => _linearVelocity;
            set
            {
                if (value != Vector2.Zero)
                    ControlledComponent?.WakeBody();

                if (_linearVelocity.EqualsApprox(value, 0.0001))
                    return;

                _linearVelocity = value;
                ControlledComponent?.Dirty();
            }
        }

        public Vector2 Impulse
        {
            get => _impulse;
            set
            {
                if (value != Vector2.Zero)
                    ControlledComponent?.WakeBody();

                if (_impulse.EqualsApprox(value, 0.0001))
                    return;

                _impulse = value;
                ControlledComponent?.Dirty();
            }
        }

        private Vector2 _impulse;

        public virtual PhysicsComponent? ControlledComponent { protected get; set; }

        /// <summary>
        ///     Tries to set this controller's linear velocity to zero.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        public virtual bool Stop()
        {
            LinearVelocity = Vector2.Zero;
            return true;
        }

        /// <summary>
        ///     Modify a physics component before processing impulses
        /// </summary>
        public virtual void UpdateBeforeProcessing() { }

        /// <summary>
        ///     Modify a physics component after processing impulses
        /// </summary>
        public virtual void UpdateAfterProcessing() { }
    }
}
