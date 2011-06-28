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
        private SceneManager sceneManager;
        public Player(SceneManager sceneManager, Mogre.Vector3 position, ushort ID)
            : base()
        {
            name = "Mob" + ID;
            this.sceneManager = sceneManager;

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
            Entity = sceneManager.CreateEntity(entityName, "male.mesh");
            Entity.UserObject = (AtomBaseClass)this;

            Node.Position = position;
            Node.AttachObject(Entity);
            interpolationPacket = new List<InterpolationPacket>();
            mobID = ID;

            animState = Entity.GetAnimationState("idle");
            animState.Loop = true;
            animState.Enabled = true;

            speed = walkSpeed;
        }

         public Player()
            :base()
        {
            serverInfo = new ServerItemInfo();
            serverInfo.position = new HelperClasses.Vector3(0, 0, 0);
            serverInfo.rotW = 1;
            serverInfo.rotY = 0;
        }

         public void Delete()
         {
             if (sceneManager.HasEntity(name))
             {
                 sceneManager.DestroyEntity(name);
             }
             if (sceneManager.HasSceneNode(name))
             {
                 sceneManager.DestroySceneNode(name);
             }
         }

    }
}
