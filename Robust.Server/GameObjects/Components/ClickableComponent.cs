using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;

namespace Robust.Server.GameObjects
{
    public class ClickableComponent : Component, IClickableComponent
    {
        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        // If you came here looking for Click Events, use the Input System.
    }
}
