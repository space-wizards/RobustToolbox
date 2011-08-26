using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SS3D.HelperClasses;
using SS3D_shared;
//using SS3D.Modules.Items;

using System.Xml;
using System.Xml.Serialization;

namespace SS3D.Modules.Map
{
    public class MapSaver
    {
        private Map map;
        private int tileSpacing;

        public string[,] nameArray;
        public int mapWidth;
        public int mapHeight;

        public MapSaver(Map _map)
        {
            map = _map;
            tileSpacing = map.tileSpacing;
        }

        public bool Save(string filename)
        {
            if (filename == "")
            {
                return false;
            }

            FileStream fs = new FileStream(filename, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            sw.WriteLine(map.mapWidth);
            sw.WriteLine(map.mapHeight);

            for (int y = 0; y < map.mapHeight; y++)
            {
                for (int x = 0; x < map.mapWidth; x++)
                {
                    sw.WriteLine(map.tileArray[x,y].name);
                }
            }

            sw.Close();
            fs.Close();

            return true;
        }

        public bool Load(string filename)
        {
            if(!File.Exists(filename))
            {
                return false;
            }

            FileStream fs = new FileStream(filename, FileMode.Open);
            StreamReader sr = new StreamReader(fs);


            mapWidth = int.Parse(sr.ReadLine());
            mapHeight = int.Parse(sr.ReadLine());

            nameArray = new string[mapWidth, mapHeight];

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    nameArray[x, y] = sr.ReadLine();
                }
            }

            sr.Close();
            fs.Close();
            return true;
        }


    }

    public class MapFileHandler
    {
        //TO-DO: Add support for objects once they exist. Also need to change MapFile class for that.
        //       Maybe save / Load in separate threads.
        //       Support for rotated Items & Objects.

        public static void SaveMap(string path, Map map)
        {
            MapFile MapToSave = new MapFile();

            //Preparing the tile data. MapWidth - MapHeight
            MapToSave.TileData = new TileData();
            MapToSave.TileData.width = map.mapWidth;
            MapToSave.TileData.height = map.mapHeight;
            MapToSave.TileData.TileInfo = new List<TileEntry>();
            //Unfortunately we can't serialize multidimensional arrays :(. So it's a list.
            for (int y = 0; y < map.mapHeight; y++)
            {
                for (int x = 0; x < map.mapWidth; x++)
                {
                    if (map.tileArray[x, y].tileType == TileType.Space) continue; //Not saving space tiles. All empty tiles default to space. see LoadMap method in Map.cs
                    TileEntry NewTileEntry = new TileEntry();
                    NewTileEntry.type = (int)map.tileArray[x,y].tileType;
                    NewTileEntry.position = new Vec2(x, y);
                    MapToSave.TileData.TileInfo.Add(NewTileEntry);
                }
            }

            //Preparing the item data. ADD CHECK FOR PICKED UP ITEMS / ITEMS NOT IN WORLD / ITEMS IN OBJECTS OR OTHER ITEMS (CLOSET?) !!!
            MapToSave.ItemData = new ItemData();
            MapToSave.ItemData.ItemEntries = new List<ItemEntry>();
            /*foreach(KeyValuePair<ushort, Item> pair in itemManager.itemDict)
            {
                Item currentItem = pair.Value;
                ItemEntry newEntry = new ItemEntry();
                newEntry.type = (int)currentItem.ItemType;
                newEntry.position = new Vec3f(currentItem.Node.Position.x,currentItem.Node.Position.y,currentItem.Node.Position.z);
                newEntry.rotation = new RotaDeg(currentItem.Node.Orientation.Yaw.ValueDegrees, currentItem.Node.Orientation.Pitch.ValueDegrees, currentItem.Node.Orientation.Roll.ValueDegrees);
                MapToSave.ItemData.ItemEntries.Add(newEntry);
            }*/

            //Serialize & Write map to file.
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            System.Xml.Serialization.XmlSerializer MapSerializer = new System.Xml.Serialization.XmlSerializer(typeof(MapFile));
            StreamWriter MapWriter = File.CreateText(path);
            MapSerializer.Serialize(MapWriter, MapToSave);
            MapWriter.Flush();
            MapWriter.Close();
        }

        public static MapFile LoadMap(string path)
        {
            if(!File.Exists(path)) throw new FileNotFoundException("Map file not found.");
            MapFile LoadedMap;
            System.Xml.Serialization.XmlSerializer MapLoader = new System.Xml.Serialization.XmlSerializer(typeof(MapFile));
            StreamReader MapReader = File.OpenText(path);
            LoadedMap = (MapFile)MapLoader.Deserialize(MapReader);
            MapReader.Close();
            return LoadedMap;
        }
    }
}
