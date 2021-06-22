using System;
using System.Linq;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     An optimisation component for stuff that should be set as collidable when it's awake and non-collidable when asleep.
    /// </summary>
    public sealed class CollisionWakeComponent : Component
    {
        public override string Name => "CollisionWake";
        public override uint? NetID => NetIDs.COLLISION_WAKE;

        [DataField("enabled")]
        private bool _enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (value == _enabled) return;

                _enabled = value;
                Dirty();
                RaiseStateChange();
            }
        }

        internal void RaiseStateChange()
        {
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new CollisionWakeStateMessage(), false);
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            if (Owner.TryGetComponent(out IPhysBody? body))
            {
                body.CanCollide = true;
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new CollisionWakeState(Enabled);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not CollisionWakeState state) return;

            Enabled = state.Enabled;
        }

        [Serializable, NetSerializable]
        public class CollisionWakeState : ComponentState
        {
            public bool Enabled { get; }

            public CollisionWakeState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }
}
