using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace SS14.Server.GameObjects
{
    public class ClickableComponent : Component, IClickableComponent
    {
        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        // If you came here looking for Click Events, use the Input System.
    }
}
