using System;

namespace SS14.Shared.Players
{
    [Serializable]
    public struct PlayerIndex
    {
        /// <summary>
        ///     Zero indexed position in the PlayerSession array.
        /// </summary>
        public int Index { get; }

        public PlayerIndex(int index)
        {
            Index = index;
        }

        public static implicit operator int(PlayerIndex index)
        {
            return index.Index;
        }

        public override string ToString()
        {
            return Index.ToString();
        }
    }
}
