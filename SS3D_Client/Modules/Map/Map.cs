using System;
using System.Collections.Generic;
using System.Text;
using Mogre;
using SS3D.HelperClasses;
using SS3D_shared;

namespace SS3D.Modules.Map
{
    public class Map : LoadingTracker
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
        public int StaticGeoSize = 10; // Size of one side of the square of tiles stored by each staticgeometry in the array.
        private Vector2 lastCamGeoPos = Vector2.UNIT_SCALE;

        #endregion

        public Map(OgreManager _mEngine, bool _useStaticGeo)
        {
            mEngine = _mEngine;
            useStaticGeo = _useStaticGeo;
        }

        public ManualObject CreateGrid(int height = 1)
        {
            if (tileArray == null) return null;

            ManualObject grid = new ManualObject("MapGrid");
            grid.Begin("Grid", RenderOperation.OperationTypes.OT_LINE_LIST);

            Vector3 vert1 = Vector3.ZERO;
            Vector3 vert2 = Vector3.ZERO;

            float offset = tileSpacing / 2; //Used to correctly align the grid. Tiles are centered on their nodes.

            ColourValue gridColor = new ColourValue(1.0f, 0f, 0f);

            for (int z = 0; z < mapHeight; z++)
            {
                if (z == 0) continue;
                vert1 = new Vector3(0 - offset, height, z * tileSpacing - offset);
                vert2 = new Vector3(mapWidth * tileSpacing - offset, height, z * tileSpacing - offset);
                grid.Position(vert1);
                grid.Colour(gridColor);
                grid.Position(vert2);
                grid.Colour(gridColor);
            }

            for (int x = 0; x < mapWidth; x++)
            {
                if (x == 0) continue;
                vert1 = new Vector3(x * tileSpacing - offset, height, 0 - offset);
                vert2 = new Vector3(x * tileSpacing - offset, height, mapHeight * tileSpacing - offset);
                grid.Position(vert1);
                grid.Colour(gridColor);
                grid.Position(vert2);
                grid.Colour(gridColor);
            }

            grid.QueryFlags = SS3D_shared.HelperClasses.QueryFlags.DO_NOT_PICK;
            grid.End();

            return grid;
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

            loadingText = "Building Map...";
            loadingPercent = 0;
            mEngine.OneUpdate();

            float maxElements = (mapHeight * mapWidth);
            float oneElement = 100f / maxElements;
            float currCount = 0;

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

                    currCount += oneElement;
                    if (currCount >= 1)
                    {
                        loadingPercent += maxElements > 100 ? 1 : oneElement;
                        currCount = 0;
                        mEngine.OneUpdate();
                    }

                }
            }

            if (useStaticGeo)
            {
                // Build all the staticgeometrys.
                BuildAllgeometry();
            }
            loadingText = "Map Created";
            loadingPercent = 0;
            mEngine.OneUpdate();
            return true;
        }

        public bool LoadMap(MapFile toLoad)
        {
            meshManager = new geometryMeshManager();
            meshManager.CreateMeshes();

            mapWidth = toLoad.TileData.width;
            mapHeight = toLoad.TileData.height;

            tileArray = new BaseTile[mapWidth, mapHeight];

            float geoSize = StaticGeoSize;

            StaticGeoX = (int)System.Math.Ceiling(mapWidth / geoSize);
            StaticGeoZ = (int)System.Math.Ceiling(mapHeight / geoSize);

            InitStaticgeometry();

            float maxElements = toLoad.TileData.TileInfo.Count;       //Number of elements total.
            float oneElement = 100f / toLoad.TileData.TileInfo.Count; //Value of one element.
            float currCount = 0;                                      //Counter.
            loadingText = "Loading Tiles...";                         //Setting the text of the inherited abstract class.

            foreach (TileEntry entry in toLoad.TileData.TileInfo)
            {   // x=x z=y , sorry about that.

                int posX = entry.position.x * tileSpacing;
                int posZ = entry.position.y * tileSpacing;

                TileType type = (TileType)entry.type; //Enum is saved as int in file.
                Type classType = type.GetClass();     //Reflection magic in the enum.

                // Arguments for Turf Constructuctors : (SceneManager sceneManager, Vector3 position, int tileSpacing)
                object[] arguments = new object[3];
                arguments[0] = mEngine.SceneMgr;
                arguments[1] = new Vector3(posX, 0, posZ);
                arguments[2] = tileSpacing;

                object newTile = Activator.CreateInstance(classType, arguments);      //Create new instance.
                BaseTile newTileConv = (BaseTile)newTile;                        
                tileArray[entry.position.x, entry.position.y] = newTileConv;          //Put instance in correct place.
                tileArray[entry.position.x, entry.position.y].Node.SetVisible(false);
                staticGeometry[newTileConv.GeoPosX, newTileConv.GeoPosZ].AddSceneNode(tileArray[entry.position.x, entry.position.y].Node); //Add to static geometry array.

                currCount += oneElement; //Increase counter by value of one element.
                if (currCount >= 1)      //One percent full.
                {
                    loadingPercent += maxElements > 100 ? 1 : oneElement; //Setting the value of the inherited class.
                    currCount = 0;                                        //If more than 100 elements total inc by 1, else by the value of one element
                    mEngine.OneUpdate();//Update Engine & Render          //This allows the progress bar to move at bigger steps than 1% if needed.
                }

            }

            loadingPercent = 0; //Reset all the progress bar stuff.
            maxElements = toLoad.TileData.TileInfo.Count;
            oneElement = 100f / maxElements;
            currCount = 0;

            loadingText = "Loading Space...";
            mEngine.OneUpdate();

            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    if (tileArray[x, z] == null) //If theres nothing in it at this point, then its space.
                    {                            //Space tiles are not saved or loaded.
                        int posX = x * tileSpacing;
                        int posZ = z * tileSpacing;
                        tileArray[x, z] = new Space(mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
                        tileArray[x, z].Node.SetVisible(false);
                        staticGeometry[tileArray[x, z].GeoPosX, tileArray[x, z].GeoPosZ].AddSceneNode(tileArray[x, z].Node);
                    }
                    currCount += oneElement;
                    if (currCount >= 1)
                    {
                        loadingPercent += maxElements > 100 ? 1 : oneElement;
                        currCount = 0;
                        mEngine.OneUpdate();
                    }
                }
            }

            BuildAllgeometry();
            mEngine.OneUpdate();

            loadingText = "Map loaded";
            loadingPercent = 0;

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

            loadingText = "Building Map...";
            loadingPercent = 0;
            mEngine.OneUpdate();

            float maxElements = (mapHeight * mapWidth);
            float oneElement = 100f / maxElements;
            float currCount = 0;

            for (int z = 0; z < mapHeight; z++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int posX = x * tileSpacing;
                    int posZ = z * tileSpacing;

                    switch (networkedArray[x, z])
                    {
                        case TileType.Wall:
                            tileArray[x, z] = new Wall(mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
                            break;
                        case TileType.Floor:
                            tileArray[x, z] = new Floor(mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
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

                    currCount += oneElement;
                    if (currCount >= 1)
                    {
                        loadingPercent += maxElements > 100 ? 1 : oneElement;
                        currCount = 0;
                        mEngine.OneUpdate();
                    }
                }
            }


            if (useStaticGeo)
            {
                loadingText = "Initializing Static Geometry...";
                loadingPercent = 0;
                mEngine.OneUpdate();

                maxElements = (mapHeight * mapWidth);
                oneElement = 100f / maxElements;
                currCount = 0;

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

                        currCount += oneElement;
                        if (currCount >= 1)
                        {
                            loadingPercent += maxElements > 100 ? 1 : oneElement;
                            currCount = 0;
                            mEngine.OneUpdate();
                        }
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
                            tileArray[x, z] = new Wall(mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
                            break;
                        case "floor":
                            tileArray[x, z] = new Floor(mEngine.SceneMgr, new Vector3(posX, 0, posZ), tileSpacing);
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
            loadingText = "Building Static Geometry...";
            loadingPercent = 0;
            mEngine.OneUpdate();

            float maxElements = (StaticGeoX * StaticGeoZ);
            float oneElement = 100f / maxElements;
            float currCount = 0;

            for (int i = 0; i < StaticGeoX; i++)
            {
                for (int j = 0; j < StaticGeoZ; j++)
                {
                    staticGeometry[i, j].Build();

                    currCount += oneElement;
                    if (currCount >= 1)
                    {
                        loadingPercent += maxElements > 100 ? 1 : oneElement;
                        currCount = 0;
                        mEngine.OneUpdate();
                    }

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
                    Floor floor = new Floor(mEngine.SceneMgr, pos, tileSpacing);
                    return floor;
                case TileType.Wall:
                    Wall wall = new Wall(mEngine.SceneMgr, pos, tileSpacing);
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

            loadingText = "";
            loadingPercent = 0;
            //WHY IS IT UPDATING A BAZILLION TIMES WHEN SHUTTING DOWN?
            //mEngine.OneUpdate();

            float maxElements;
            float oneElement;
            float currCount = 0;

            if (useStaticGeo)
            {
                loadingText = "Unloading Static Geometry...";
                loadingPercent = 0;
                //mEngine.OneUpdate();

                maxElements = (StaticGeoX * StaticGeoZ);
                oneElement = 100f / maxElements;
                currCount = 0;

                for (int i = 0; i < StaticGeoX; i++)
                {
                    for (int j = 0; j < StaticGeoZ; j++)
                    {
                        staticGeometry[i, j].Reset();
                        currCount += oneElement;
                        if (currCount >= 1)
                        {
                            loadingPercent += maxElements > 100 ? 1 : oneElement;
                            currCount = 0;
                            //mEngine.OneUpdate();
                        }
                    }
                }
                mEngine.SceneMgr.DestroyAllStaticGeometry();
            }
            mEngine.SceneMgr.DestroyAllEntities();

            loadingText = "Unloading Map...";
            loadingPercent = 0;
            //mEngine.OneUpdate();

            maxElements = (mapWidth * mapHeight);
            oneElement = 100f / maxElements;
            currCount = 0;

            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    mEngine.SceneMgr.DestroySceneNode(tileArray[x, z].Node.Name);
                    currCount += oneElement;
                    if (currCount >= 1)
                    {
                        loadingPercent += maxElements > 100 ? 1 : oneElement;
                        currCount = 0;
                        //mEngine.OneUpdate();
                    }
                }
            }
            tileArray = null;
            meshManager = null;
            boundingBoxArray = null;
            staticGeometry = null;
            loadingPercent = 0;
        }
        #endregion
    }
}
