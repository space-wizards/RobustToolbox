using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using Mogre;

namespace SS3D_shared
{
    public class Player : Mob
    {

         public Player(SceneManager sceneManager, Mogre.Vector3 position, ushort ID)
            : base()
        {
            name = "Mob" + ID;
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
            Entity = sceneManager.CreateEntity(entityName, "robot.mesh");
            Entity.UserObject = (AtomBaseClass)this;
            Node.Scale(0.4f, 0.4f, 0.4f);
            Node.Position = position;
            Node.AttachObject(Entity);
            interpolationPacket = new List<InterpolationPacket>();
            mobID = ID;

            animState = Entity.GetAnimationState("Idle");
            animState.Loop = true;
            animState.Enabled = true;
        }

         public Player()
            :base()
        {
            serverInfo = new ServerItemInfo();
            serverInfo.position = new HelperClasses.Vector3(0, 0, 0);
            serverInfo.rotW = 1;
            serverInfo.rotY = 0;
        }

    }
}
