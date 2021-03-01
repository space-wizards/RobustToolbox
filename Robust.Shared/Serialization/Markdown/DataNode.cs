using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.Serialization.Markdown
{
    public abstract class DataNode
    {
        public abstract DataNode Copy();
        public string? Tag;
    }
}
