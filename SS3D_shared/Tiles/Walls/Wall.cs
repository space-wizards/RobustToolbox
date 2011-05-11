using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;

namespace SS3D_shared
{
    public class Wall : BaseTile
    {
        public Wall(string meshName, SceneManager sceneManager, Vector3 position, int tileSpacing)
            : base()
        {
            TileType = TileType.Wall;
            string entityName;
            name = "wall";
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
            Entity = sceneManager.CreateEntity(entityName, meshName);
            Node.Position = position;
            Node.AttachObject(Entity);
            Entity.UserObject = (AtomBaseClass)this;
            SetGeoPos();
        }

        public Wall()
            : base()
        {
            TileType = TileType.Wall;
            name = "wall";
        }
    }
}