namespace Robust.Shared.Serialization.Markdown
{
    public abstract class DataNode
    {
        public string? Tag;

        public abstract DataNode Copy();

        public T CopyCast<T>() where T : DataNode
        {
            return (T) Copy();
        }
    }
}
