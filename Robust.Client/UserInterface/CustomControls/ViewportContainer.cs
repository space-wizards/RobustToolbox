using Robust.Shared.Maths;
using Robust.Shared.Utility;
﻿using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls
{
    /// <summary>
    ///     A viewport container shows a viewport.
    /// </summary>
    public class ViewportContainer : Control
    {
        private readonly IClyde _displayManager;
        public IClydeViewport? Viewport;
        public readonly bool OwnsViewport;

        public ViewportContainer(bool owns)
        {
            _displayManager = IoCManager.Resolve<IClyde>();
            OwnsViewport = owns;
            if (owns)
            {
                Viewport = _displayManager.CreateViewport((1, 1), "ViewportContainerOwnedViewport");
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);
            if (OwnsViewport)
            {
                Viewport.Render();
            }
            if (Viewport == null)
            {
                handle.DrawRect(UIBox2.FromDimensions((0, 0), Size * UserInterfaceManager.UIScale), Color.Red);
            }
            else
            {
                handle.DrawTextureRect(Viewport.RenderTarget.Texture, UIBox2.FromDimensions((0, 0), PixelSize));
            }
        }

        protected override void Resized()
        {
            if (OwnsViewport)
            {
                Viewport?.Dispose();
                Viewport = _displayManager.CreateViewport(Vector2i.ComponentMax((1, 1), PixelSize), "ViewportContainerOwnedViewport");
            }
        }

    }
}
