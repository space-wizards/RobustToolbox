namespace SS13_Shared
{
    public static class Constants
    {
        #region MoveDirs enum

        public enum MoveDirs
        {
            north,
            northeast,
            east,
            southeast,
            south,
            southwest,
            west,
            northwest
        }

        #endregion

        public const byte NORTH = 1;
        public const byte EAST = 2;
        public const byte SOUTH = 4;
        public const byte WEST = 8;
    }
}