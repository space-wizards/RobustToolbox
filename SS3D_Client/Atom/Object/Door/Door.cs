using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using CGO;
using SS3D_shared.GO;
using ClientServices;
using ClientServices.Map;

namespace SS3D.Atom.Object.Door
{
    public class Door : Object
    {
        DoorState status = DoorState.Closed;
        DoorState laststatus = DoorState.Closed;

        public Door()
            : base()
        {
            
        }

        public override void Initialize()
        {
            base.Initialize();
            ISpriteComponent c = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
            c.AddSprite("door_ew");
            c.AddSprite("door_ewo");
            c.SetSpriteByKey("door_ew");
            CollidableComponent co = (CollidableComponent)ComponentFactory.Singleton.GetComponent("CollidableComponent");
            co.SetParameter(new ComponentParameter("TweakAABB", typeof(Vector4D), new Vector4D(63, 0, 0, 0)));
            AddComponent(SS3D_shared.GO.ComponentFamily.Collidable, co);

            collidable = true;
            snapTogrid = true;
        }

        protected override void HandleExtendedMessage(Lidgren.Network.NetIncomingMessage message)
        {
            UpdateStatus((DoorState)message.ReadByte());
        }

        // This is virtual so future doors can override it if they
        // want different things to happen depending on their status
        public virtual void UpdateStatus(DoorState newStatus)
        {
            if (newStatus == status)
                return;
            laststatus = status;
            status = newStatus;
            Map map = (Map)ServiceManager.Singleton.GetService(ClientServiceType.Map);
            switch (status)
            {
                case DoorState.Closed:
                    ISpriteComponent c = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
                    c.SetSpriteByKey("door_ew");
                    visible = true;
                    SendMessage(null, ComponentMessageType.EnableCollision, null);
                    map.GetTileAt(Position).sightBlocked = true;
                    map.needVisUpdate = true;
                    Draw();
                    break;
                case DoorState.Open:
                    ISpriteComponent d = (ISpriteComponent)GetComponent(SS3D_shared.GO.ComponentFamily.Renderable);
                    d.SetSpriteByKey("door_ewo");
                    SendMessage(null, ComponentMessageType.DisableCollision, null);
                    map.GetTileAt(Position).sightBlocked = false;
                    map.needVisUpdate = true;
                    Draw();
                    break;
                case DoorState.Broken:
                    break;
                default:
                    break;
            }
            updateRequired = true;
        }

        public override void Update(float time)
        {
            base.Update(time);
            if (this.interpolationPackets.Count == 0)
            {
                //UpdateStatus();
            }
            else
            {
                updateRequired = true;
            }


        }

        public override RectangleF GetAABB()
        {
            return new RectangleF(Position.X - ((sprite.Width * sprite.UniformScale) / 2),
                    Position.Y + ((sprite.Height * sprite.UniformScale) / 2) - 1,
                    (sprite.Width * sprite.UniformScale),
                    1);
        }
    }
}
