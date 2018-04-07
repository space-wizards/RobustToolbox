using SS14.Client.Interfaces;
using SS14.Client.Utility;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.ClientEye
{
    /// <summary>
    ///     A fixed eye is an eye which is fixed to one point, its position.
    /// </summary>
    public class FixedEye : Eye
    {
        private Vector2 position;
        public Vector2 Position
        {
            get => position;
            set
            {
                GodotCamera.Position = value.Convert();
                position = value;
            }
        }

        private ISceneTreeHolder sceneTree;

        public FixedEye()
        {
            sceneTree = IoCManager.Resolve<ISceneTreeHolder>();
            sceneTree.WorldRoot.AddChild(GodotCamera);
        }
    }
}
