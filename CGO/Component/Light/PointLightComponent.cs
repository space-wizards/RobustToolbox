using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces;
using ClientServices.Lighting;
using GorgonLibrary;
using ClientServices.Map;

namespace CGO
{
    public class PointLightComponent : GameObjectComponent
    {
        //Contains a standard light
        private Light light;
        private Vector2D LightOffset = new Vector2D(0,0);

        public PointLightComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Light;
        }

        //When added, set up the light.
        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);

            light = new Light((Map)ClientServices.ServiceManager.Singleton.GetService(ClientServiceType.Map),
                    System.Drawing.Color.FloralWhite, 300, LightState.On, Owner.Position);
            light.brightness = 1.5f;

            light.UpdatePosition(Owner.Position + LightOffset);
            light.UpdateLight();
            Owner.OnMove += new Entity.EntityMoveEvent(OnMove);
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "lightoffset":
                    LightOffset = (Vector2D)parameter.Parameter;
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
