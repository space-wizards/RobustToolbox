using SS14.Shared.GameObjects;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
{
    public class ServerComponentFactory : ComponentFactory
    {
        protected override HashSet<string> IgnoredComponentNames { get; } = new HashSet<string>()
        {
            "Icon"
        };
    }
}
