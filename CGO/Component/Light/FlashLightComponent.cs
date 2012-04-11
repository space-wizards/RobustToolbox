using System;
using System.Drawing;
using ClientInterfaces.GOC;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using GorgonLibrary;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class FlashLightComponent : PointLightComponent
    {
        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return reply;

            switch(type)
            {
                case ComponentMessageType.MoveDirection:
                    var movedir = (Constants.MoveDirs) list[0];
                    LightMoveDir(movedir);
                    break;
            }

            return reply;
        }

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);

            _light.SetState(LightState.Off);
        }

        private void LightMoveDir(Constants.MoveDirs movedir)
        {
            switch (movedir)
            {
                case Constants.MoveDirs.east:
                    SetMask("flashlight_mask");
                    _light.LightArea.Rot90 = true;
                    _light.LightArea.MaskFlipX = true;
                    _light.LightArea.MaskFlipY = false;
                    _light.LightArea.Calculated = false;
                    break;
                case Constants.MoveDirs.southeast:
                    SetMask("flashlight_mask_diag");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = false;
                    _light.LightArea.MaskFlipY = true;
                    _light.LightArea.Calculated = false;
                    break;
                case Constants.MoveDirs.north:
                    SetMask("flashlight_mask");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = false;
                    _light.LightArea.MaskFlipY = false;
                    _light.LightArea.Calculated = false;
                    break;
                case Constants.MoveDirs.northeast:
                    SetMask("flashlight_mask_diag");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = false;
                    _light.LightArea.MaskFlipY = false;
                    _light.LightArea.Calculated = false;
                    break;
                case Constants.MoveDirs.west:
                    SetMask("flashlight_mask");
                    _light.LightArea.Rot90 = true;
                    _light.LightArea.MaskFlipX = false;
                    _light.LightArea.MaskFlipY = false;
                    _light.LightArea.Calculated = false;
                    break;
                case Constants.MoveDirs.northwest:
                    SetMask("flashlight_mask_diag");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = true;
                    _light.LightArea.MaskFlipY = false;
                    _light.LightArea.Calculated = false;
                    break;
                case Constants.MoveDirs.south:
                    SetMask("flashlight_mask");
                    _light.LightArea.Rot90 = false;
                    _light.LightArea.MaskFlipX = false;
                    _light.LightArea.MaskFlipY = true;
                    _light.LightArea.Calculated = false;
                    break;
                case Constants.MoveDirs.southwest:
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
