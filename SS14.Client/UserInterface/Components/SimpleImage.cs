using System;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    /// <summary>
    ///     Displays an image on the screen.
    /// </summary>
    public class SimpleImage : GuiComponent
    {
        private readonly IResourceCache _resourceCache;

        private Sprite _drawingSprite;

        /// <summary>
        ///     Sprite to draw inside of this control.
        /// </summary>
        public string Sprite
        {
            set => _drawingSprite = _resourceCache.GetSprite(value);
        }

        /// <summary>
        ///     Color to mix the sprite with.
        /// </summary>
        public Color Color
        {
            get => _drawingSprite?.Color ?? Color.White;
            set => _drawingSprite.Color = value;
        }

        /// <summary>
        ///     Constructs an instance of this class.
        /// </summary>
        public SimpleImage()
        {
            _resourceCache = IoCManager.Resolve<IResourceCache>();
        }

        /// <inheritdoc />
        public override void Render()
        {
            base.Render();

            _drawingSprite.Draw(CluwneLib.CurrentRenderTarget, new RenderStates(BlendMode.Alpha));
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _drawingSprite = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public override void Resize()
        {
            var fr = _drawingSprite.GetLocalBounds().Convert();
            _size = new Vector2i((int) fr.Width, (int) fr.Height);
            _clientArea = Box2i.FromDimensions(0, 0, _size.X, _size.Y);

            base.Resize();

            if (_drawingSprite != null)
                _drawingSprite.Position = new Vector2f(_screenPos.X, _screenPos.Y);
        }
    }
}
