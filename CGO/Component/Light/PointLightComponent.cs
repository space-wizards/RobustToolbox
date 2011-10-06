using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces;
using ClientLighting;
using GorgonLibrary;

namespace CGO
{
    public class PointLightComponent : GameObjectComponent, ILight
    {
        private Light light;

        #region ILight members
        public Vector2D Position { get { return Owner.position; } set { } }
        public int Range { get { return light.Range; } set { light.Range = value; } }
        public void ClearTiles()
        { light.ClearTiles(); }
        public List<object> GetTiles()
        { return light.GetTiles(); }
        public void AddTile(object tile)
        { light.AddTile(tile); }
        public void UpdateLight()
        { light.UpdateLight(); }
        #endregion

        public PointLightComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Light;
        }

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);

            light = new Light((ClientMap.Map)ClientServices.ServiceManager.Singleton.GetService(ClientServiceType.Map),
                    System.Drawing.Color.FloralWhite, 300, LightState.On, Owner.position);
            light.brightness = 1.5f;

            light.UpdatePosition(Owner.position);
            light.UpdateLight();
            Owner.OnMove += new Entity.EntityMoveEvent(OnMove);
        }

        public override void OnRemove()
        {
            Owner.OnMove -= new Entity.EntityMoveEvent(OnMove);
         
            base.OnRemove();
        }

        private void OnMove(Vector2D toPosition)
        {
            light.UpdatePosition(Owner.position);
        }


    }
}
