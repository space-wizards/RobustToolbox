/*
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using System;
using System.Text;
using SS14.Client.ResourceManagement;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    public class SpeechBubble
    {
        #region Fields

        /// <summary>
        /// Constant lifetime of speech bubble in milliseconds.
        /// TODO: Consider making configurable?
        /// </summary>
        private const double MillisecondsToLive = 8000;

        /// <summary>
        /// Holder for built bubble render image.
        /// Rebuilt upon text change.
        /// </summary>
        private readonly RenderImage _bubbleRender;

        /// <summary>
        /// Holder for built bubble sprite.
        /// Rebuilt upon text change.
        /// </summary>
        private readonly Sprite _bubbleSprite;

        /// <summary>
        /// Owner mob unique name.
        /// </summary>
        private readonly string _mobName;

        /// <summary>
        /// Reference to Resource Manager service to prevent
        /// calling IoCManager every time speechbubble is drawn.
        /// </summary>
        private readonly IResourceCache _resourceCache;

        /// <summary>
        /// StringBuilder to handle
        /// </summary>
        private readonly StringBuilder _stringBuilder;

        /// <summary>
        /// TextSprite used to hold and display speech text.
        /// </summary>
        private readonly TextSprite _textSprite;

        /// <summary>
        /// Holder for last time the sprite was
        /// built. Used to detect if speech bubble
        /// has expired.
        /// </summary>
        private DateTime _buildTime;

        #endregion

        #region Methods

        #region Constructor

        public SpeechBubble(string mobname)
        {
            _resourceCache = IoCManager.Resolve<IResourceCache>();
            _mobName = mobname;
            _buildTime = DateTime.Now;
            _textSprite = new TextSprite(String.Empty, _resourceCache.GetResource<FontResource>("Fonts/CALIBRI.TTF").Font);
            _textSprite.FillColor = Color.Black;
            // TODO Word wrap!
            _textSprite.Position = new Vector2(5, 3);
            _stringBuilder = new StringBuilder();

            _bubbleRender = new RenderImage("bubble ",1, 1);
            _bubbleSprite = new Sprite(_bubbleRender.Texture);
        }

        #endregion

        #region Privates

        #endregion

        #region Publics

        public void Draw(Vector2 position, Vector2 windowOrigin, Sprite spriteToDrawAbove)
        {
            if ((DateTime.Now - _buildTime).TotalMilliseconds >= MillisecondsToLive) return;

            var bubbleBounds = _bubbleSprite.LocalBounds;
            var spriteBounds = spriteToDrawAbove.LocalBounds;

            float x = position.X - windowOrigin.X - (bubbleBounds.Width / 2.0f);
            float y = position.Y - windowOrigin.Y - (bubbleBounds.Height) - (spriteBounds.Height / 2.0f) - 5.0f;

            _bubbleSprite.Position = new Vector2(x, y);
            _bubbleSprite.Draw();
        }

        public void Draw(Vector2 position, Vector2 windowOrigin, Box2 boundingBox)
        {
            if ((DateTime.Now - _buildTime).TotalMilliseconds >= MillisecondsToLive) return;

            var bubbleBounds = _bubbleSprite.LocalBounds;

            float x = position.X - windowOrigin.X - (bubbleBounds.Width / 2.0f);
            float y = position.Y - windowOrigin.Y - (bubbleBounds.Height) - (boundingBox.Height / 2.0f) - 5.0f;

            _bubbleSprite.Position = new Vector2(x, y);
            _bubbleSprite.Draw();
        }

        public void SetText(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && i%50 == 0)
                    _stringBuilder.Append("\n" + text[i]);
                else
                    _stringBuilder.Append(text[i]);
            }

            _textSprite.Text = _stringBuilder.ToString();
            _stringBuilder.Clear();

            DrawBubbleSprite();
        }

        #endregion

        private void DrawBubbleSprite()
        {
            // TODO implement this
        }

        #endregion
    }
}
*/
