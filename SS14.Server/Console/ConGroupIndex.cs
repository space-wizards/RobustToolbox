namespace SS14.Server.Console
{
    internal struct ConGroupIndex
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
