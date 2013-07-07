using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Light;
using ServerInterfaces.Chat;

namespace SGO
{
    public class LightComponent : GameObjectComponent
    {
        private LightState _state = LightState.On;
        private int _colorR = 200;
        private int _colorG = 200;
        private int _colorB = 200;
        private LightModeClass _mode = LightModeClass.Constant;

        public LightComponent()
        {
            family = ComponentFamily.Light;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch(parameter.MemberName)
            {
                case "startState":
                    _state = (LightState)Enum.Parse(typeof(LightState), parameter.GetValue<string>(), true);
                    break;
                case "lightColorR":
                    _colorR = int.Parse(parameter.GetValue<string>());
                    break;
                case "lightColorG":
                    _colorG = int.Parse(parameter.GetValue<string>());
                    break;
                case "lightColorB":
                    _colorB = int.Parse(parameter.GetValue<string>());
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return reply;

            switch (type)
            {
                case ComponentMessageType.Die:
                    SetState(LightState.Broken);
                    break;
                case ComponentMessageType.Activate:
                    HandleClickedInHand();
                    break;
            }

            return reply;
        }

        private void HandleClickedInHand()
        {
            switch (_state)
            {
                case LightState.On:
                    SetState(LightState.Off);
                    break;
                case LightState.Off:
                    SetState(LightState.On);
                    break;
                case LightState.Broken:
                    IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Damage, "You fiddle with it, but nothing happens. It must be broken.", Owner.Name, Owner.Uid);
                    break;
            }
        }

        private void SetState(LightState state)
        {
            _state = state;
            SendState();
        }

        private void SendState()
        {
            //Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null, ComponentMessageType.SetLightState, _state);
        }

        public override void  HandleInstantiationMessage(NetConnection netConnection)
        {
 	        base.HandleInstantiationMessage(netConnection);

            //Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, netConnection, ComponentMessageType.SetLightState, _state);
        }

        public override ComponentState GetComponentState()
        {
            return new LightComponentState(_state, _colorR, _colorG, _colorB, _mode);
        }
    }
}
