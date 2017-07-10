using SS14.Shared.GameObjects;
using System.Collections.Generic;

namespace SS14.Client.GameObjects
{
    public class ClientComponentFactory : ComponentFactory
    {
        protected override HashSet<string> IgnoredComponentNames { get; } = new HashSet<string>()
        {
            "BasicInteractable",
            "BasicDoor",
            "WallMounted",
            "Worktop",
            "BasicLargeObject"
        };
    }
}
