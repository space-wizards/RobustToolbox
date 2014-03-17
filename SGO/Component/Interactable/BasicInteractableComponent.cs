using System.Linq;
using GameObject;
using SS13_Shared.GO;

namespace SGO
{
    public class BasicInteractableComponent : Component
    {
        public BasicInteractableComponent()
        {
            Family = ComponentFamily.Interactable;
        }
    }
}