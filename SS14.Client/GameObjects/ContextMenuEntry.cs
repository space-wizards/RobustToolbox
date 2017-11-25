namespace SS14.Client.GameObjects
{
    public struct ContextMenuEntry
    {
        private string componentMessage;
        private string entryName;
        private string iconName;

        public string ComponentMessage { get => componentMessage; set => componentMessage = value; }
        public string EntryName { get => entryName; set => entryName = value; }
        public string IconName { get => iconName; set => iconName = value; }
    }
}
