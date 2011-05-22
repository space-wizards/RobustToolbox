using System;
using System.Collections.Generic;
using System.Text;
using Mogre;
using SS3D.HelperClasses;
using SS3D_shared;

namespace SS3D.Modules.Map
{
    public class Map
    {
        #region Variables
        public BaseTile[,] tileArray; // The array holding all the tiles that make up the map
        public int mapWidth; // Number of tiles across the map (must be a multiple of StaticGeoSize)
        public int mapHeight; // Number of tiles up the map (must be a multiple of StaticGeoSize)
        public int tileSpacing = 16; // Distance between tiles
        public AxisAlignedBox[,] boundingBoxArray;
        private bool useStaticGeo;

        private OgreManager mEngine;
        private geometryMeshManager meshManager; // Builds and stores the meshes for all floors/walls.

        /* The static geometry makes up the map - all the walls and floors.
         * It is very fast to render but cannot be moved or edited once built.
         * It has been split into 10x10 chunks which are assigned automatically, meaning
         * if we want to replace/change a tile, we only have to rebuild a small are rather than
         * rebuild the thousands of tiles that make up the whole level.
         */

        public StaticGeometry[,] staticGeometry;
        private int StaticGeoX; // The width of the array - the number of tiles / 10 rounded up.
        private int StaticGeoZ; // Same as above.
        private int StaticGeoSize = 10; // Size of one side of the square of tiles stored by each staticgeometry in the array.
        private Vector2 lastCamGeoPos = Vector2.UNIT_SCALE;
        #endregion

        public Map(OgreManager _mEngine, bool _useStaticGeo)
        {
            mEngine = _mEngine;
            useStaticGeo = _useStaticGeo;
        }

        #region Startup / Loading
        public bool InitMap(int width, int height, bool wallSurround, bool startBlank, int partitionSize)
        {
            meshManager = new geometryMeshManager();
            meshManager.CreateMeshes();

            mapWidth = width;
            mapHeight = height;

            if (useStaticGeo)
            {
                float geoSize = StaticGeoSize;

                // Get the width and height our staticgeometry array needs to be.
                StaticGeoX = (int)System.Math.Ceiling(width / geoSize);
                StaticGeoZ = (int)System.Math.Ceiling(height / geoSize);

                InitStaticgeometry();
            }

            // Init our tileArray.
            tileArray = new BaseTile[mapWidth, mapHeight];
            boundingBoxArray = new AxisAlignedBox[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    // The position of the tiles scenenode.
                    int posX = x * tileSpacing;
                    int posZ = z * tileSpacing;
                    if (startBlank)
                    {
                        tileArray[x, z] = GenerateNewTile(TileType.Space, new Vector3(posX, 0, posZ));
                    }
                    else if ((wallSurround && (x == 0 || (x == mapWidth - 1) || z == 0 || (z == mapHeight - 1))) || 
                        (partitionSize > 0 && ((x > 0 && x % partitionSize == 0) || (z > 0 && z % partitionSize == 0))))
                    {
                        tileArray[x,z] = GenerateNewTile(TileType.Wall, new Vector3(posX, 0, posZ));  
                    }
                    else
                    {
                        tileArray[x, z] = GenerateNewTile(TileType.Floor, new Vector3(posX, 0, posZ));  
                    }

                    if (useStaticGeo)
                    {
                        // Get which piece of the staticGeometry array this tile belongs in (automatically
                        // worked out when the tile is created)
                        int GeoX = tileArray[x, z].GeoPosX;
                        int GeoZ = tileArray[x, z].GeoPosZ;

                        // Add it to the appropriate staticgeometry array.
                        staticGeometry[GeoX, GeoZ].AddSceneNode(tileArray[x, z].Node);
                        // Remove it from the main scene manager, otherwise it would be draw twice.
                        //mEngine.SceneMgr.RootSceneNode.RemoveChild(tileArray[x, z].tileNode);
                        tileArray[x, z].Node.SetVisible(false);
                    }

                }
            }

            if (useStaticGeo)
            {
                // Build all the staticgeometrys.
                BuildAllgeometry();
            }
            return true;
        }

