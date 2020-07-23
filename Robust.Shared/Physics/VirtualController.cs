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
        private Vector2 _linVelocity;

        /// <summary>
        ///     Current contribution to the linear velocity of the entity in meters per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public virtual Vector2 LinearVelocity
        {
            get => _linVelocity;
            set
            {
                if (_linVelocity == value)
                    return;

                _linVelocity = value;
                ControlledComponent?.Dirty();
            }
        }

        public virtual ICollidableComponent? ControlledComponent { protected get; set; }


        public virtual void Stop()
        {
            LinearVelocity = Vector2.Zero;
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
