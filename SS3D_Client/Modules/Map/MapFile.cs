using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D.Modules.Map
{
    public struct Vec2f
    {
        public float x, y;
        public Vec2f(float X, float Y)
        {
            x = X;
            y = Y;
        }
    }

    public struct Vec2
    {
        public int x, y;
        public Vec2(int X, int Y)
        {
            x = X;
            y = Y;
        }
    }

    public struct Vec3f
    {
        public float x, y, z;
        public Vec3f(float X, float Y, float Z)
        {
            x = X;
            y = Y;
            z = Z;
        }
    }

    public struct Vec3
    {
        public int x, y, z;
        public Vec3(int X, int Y, int Z)
        {
            x = X;
            y = Y;
            z = Z;
        }
    }

    public struct RotaDeg
    {
        public float yaw, pitch, roll;
        public RotaDeg(float Yaw, float Pitch, float Roll)
        {
            yaw = Yaw;
            pitch = Pitch;
            roll = Roll;
        }
    }

    [Serializable]
    public class MapFile
    {
        const int _version = 1;

        public TileData TileData;
        public ItemData ItemData;
    }

    public class TileData
    {
        public int width;
        public int height;
        public List<TileEntry> TileInfo;
    }

    public class TileEntry
    {
        public Vec2 position;
        public int type;
    }

    public class ItemData
    {
        public List<ItemEntry> ItemEntries;
    }

    public class ItemEntry
    {
        public int type;
        public Vec3f position;
        public RotaDeg rotation;
    }

    public class ObjectData
    {
        public List<ItemEntry> ObjectEntries;
    }

    public class ObjectEntry
    {
        public int type;
        public Vec3f position;
    }
}
