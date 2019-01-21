using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics
{
    internal class DisplayManagerHeadless : DisplayManager
    {
        // Would it make sense to report a fake resolution like 720p here so code doesn't break? idk.
        public override Vector2i ScreenSize => Vector2i.Zero;

        public override void SetWindowTitle(string title)
        {
            // Nada.
        }

        public override void Initialize()
        {
            // Nada.
        }

        public override void ReloadConfig()
        {
            // Do nothing.
        }
    }
}
