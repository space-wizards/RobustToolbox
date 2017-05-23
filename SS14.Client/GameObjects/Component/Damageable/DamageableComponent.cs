using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Damageable;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Basic damageable component only tracks whether its dead or not
    /// </summary>
    [IoCTarget]
    [Component("Damageable")]
    public class DamageableComponent : Component
    {
        //Used for things that are binary. Either broken or not broken. (windows?)
        protected bool IsDead;

        public DamageableComponent()
        {
            Family = ComponentFamily.Damageable;
        }

        public override Type StateType
        {
            get { return typeof (DamageableComponentState); }
        }

        protected virtual void Die()
        {
            if (IsDead) return;

            IsDead = true;
            Owner.SendMessage(this, ComponentMessageType.Die);
        }

        public override void HandleComponentState(dynamic state)
        {
            dynamic newIsDeadState = state.IsDead;

            if (newIsDeadState && IsDead == false)
                Die();
        }
    }
}
