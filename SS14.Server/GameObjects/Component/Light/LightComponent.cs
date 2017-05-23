using SS14.Server.Interfaces.Chat;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Light;
using SS14.Shared.IoC;
using System;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    [IoCTarget]
    [Component("Light")]
    public class LightComponent : Component
    {
        private int _colorB = 200;
        private int _colorG = 200;
        private int _colorR = 200;
        private LightModeClass _mode = LightModeClass.Constant;
        private LightState _state = LightState.On;

        public LightComponent()
        {
            Family = ComponentFamily.Light;
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.Children.TryGetValue(new YamlScalarNode("startState"), out node))
            {
                _state = (LightState) Enum.Parse(typeof (LightState), ((YamlScalarNode)node).Value, true);
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("lightColorR"), out node))
            {
                _colorR = int.Parse(((YamlScalarNode)node).Value);
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("lightColorG"), out node))
            {
                _colorG = int.Parse(((YamlScalarNode)node).Value);
            }

            if (mapping.Children.TryGetValue(new YamlScalarNode("lightColorB"), out node))
            {
                _colorB = int.Parse(((YamlScalarNode)node).Value);
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
                    IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Damage,
                                                                       "You fiddle with it, but nothing happens. It must be broken.",
                                                                       Owner.Name, Owner.Uid);
                    break;
            }
        }

        private void SetState(LightState state)
        {
            _state = state;
        }

        public override ComponentState GetComponentState()
        {
            return new LightComponentState(_state, _colorR, _colorG, _colorB, _mode);
        }
    }
}
