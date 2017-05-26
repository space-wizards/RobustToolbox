using SS14.Shared.GameObjects;

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
