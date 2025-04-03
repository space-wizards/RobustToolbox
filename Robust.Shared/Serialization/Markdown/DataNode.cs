using System;
using System.IO;
using Robust.Shared.Serialization.Manager;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

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
        public virtual bool IsNull { get; init; } = false;

        public abstract DataNode Copy();

        /// <summary>
        ///     This function will return a data node that contains only the elements within this data node that do not
        ///     have an equivalent entry in some other data node.
        /// </summary>
        public abstract DataNode? Except(DataNode node);

        [Obsolete("Use SerializationManager.PushComposition()")]
        public abstract DataNode PushInheritance(DataNode parent);

        public T CopyCast<T>() where T : DataNode
        {
            return (T) Copy();
        }

        public void Write(TextWriter writer)
        {
            var yaml = this.ToYamlNode();
            var stream = new YamlStream { new(yaml) };
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        }

        public override string ToString()
        {
            StringWriter sw = new StringWriter();
            Write(sw);
            return sw.ToString();
        }
    }

    public abstract class DataNode<T> : DataNode where T : DataNode<T>
    {
        protected DataNode(NodeMark start, NodeMark end) : base(start, end)
        {
        }

        public abstract override T Copy();

        public abstract T? Except(T node);

        [Obsolete("Use SerializationManager.PushComposition()")]
        public abstract T PushInheritance(T node);

        public override DataNode? Except(DataNode node)
        {
            return node is not T tNode ? throw new InvalidNodeTypeException() : Except(tNode);
        }

        [Obsolete("Use SerializationManager.PushComposition()")]
        public override DataNode PushInheritance(DataNode parent)
        {
            return parent is not T tNode ? throw new InvalidNodeTypeException() : PushInheritance(tNode);
        }
    }
}
