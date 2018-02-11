namespace SS14.Shared.GameObjects.Serialization
{
    public abstract class EntitySerializer
    {
        public bool Reading { get; protected set; }

        public abstract void EntityHeader();
        public abstract void EntityFooter();

        public abstract void CompHeader();
        public abstract void CompStart(string name);
        public abstract void CompFooter();

        public abstract void DataField<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false);
    }
}
