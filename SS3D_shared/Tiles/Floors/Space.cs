using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;

namespace SS3D_shared
{
    public class Space : BaseTile
    {
        public Space(SceneManager sceneManager, Vector3 position, int tileSpacing)
            : base(tileSpacing)
        {
            TileType = TileType.Space;
            string entityName;
            name = "space";
            meshName = "spaceMesh";
            entityName = "0" + position.x + "0" + position.z;
            if (sceneManager.HasEntity(entityName))
            {
                sceneManager.DestroyEntity(entityName);
            }
            if (sceneManager.HasSceneNode(entityName))
            {
                sceneManager.DestroySceneNode(entityName);
            }
            
            Node = sceneManager.RootSceneNode.CreateChildSceneNode(entityName);
            Entity = sceneManager.CreateEntity(entityName, "spaceMesh");
            Entity.QueryFlags = HelperClasses.QueryFlags.ENTITY_SPACE;
            Node.Position = position;
            Node.AttachObject(Entity);
            Entity.UserObject = (AtomBaseClass)this;
            SetGeoPos();
            /*
            Node = sceneManager.RootSceneNode.CreateChildSceneNode(entityName);
            Node.Position = position;
            SetGeoPos();*/
        }

        public Space()
            : base()
        {
            TileType = TileType.Space;
            name = "space";
        }
    }
}
