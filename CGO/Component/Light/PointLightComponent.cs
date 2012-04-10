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
        private ILight _light;
        private Vector2D _lightOffset = new Vector2D(0, 0);

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

            _light.SetRadius(1024);
            _light.SetColor(255, 193, 194, 180);
            _light.Move(Owner.Position + _lightOffset);
            Owner.OnMove += OnMove;
            
            /*_light = IoCManager.Resolve<ILightManager>().CreateLight(IoCManager.Resolve<IMapManager>(),
                                                                     System.Drawing.Color.FloralWhite, 300,
                                                                     LightState.On, Owner.Position);
            _light.Brightness = 1.5f;

            _light.UpdatePosition(Owner.Position + _lightOffset);
            _light.UpdateLight();
            Owner.OnMove += OnMove;*/
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
            }
        }

        public override void OnRemove()
        {
            Owner.OnMove -= OnMove;
            IoCManager.Resolve<ILightManager>().RemoveLight(_light);
            //_light.ClearTiles();
            base.OnRemove();
        }

        private void OnMove(object sender, VectorEventArgs args)
        {
            _light.Move(Owner.Position + _lightOffset);
            //_light.UpdatePosition(Owner.Position + _lightOffset);
        }


    }
}