        public bool LoadMap(MapFile toLoad)
        {
            meshManager = new geometryMeshManager();
            meshManager.CreateMeshes();

            mapWidth = toLoad.TileData.width;
            mapHeight = toLoad.TileData.height;

            float geoSize = StaticGeoSize;

            StaticGeoX = (int)System.Math.Ceiling(mapWidth / geoSize);
            StaticGeoZ = (int)System.Math.Ceiling(mapHeight / geoSize);

            InitStaticgeometry();

            tileArray = new BaseTile[mapWidth, mapHeight];

            foreach (TileEntry entry in toLoad.TileData.TileInfo)
            {   // x=x z=y
                int posX = entry.position.x * tileSpacing;
                int posZ = entry.position.y * tileSpacing;
                //OH FUCK.
                TileType type = (TileType)entry.type;
                Type classType = type.GetClass();
                object[] arguments = new object[3];
                //arguments[0] = mEngine.SceneMgr;
                //arguments[1] = new Vector3(x, y, z);
                //arguments[2] = itemID;

                object newTile = Activator.CreateInstance(classType, arguments);
            }


            //for (int z = 0; z < mapHeight; z++)
            //{
            //    for (int x = 0; x < mapWidth; x++)
            //    {
            //        int posX = x * tileSpacing;
            //        int posZ = z * tileSpacing;

            //        switch (savedArray[x, z])
            //        {
            //            case "wall":
            //                tileArray[x, z] = new Wall(meshManager.wallMesh.Name, mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
            //                break;
            //            case "floor":
            //                tileArray[x, z] = new Floor(meshManager.floorMesh.Name, mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
            //                break;
            //            default:
            //                break;
            //        }
            //        tileArray[x, z].Node.SetVisible(false);
            //    }
            //}

            //for (int x = 0; x < mapWidth; x++)
            //{
            //    for (int z = 0; z < mapHeight; z++)
            //    {

            //        // Get which piece of the staticGeometry array this tile belongs in (automatically
            //        // worked out when the tile is created)
            //        int GeoX = tileArray[x, z].GeoPosX;
            //        int GeoZ = tileArray[x, z].GeoPosZ;

            //        // Add it to the appropriate staticgeometry array.
            //        staticGeometry[GeoX, GeoZ].AddSceneNode(tileArray[x, z].Node);
            //    }
            //}

            //// Build all the staticgeometrys.
            //BuildAllgeometry();
            //mEngine.Update();
            return true;
        }

        public bool LoadMapSaverMap(int width, int height, string[,] savedArray)
        {
            meshManager = new geometryMeshManager();
            meshManager.CreateMeshes();

            mapWidth = width;
            mapHeight = height;

            if (useStaticGeo)
            {
                float geoSize = StaticGeoSize;
                // Get the width and height our staticgeometry array needs to be.
                StaticGeoX = (int)System.Math.Ceiling(width / geoSize);
                StaticGeoZ = (int)System.Math.Ceiling(height / geoSize);

                InitStaticgeometry();
            }

            ParseNameArray(savedArray);

            if (useStaticGeo)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    for (int z = 0; z < mapHeight; z++)
                    {

                        // Get which piece of the staticGeometry array this tile belongs in (automatically
                        // worked out when the tile is created)
                        int GeoX = tileArray[x, z].GeoPosX;
                        int GeoZ = tileArray[x, z].GeoPosZ;

                        // Add it to the appropriate staticgeometry array.
                        staticGeometry[GeoX, GeoZ].AddSceneNode(tileArray[x, z].Node);
                    }
                }

