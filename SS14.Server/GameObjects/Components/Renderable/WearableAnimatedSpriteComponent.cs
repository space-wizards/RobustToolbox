using SS14.Shared.GameObjects;

namespace SS14.Server.GameObjects
{
    public class WearableAnimatedSpriteComponent : AnimatedSpriteComponent
    {
        public override string Name => "WearableAnimatedSprite";
        public override uint? NetID => NetIDs.WEARABLE_ANIMATED_SPRITE;

        public bool IsCurrentlyWorn { get; set; } = false;
        public bool IsCurrentlyCarried { get; set; } = false;

        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                      params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.ItemEquipped:
                    IsCurrentlyWorn = true;
                    IsCurrentlyCarried = false;
                    break;
                case ComponentMessageType.ItemUnEquipped:
                    IsCurrentlyWorn = false;
                    break;
                case ComponentMessageType.Dropped:
                    IsCurrentlyCarried = false;
                    break;
                case ComponentMessageType.PickedUp:
                    IsCurrentlyCarried = true;
                    break;
            }

            return reply;
        }

        public override ComponentState GetComponentState()
        {
            var masterUid = master?.Owner.Uid;
            return new WearableAnimatedSpriteComponentState(IsCurrentlyWorn, IsCurrentlyCarried, Visible, DrawDepth, SpriteName, CurrentAnimation, Loop, masterUid);
        }
    }
}
