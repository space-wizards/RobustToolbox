using SS14.Client.Interfaces;
using SS14.Client.Utility;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.Input
{
    public class GodotInputManager : InputManager
    {
        [Dependency]
        readonly ISceneTreeHolder sceneTree;

        public override Vector2 MouseScreenPosition => sceneTree.SceneTree.Root.GetMousePosition().Convert();
    }
}