                // Build all the staticgeometrys.
                BuildAllgeometry();
            }
            mEngine.Update();
            return true;
        }

        public bool LoadNetworkedMap(TileType[,] networkedArray, int _mapWidth, int _mapHeight)
        {
            meshManager = new geometryMeshManager();
            meshManager.CreateMeshes();

            mapWidth = _mapWidth;
            mapHeight = _mapHeight;

            tileArray = new BaseTile[mapWidth, mapHeight];

            for (int z = 0; z < mapHeight; z++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int posX = x * tileSpacing;
                    int posZ = z * tileSpacing;

                    switch (networkedArray[x, z])
                    {
                        case TileType.Wall:
                            tileArray[x, z] = new Wall(meshManager.wallMesh.Name, mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
                            break;
                        case TileType.Floor:
                            tileArray[x, z] = new Floor(meshManager.floorMesh.Name, mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
                            break;
                        case TileType.Space:
                            tileArray[x, z] = new Space(mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
                            break;
                        default:
                            break;
                    }

                    if (useStaticGeo)
                    {
                        tileArray[x, z].Node.SetVisible(false);
                    }
                }
            }

            if (useStaticGeo)
            {
                float geoSize = StaticGeoSize;
                // Get the width and height our staticgeometry array needs to be.
                StaticGeoX = (int)System.Math.Ceiling(mapWidth / geoSize);
                StaticGeoZ = (int)System.Math.Ceiling(mapHeight / geoSize);

                InitStaticgeometry();

                for (int x = 0; x < mapWidth; x++)
                {
                    for (int z = 0; z < mapHeight; z++)
                    {

                        // Get which piece of the staticGeometry array this tile belongs in (automatically
                        // worked out when the tile is created)
                        int GeoX = tileArray[x, z].GeoPosX;
                        int GeoZ = tileArray[x, z].GeoPosZ;

                        // Add it to the appropriate staticgeometry array.
                        staticGeometry[GeoX, GeoZ].AddSceneNode(tileArray[x, z].Node);
                    }
                }

                // Build all the staticgeometrys.
                BuildAllgeometry();
            }
            mEngine.Update();
            return true;
        }

        private void ParseNameArray(string[,] savedArray)
        {
            tileArray = new BaseTile[mapWidth, mapHeight];

            for (int z = 0; z < mapHeight; z++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int posX = x * tileSpacing;
                    int posZ = z * tileSpacing;

                    switch (savedArray[x, z])
                    {
                        case "wall":
                            tileArray[x, z] = new Wall(meshManager.wallMesh.Name, mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
                            break;
                        case "floor":
                            tileArray[x, z] = new Floor(meshManager.floorMesh.Name, mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
                            break;
                        default:
                            break;
                    }

                    if (useStaticGeo)
                    {
                        tileArray[x, z].Node.SetVisible(false);
                    }
                }
            }
        }
        #endregion

        #region Geomap functions
        // Initilizes the staticgeometry array, should only ever be called once when the map is being made
        // for the first time.
        private void InitStaticgeometry()
        {
            staticGeometry = new StaticGeometry[StaticGeoX, StaticGeoZ];
            for (int i = 0; i < StaticGeoX; i++)
            {
                for (int j = 0; j < StaticGeoZ; j++)
                {
                    staticGeometry[i, j] = mEngine.SceneMgr.CreateStaticGeometry("Mapgeometry" + "0" + i + "0" + j);
                    float halfway = tileSpacing * (StaticGeoSize / 2);
                    float originX = (i * (StaticGeoSize * tileSpacing));
                    float originY = 0f;
                    float originZ = ((j + 1)* (StaticGeoSize * tileSpacing));
                    staticGeometry[i, j].Origin = new Vector3(originX, originY, originZ);
                    staticGeometry[i, j].RegionDimensions = new Vector3(StaticGeoSize * tileSpacing, tileSpacing * 2, StaticGeoSize * tileSpacing);
                    staticGeometry[i, j].CastShadows = true;
                }
            }
          
        }

        // Builds all the geometry in the level, should only be called once when the map is being made
        // for the first time.
        private void BuildAllgeometry()
        {
            for (int i = 0; i < StaticGeoX; i++)
            {
                for (int j = 0; j < StaticGeoZ; j++)
                {
                    staticGeometry[i, j].Build();
                }
                
            }
        }

        // Clears one part of the staticgeometry, repopulates it from the tile array and then rebuilds it.
        // This is what should be used when a tile changes, passing in the location in the staticgeometry
        // array that the tile lives in. (This is stored on the tile in .GeoPosX and .GeoPosZ)
        public void RepopRebuildOnegeometry(int x, int y)
        {
            if (x > StaticGeoX || y > StaticGeoZ)
                return;
            staticGeometry[x, y].Reset();
            for (int i = x * StaticGeoSize; i < (x + 1) * StaticGeoSize; i++)
            {
                for (int j = y * StaticGeoSize; j < (y + 1) * StaticGeoSize; j++)
                {
                    staticGeometry[x, y].AddSceneNode(tileArray[i, j].Node);
                }
            }
            staticGeometry[x, y].Build();
        }

        public void ClearOnegeometry(int x, int y)
        {
            if (x > StaticGeoX || y > StaticGeoZ)
                return;
            staticGeometry[x, y].Reset();
        }

        public Vector2 GetGeoArrayPositionFromWorldPosition(Vector3 pos)
        {
            Vector2 geoPos = Vector2.ZERO;
            double camPosX = pos.x;
            double camPosZ = pos.z;
            camPosX /= (tileSpacing * StaticGeoSize);
            camPosZ /= (tileSpacing * StaticGeoSize);
            camPosX = System.Math.Floor(camPosX);
            camPosZ = System.Math.Floor(camPosZ);

            geoPos.x = (float)camPosX;
            geoPos.y = (float)camPosZ;

            return geoPos;
        }
        #endregion

        #region Tile helper functions
        // Returns the position of a tile in the tileArray from world coordinates
        // Returns -1,-1 if an invalid position was passed in.
        public Vector2 GetTileArrayPositionFromWorldPosition(float x, float z)
        {
            if (x < 0 || z < 0)
                return new Vector2(-1, -1);
            if (x > mapWidth * tileSpacing || z > mapWidth * tileSpacing)
                return new Vector2(-1, -1);

            int xPos = (int)System.Math.Floor(x / tileSpacing);
            int zPos = (int)System.Math.Floor(z / tileSpacing);

            return new Vector2(xPos, zPos);
        }


        private TileType GetObjectTypeFromWorldPosition(float x, float z)
        {
            Vector2 arrayPosition = GetTileArrayPositionFromWorldPosition(x, z);
            if (arrayPosition.x < 0 || arrayPosition.y < 0)
            {
                return TileType.None;
            }
            else
            {
                return GetObjectTypeFromArrayPosition((int)arrayPosition.x, (int)arrayPosition.y);
            }
        }

        private TileType GetObjectTypeFromWorldPosition(Vector3 pos)
        {
            Vector2 arrayPosition = GetTileArrayPositionFromWorldPosition(pos.x, pos.z);
            if (arrayPosition.x < 0 || arrayPosition.y < 0)
            {
                return TileType.None;
            }
            else
            {
                return GetObjectTypeFromArrayPosition((int)arrayPosition.x, (int)arrayPosition.y);
            }
        }

        private TileType GetObjectTypeFromArrayPosition(int x, int z)
        {
            if (x < 0 || z < 0 || x >= mapWidth || z >= mapHeight)
            {
                return TileType.None;
            }
            else
            {
                return tileArray[x, z].TileType;
            }
        }

        // Changes a tile based on its array position (get from world
        // coordinates using GetTileFromWorldPosition(int, int). Returns true if successful.
        public bool ChangeTile(Vector2 arrayPosition, TileType newType)
        {
            int x = (int)arrayPosition.x;
            int z = (int)arrayPosition.y;

            if (x < 0 || z < 0)
                return false;
            if (x > mapWidth || z > mapWidth)
                return false;
            Vector3 pos = tileArray[x, z].Node.Position;
            BaseTile tile = GenerateNewTile(newType, pos);

            if (tile == null)
            {
                return false;
            }

            tileArray[x, z] = tile;
            if (useStaticGeo)
            {
                tileArray[x, z].Node.SetVisible(false);
                int xPos = tileArray[x, z].GeoPosX;
                int yPos = tileArray[x, z].GeoPosZ;


                RepopRebuildOnegeometry(xPos, yPos);
            }
            return true;
        }

        public bool ChangeTile(int x, int z, TileType newType)
        {
            Vector2 pos = new Vector2(x, z);
            return ChangeTile(pos, newType);
        }

        public BaseTile GenerateNewTile(TileType type, Vector3 pos)
        {
            switch (type)
            {
                case TileType.Space:
                    Space space = new Space(mEngine.SceneMgr, pos, tileSpacing);
                    return space;
                case TileType.Floor:
                    Floor floor = new Floor(meshManager.floorMesh.Name, mEngine.SceneMgr, pos, tileSpacing);
                    return floor;
                case TileType.Wall:
                    Wall wall = new Wall(meshManager.wallMesh.Name, mEngine.SceneMgr, pos, tileSpacing);
                    return wall;
                default:
                    return null;
            }
        }

        #endregion

        #region Quick collision checks

        public bool CheckCollision(Vector3 pos)
        {
            TileType tile = GetObjectTypeFromWorldPosition(pos);

            if (tile == TileType.None)
            {
                return false;
            }
            else if (tile == TileType.Wall && pos.y <= meshManager.GetWallHeight())
            {
                return true;
            }
            else if ((tile == TileType.Floor || tile == TileType.Space) && pos.y < 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Vector3 GetPointAboveTileAt(Vector3 pos)
        {
            TileType tile = GetObjectTypeFromWorldPosition(pos);

            if (tile == TileType.None)
            {
                return pos;
            }
            else if (tile == TileType.Wall)
            {
                return new Vector3(pos.x, meshManager.GetWallHeight(), pos.z);
            }
            else if ((tile == TileType.Floor || tile == TileType.Space) && pos.y < 0)
            {
                return new Vector3(pos.x, 0, pos.z);
            }
            else
            {
                return pos;
            }
        }

        public float GetHeightAboveTileAt(Vector3 pos)
        {
            TileType tile = GetObjectTypeFromWorldPosition(pos);

            if (tile == TileType.None)
            {
                return pos.y;
            }
            else if (tile == TileType.Wall)
            {
                return meshManager.GetWallHeight();
            }
            else if ((tile == TileType.Floor || tile == TileType.Space) && pos.y < 0)
            {
                return meshManager.GetFloorHeight();
            }
            else
            {
                return pos.y;
            }
        }

        public TileType GetObjectTypeAt(Vector3 pos)
        {
            return GetObjectTypeFromWorldPosition(pos);
        }

        public bool IsFloorUnder(Vector3 pos)
        {
            if (GetObjectTypeFromWorldPosition(pos) == TileType.Floor)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<AxisAlignedBox> GetSurroundingAABB(Vector3 pos)
        {
            List<AxisAlignedBox> AABBList = new List<AxisAlignedBox>();

            Vector2 tilePos = GetTileArrayPositionFromWorldPosition(pos.x, pos.z);

            List<Vector2> cardinalList = new List<Vector2>();
            cardinalList.Add(new Vector2(0, 0));
            cardinalList.Add(new Vector2(0, 1));
            cardinalList.Add(new Vector2(0, -1));
            cardinalList.Add(new Vector2(1, 0));
            cardinalList.Add(new Vector2(-1, 0));
            cardinalList.Add(new Vector2(1, 1));
            cardinalList.Add(new Vector2(-1, -1));
            cardinalList.Add(new Vector2(-1, 1));
            cardinalList.Add(new Vector2(1, -1));

            foreach (Vector2 dir in cardinalList)
            {
                Vector2 checkPos = tilePos + dir;
                if (GetObjectTypeFromArrayPosition((int)checkPos.x, (int)checkPos.y) == TileType.Wall)
                {
                    AxisAlignedBox AABB = GetAABB(checkPos);
                    if (AABB != null)
                    {
                        AABBList.Add(AABB);
                    }
                }
            }
            return AABBList;
        }

        public AxisAlignedBox GetAABB(Vector2 tilePos)
        {
            if (tilePos.x < 0 || tilePos.x > mapWidth || tilePos.y < 0 || tilePos.y > mapHeight)
            {
                return null;
            }

            return tileArray[(int)tilePos.x, (int)tilePos.y].Node._getWorldAABB();
        }

        


        #endregion

        #region Shutdown
        public void Shutdown()
        {
            if (useStaticGeo)
            {
                for (int i = 0; i < StaticGeoX; i++)
                {
                    for (int j = 0; j < StaticGeoZ; j++)
                    {
                        staticGeometry[i, j].Reset();
                    }
                }
                mEngine.SceneMgr.DestroyAllStaticGeometry();
            }
            mEngine.SceneMgr.DestroyAllEntities();
            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    mEngine.SceneMgr.DestroySceneNode(tileArray[x, z].Node.Name);
                }
            }
            tileArray = null;
            meshManager = null;
            boundingBoxArray = null;
        }
        #endregion
    }
}
