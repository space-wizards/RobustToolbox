using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ServerServices.Tiles;

namespace SS3D_Server.Atom.Object.WallMounted
{
    [Serializable()]
    public class WallMounted : Object
    {
        public WallMounted()
            : base()
        {
            name = "wallmountedobj";
        }

        public override void PostSpawnActions()
        {
            base.PostSpawnActions();
            spawnTile.TileChange += new Tile.TileChangeHandler(WallChanged);
        }

        protected virtual void WallChanged(TileType tNew)
        {
            //Do whatever.
            SS3D_Server.SS3DServer.Singleton.chatManager.SendChatMessage(ChatChannel.Server," ("+this.Uid.ToString()+") -> Connected Wall Changed.",this.name,this.Uid);
            this.Translate(new SS3D_shared.HelperClasses.Vector2(position.X, position.Y + 64), 90); // IT FELL DOWN. DERP... Just testing.
        }

        public WallMounted(SerializationInfo info, StreamingContext ctxt)
        {
            SerializeBasicInfo(info, ctxt);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
    }
}
