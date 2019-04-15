using System;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
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

        #pragma warning disable CS0067
        public override event Action<WindowResizedEventArgs> OnWindowResized;
        #pragma warning restore CS0067
    }
}
