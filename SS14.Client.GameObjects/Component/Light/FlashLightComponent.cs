using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;

namespace SS14.Client.GameObjects
{
    public class FlashLightComponent : PointLightComponent
    {
        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return reply;

            switch (type)
            {
                case ComponentMessageType.MoveDirection:
                    var movedir = (Direction) list[0];
                    LightMoveDir(movedir);
                    break;
            }

            return reply;
        }

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);

            _light.SetState(LightState.Off);
        }

        private void LightMoveDir(Direction movedir)
        {
            switch (movedir)
            {
                case Direction.East:
                    SetMask("flashlight_mask");
                    _light.LightArea.Rot90 = true;
                    _light.LightArea.MaskFlipX = true;
                    _light.LightArea.MaskFlipY = false;
                    _light.LightArea.Calculated = false;
                    break;
                case Direction.SouthEast:
                    SetMask("flashlight_mask_diag");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = false;
                    _light.LightArea.MaskFlipY = true;
                    _light.LightArea.Calculated = false;
                    break;
                case Direction.North:
                    SetMask("flashlight_mask");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = false;
                    _light.LightArea.MaskFlipY = false;
                    _light.LightArea.Calculated = false;
                    break;
                case Direction.NorthEast:
                    SetMask("flashlight_mask_diag");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = false;
                    _light.LightArea.MaskFlipY = false;
                    _light.LightArea.Calculated = false;
                    break;
                case Direction.West:
                    SetMask("flashlight_mask");
                    _light.LightArea.Rot90 = true;
                    _light.LightArea.MaskFlipX = false;
                    _light.LightArea.MaskFlipY = false;
                    _light.LightArea.Calculated = false;
                    break;
                case Direction.NorthWest:
                    SetMask("flashlight_mask_diag");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = true;
                    _light.LightArea.MaskFlipY = false;
                    _light.LightArea.Calculated = false;
                    break;
                case Direction.South:
                    SetMask("flashlight_mask");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = false;
                    _light.LightArea.MaskFlipY = true;
                    _light.LightArea.Calculated = false;
                    break;
                case Direction.SouthWest:
                    SetMask("flashlight_mask_diag");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = true;
                    _light.LightArea.MaskFlipY = true;
                    _light.LightArea.Calculated = false;
                    break;
            }
        }
    }
}