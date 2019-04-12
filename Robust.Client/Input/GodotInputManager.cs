using Robust.Client.Interfaces;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Input
{
    internal class GodotInputManager : InputManager
    {
        [Dependency]
        readonly ISceneTreeHolder sceneTree;

        public override Vector2 MouseScreenPosition => sceneTree.SceneTree.Root.GetMousePosition().Convert();
    }
}
