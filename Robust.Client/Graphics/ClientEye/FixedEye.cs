using Robust.Client.Interfaces;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.ClientEye
{
    /// <summary>
    ///     A fixed eye is an eye which is fixed to one point, its position.
    /// </summary>
    public class FixedEye : Eye
    {
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
