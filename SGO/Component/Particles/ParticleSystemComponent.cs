using System;
using System.Drawing;
using GameObject;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Light;
using SS13_Shared.GO.Component.Particles;
using ServerInterfaces.Chat;

namespace SGO
{
    public class ParticleSystemComponent : Component
    {
        private Vector4 _startColor = new Vector4(255, 0, 0, 0);
        private Vector4 _endColor = new Vector4(0, 0, 0, 0);
        private bool _active = false;

        public ParticleSystemComponent()
        {
            Family = ComponentFamily.Particles;
        }
        
        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);
            switch (parameter.MemberName)
            {
                case "colorStart":
                    _startColor = parameter.GetValue<Vector4>();
                    break;
                case "colorEnd":
                    _endColor = parameter.GetValue<Vector4>();
                    break;
                case "startActive":
                    _active = parameter.GetValue<bool>();
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return reply;

            switch (type)
            {
                case ComponentMessageType.Activate:
                    HandleClickedInHand();
                    break;
            }

            return reply;
        }

        private void HandleClickedInHand()
        {
            _active = !_active;
        }
        
        public override ComponentState GetComponentState()
        {
            return new ParticleSystemComponentState(_active, _startColor, _endColor);
        }
    }
}