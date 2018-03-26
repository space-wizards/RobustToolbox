namespace SS14.Shared.GameObjects.Serialization
{
    public delegate void SetFunctionDelegate<T>(T value);
    public delegate T GetFunctionDelegate<T>();

    public abstract class EntitySerializer
    {
        public bool Reading { get; protected set; }

        public abstract void EntityHeader();
        public abstract void EntityFooter();

        public abstract void CompHeader();
        public abstract void CompStart(string name);
        public abstract void CompFooter();

        public abstract void DataField<T>(ref T value, string name, T defaultValue, bool alwaysWrite = false);

        public abstract void DataSetFunction<T>(string name, T defaultValue, SetFunctionDelegate<T> func);
        public abstract void DataGetFunction<T>(string name, T defaultValue, GetFunctionDelegate<T> func, bool alwaysWrite = false);
    }
}
