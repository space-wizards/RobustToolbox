using SS14.Shared.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects
{
    [IoCTarget]
    [Component("BasicInteractable")]
    public class BasicInteractableComponent : Component
    {
        public BasicInteractableComponent()
        {
            Family = ComponentFamily.Interactable;
        }
    }
}
