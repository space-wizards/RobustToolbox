using SS14.Shared.GameObjects;
using SS14.Shared.GO;

namespace SS14.Server.GameObjects
{
    public class BasicInteractableComponent : Component
    {
        public BasicInteractableComponent()
        {
            Family = ComponentFamily.Interactable;
        }
    }
}