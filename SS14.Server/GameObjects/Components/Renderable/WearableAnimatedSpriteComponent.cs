using SS14.Shared.GameObjects;

namespace SS14.Server.GameObjects
{
    public class WearableAnimatedSpriteComponent : AnimatedSpriteComponent
    {
        public override string Name => "WearableAnimatedSprite";
        public override uint? NetID => NetIDs.WEARABLE_ANIMATED_SPRITE;

        public bool IsCurrentlyWorn { get; set; } = false;
        public bool IsCurrentlyCarried { get; set; } = false;
        
        public override ComponentState GetComponentState()
        {
            var masterUid = master?.Owner.Uid;
            return new WearableAnimatedSpriteComponentState(IsCurrentlyWorn, IsCurrentlyCarried, Visible, DrawDepth, SpriteName, CurrentAnimation, Loop, masterUid);
        }
    }
}
