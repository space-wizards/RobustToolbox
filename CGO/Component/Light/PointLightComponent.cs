using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces;
using ClientServices;
using ClientServices.Lighting;
using GorgonLibrary;
using ClientServices.Map;
using SS13_Shared;

namespace CGO
{
    public class PointLightComponent : GameObjectComponent
    {
        //Contains a standard light
        private Light light;
        private Vector2D LightOffset = new Vector2D(0,0);

        public PointLightComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Light;
        }

        //When added, set up the light.
        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);

            light = new Light(ServiceManager.Singleton.GetService<MapManager>(),
                              System.Drawing.Color.FloralWhite, 300, LightState.On, Owner.Position) {brightness = 1.5f};

            light.UpdatePosition(Owner.Position + LightOffset);
            light.UpdateLight();
            Owner.OnMove += OnMove;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "lightoffset":
                    LightOffset = (Vector2D)parameter.Parameter;
                    break;
                case "lightoffsetx":
                    LightOffset.X = float.Parse((string)parameter.Parameter, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "lightoffsety":
                    LightOffset.Y = float.Parse((string)parameter.Parameter, System.Globalization.CultureInfo.InvariantCulture);
                    break;
            }
        }

        public override void OnRemove()
        {
            Owner.OnMove -= new Entity.EntityMoveEvent(OnMove);
            light.ClearTiles();
            base.OnRemove();
        }

        private void OnMove(Vector2D toPosition)
        {
            light.UpdatePosition(Owner.Position + LightOffset);
        }


    }
}
