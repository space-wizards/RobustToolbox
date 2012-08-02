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
    public class PointLightComponent : GameObjectComponent
    {
        //Contains a standard light
        public ILight _light;
        public int _lightRadius = 512;
        public Vector2D _lightOffset = new Vector2D(0, 0);
        public Vector3D _lightColor = new Vector3D(190,190,190);
        protected string _mask = "";
        
        public override ComponentFamily Family
        {
            get { return ComponentFamily.Light; }
        }

        //When added, set up the light.
        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);

            _light = IoCManager.Resolve<ILightManager>().CreateLight();
            IoCManager.Resolve<ILightManager>().AddLight(_light);

            _light.SetRadius(_lightRadius);
            _light.SetColor(255, (int)_lightColor.X, (int)_lightColor.Y, (int)_lightColor.Z);
            _light.Move(Owner.Position + _lightOffset);
            _light.SetMask(_mask);
            Owner.OnMove += OnMove;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            base.HandleNetworkMessage(message);
            var type = (ComponentMessageType) message.MessageParameters[0];
            switch(type)
            {
                case ComponentMessageType.SetLightState:
                    SetState((LightState)message.MessageParameters[1]);
                    break;
                case ComponentMessageType.SetLightMode:
                    SetMode((LightModeClass)message.MessageParameters[1]);
                    break;
            }
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "lightoffset":
                    _lightOffset = (Vector2D)parameter.Parameter;
                    break;
                case "lightoffsetx":
                    _lightOffset.X = float.Parse((string)parameter.Parameter, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "lightoffsety":
                    _lightOffset.Y = float.Parse((string)parameter.Parameter, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "lightradius":
                    _lightRadius = int.Parse((string) parameter.Parameter);
                    break;
                case "lightColorR":
                    _lightColor.X = int.Parse((string) parameter.Parameter);
                    break;
                case "lightColorG":
                    _lightColor.Y = int.Parse((string)parameter.Parameter);
                    break;
                case "lightColorB":
                    _lightColor.Z = int.Parse((string)parameter.Parameter);
                    break;
                case "mask":
                    _mask = (string) parameter.Parameter;
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
            Owner.OnMove -= OnMove;
            IoCManager.Resolve<ILightManager>().RemoveLight(_light);
            base.OnRemove();
        }

        private void OnMove(object sender, VectorEventArgs args)
        {
            _light.Move(Owner.Position + _lightOffset);
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

    }
}
