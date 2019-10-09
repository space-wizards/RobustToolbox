namespace Robust.Server.Console
{
    public struct ConGroupIndex
    {
        public int Index { get; }

        public ConGroupIndex(int index)
        {
            Index = index;
        }

        public override string ToString()
        {
            return Index.ToString();
        }
    }
}
