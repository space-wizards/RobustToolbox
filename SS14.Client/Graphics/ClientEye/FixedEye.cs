using SS14.Client.Interfaces;
using SS14.Client.Utility;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.ClientEye
{
    /// <summary>
    ///     A fixed eye is an eye which is fixed to one point, its position.
    /// </summary>
    public class FixedEye : Eye
    {
        private MapCoordinates position;

        public override MapCoordinates Position
        {
            get => base.Position;
            internal set
            {
                if (GameController.OnGodot)
                {
                    GodotCamera.Position = value.Position.Convert();
                }

                base.Position = value;
            }
        }

        private readonly ISceneTreeHolder sceneTree;

        public FixedEye()
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            sceneTree = IoCManager.Resolve<ISceneTreeHolder>();
            sceneTree.WorldRoot.AddChild(GodotCamera);
        }
    }
}
