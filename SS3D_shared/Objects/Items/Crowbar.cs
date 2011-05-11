using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using Mogre;

namespace SS3D_shared
{
    public class Crowbar : Item
    {
        public Crowbar(SceneManager sceneManager, Mogre.Vector3 position, ushort ID)
            : base()
        {
            ItemType = ItemType.Crowbar;
            meshName = "crowbar.mesh";
            name = "crowbar" + ID;
            string entityName = name;
            if (sceneManager.HasEntity(entityName))
            {
                sceneManager.DestroyEntity(entityName);
            }
            if (sceneManager.HasSceneNode(entityName))
            {
                sceneManager.DestroySceneNode(entityName);
            }
            Node = sceneManager.RootSceneNode.CreateChildSceneNode(entityName);
            Entity = sceneManager.CreateEntity(entityName, meshName);
            Entity.UserObject = (AtomBaseClass)this;
            Node.Scale(0.1f, 0.1f, 0.1f);
            Node.Position = position;
            Node.AttachObject(Entity);
            interpolationPacket = new List<InterpolationPacket>();
            itemID = ID;
            lastUpdate = DateTime.Now;
            
        }

        public Crowbar()
            :base()
        {
            ItemType = ItemType.Crowbar;
            serverInfo = new ServerItemInfo();
        }
    }
}
