using SS14.Shared.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects
{
    [IoCTarget]
    public class BasicInteractableComponent : Component
    {
        public override string Name => "BasicInteractable";
        public BasicInteractableComponent()
        {
            Family = ComponentFamily.Interactable;
        }
    }
}
