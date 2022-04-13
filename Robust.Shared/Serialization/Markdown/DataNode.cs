using Robust.Shared.Serialization.Manager;
using System;

namespace Robust.Shared.Serialization.Markdown
{
    public abstract class DataNode
    {
        public string? Tag;
        public NodeMark Start;
        public NodeMark End;

        public DataNode(NodeMark start, NodeMark end)
        {
            Start = start;
            End = end;
        }

        public abstract bool IsEmpty { get; }

        public abstract DataNode Copy();

        /// <summary>
        ///     This function will return a data node that contains only the elements within this data node that do not
        ///     have an equivalent entry in some other data node.
        /// </summary>
        public abstract DataNode? Except(DataNode node);

        public abstract DataNode PushInheritance(DataNode parent);

        public T CopyCast<T>() where T : DataNode
        {
            return (T) Copy();
        }
    }

    public abstract class DataNode<T> : DataNode where T : DataNode<T>
    {
        protected DataNode(NodeMark start, NodeMark end) : base(start, end)
        {
        }

        public abstract override T Copy();

        public abstract T? Except(T node);

        public abstract T PushInheritance(T node);

        public override DataNode? Except(DataNode node)
        {
            return node is not T tNode ? throw new InvalidNodeTypeException() : Except(tNode);
        }

        public override DataNode PushInheritance(DataNode parent)
        {
            return parent is not T tNode ? throw new InvalidNodeTypeException() : PushInheritance(tNode);
        }
    }
}
