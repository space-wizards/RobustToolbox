using Lidgren.Network;
using SS14.Client.Interfaces.Lighting;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Light;
using SS14.Shared.IoC;
using System;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    public class PointLightComponent : Component
    {
        //Contains a standard light
        public ILight _light;
        public Vector3 _lightColor = new Vector3(190, 190, 190);
        public Vector2 _lightOffset = new Vector2(0, 0);
        public int _lightRadius = 512;
        protected string _mask = "";
        public LightModeClass _mode = LightModeClass.Constant;

        public PointLightComponent()
        {
            Family = ComponentFamily.Light;
        }

        public override Type StateType
        {
            get { return typeof (LightComponentState); }
        }

        //When added, set up the light.
        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);

            _light = IoCManager.Resolve<ILightManager>().CreateLight();
            IoCManager.Resolve<ILightManager>().AddLight(_light);

            _light.SetRadius(_lightRadius);
            _light.SetColor(255, (int) _lightColor.X, (int) _lightColor.Y, (int) _lightColor.Z);
            _light.Move(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position + _lightOffset);
            _light.SetMask(_mask);
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += OnMove;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            base.HandleNetworkMessage(message, sender);
            var type = (ComponentMessageType) message.MessageParameters[0];
            switch (type)
            {
                case ComponentMessageType.SetLightMode:
                    SetMode((LightModeClass) message.MessageParameters[1]);
                    break;
            }
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "lightoffsetx":
                    _lightOffset.X = parameter.GetValue<float>();
                    //float.Parse((string)parameter.Parameter, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "lightoffsety":
                    _lightOffset.Y = parameter.GetValue<float>();
                    //float.Parse((string)parameter.Parameter, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "lightradius":
                    _lightRadius = parameter.GetValue<int>(); //int.Parse((string) parameter.Parameter);
                    break;
                case "lightColorR":
                    _lightColor.X = parameter.GetValue<int>(); //int.Parse((string) parameter.Parameter);
                    break;
                case "lightColorG":
                    _lightColor.Y = parameter.GetValue<int>(); //int.Parse((string)parameter.Parameter);
                    break;
                case "lightColorB":
                    _lightColor.Z = parameter.GetValue<int>(); //int.Parse((string)parameter.Parameter);
                    break;
                case "mask":
                    _mask = parameter.GetValue<string>(); // parameter.Parameter;
                    break;
            }
        }

        protected void SetState(LightState state)
        {
            _light.SetState(state);
        }

        protected void SetMode(LightModeClass mode)
        {
            IoCManager.Resolve<ILightManager>().SetLightMode(mode, _light);
        }

        public override void OnRemove()
        {
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove -= OnMove;
            IoCManager.Resolve<ILightManager>().RemoveLight(_light);
            base.OnRemove();
        }

        private void OnMove(object sender, VectorEventArgs args)
        {
            _light.Move(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position + _lightOffset);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _light.Update(frameTime);
        }

        protected void SetMask(string mask)
        {
            _light.SetMask(mask);
        }

        public override void HandleComponentState(dynamic state)
        {
            if (_light.LightState != state.State)
                _light.SetState(state.State);
            if (_light.Color.R != state.ColorR || _light.Color.G != state.ColorG || _light.Color.B != state.ColorB)
            {
                SetColor(state.ColorR, state.ColorG, state.ColorB);
            }
            if (_mode != state.Mode)
                SetMode(state.Mode);
        }

        protected void SetColor(int R, int G, int B)
        {
            _lightColor.X = R;
            _lightColor.Y = G;
            _lightColor.Z = B;
            _light.SetColor(255, R, G, B);
        }
    }
}