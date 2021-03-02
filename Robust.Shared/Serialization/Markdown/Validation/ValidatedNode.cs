using System.Collections.Generic;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public abstract class ValidatedNode
    {
        public abstract bool Valid { get; }

        public abstract IEnumerable<string> Invalids();
    }
}
