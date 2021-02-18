using System;

namespace Robust.Shared.Serialization.Markdown
{
    public abstract class DataNode
    {
        public abstract DataNode Copy();
        public string? Tag;
    }
}
