using System.Collections.Generic;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public abstract class ValidationNode
    {
        public abstract bool Valid { get; }

        public abstract IEnumerable<ErrorNode> GetErrors();
    }
}
