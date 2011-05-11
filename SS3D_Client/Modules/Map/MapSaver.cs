using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Mogre;
using SS3D.HelperClasses;
using SS3D_shared;


namespace SS3D.Modules.Map
{
    public class MapSaver
    {
        private Map map;
        private OgreManager mEngine;
        private int tileSpacing;

        public string[,] nameArray;
        public int mapWidth;
        public int mapHeight;


        public MapSaver(Map _map, OgreManager _mEngine)
        {
            map = _map;
            mEngine = _mEngine;
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
}
