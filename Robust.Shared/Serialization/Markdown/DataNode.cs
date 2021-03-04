using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Serialization.Markdown
{
    public abstract class DataNode
    {
        public string? Tag;
        public DataPosition Start;
        public DataPosition End;

        public DataNode(DataPosition start, DataPosition end)
        {
            Start = start;
            End = end;
        }

        public abstract DataNode Copy();
        public abstract DataNode? Except(DataNode node);

        public T CopyCast<T>() where T : DataNode
        {
            return (T) Copy();
        }
    }

    public abstract class DataNode<T> : DataNode where T : DataNode<T>
    {
        protected DataNode(DataPosition start, DataPosition end) : base(start, end)
        { }

        public abstract override T Copy();
        public abstract T? Except(T node);

        public override DataNode? Except(DataNode node)
        {
            if (node is not T tNode) throw new InvalidNodeTypeException();
            return Except(tNode);
        }
    }
}
