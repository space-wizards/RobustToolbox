using GorgonLibrary;
using SS14.Shared;

namespace SS14.Client.GameObjects
{
    public struct Vector2TypeConverter
    {
        /// <summary>
        /// Property for the x component of the Vector2
        /// </summary>
        public float X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        /// Property for the y component of the Vector2
        /// </summary>
        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        /// The X component of the vector
        /// </summary>
        private float x;

        /// <summary>
        /// The Y component of the vector
        /// </summary>
        private float y;

        public Vector2TypeConverter(float x, float y)
        {
            this.x = 0;
            this.y = 0;

            X = x;
            Y = y;
        }

        public static implicit operator Vector2TypeConverter(Vector2 vec)
        {
            return new Vector2TypeConverter(vec.X, vec.Y);
        }

        public static implicit operator Vector2TypeConverter(Vector2D vec)
        {
            return new Vector2TypeConverter(vec.X, vec.Y);
        }

        public static implicit operator Vector2(Vector2TypeConverter vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        public static implicit operator Vector2D(Vector2TypeConverter vec)
        {
            return new Vector2D(vec.X, vec.Y);
        }

        public static Vector2 ToVector2(Vector2D vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        public static Vector2D ToVector2D(Vector2 vec)
        {
            return new Vector2D(vec.X, vec.Y);
        }
    }
}
