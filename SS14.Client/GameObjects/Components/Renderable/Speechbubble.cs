using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using System;
using System.Text;
using SS14.Client.ResourceManagement;

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
        private readonly Text _textSprite;

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
            _textSprite = new Text(String.Empty, _resourceCache.GetResource<FontResource>("Fonts/CALIBRI.TTF").Font);
            _textSprite.Color = Color.Black;
            // TODO Word wrap!
            _textSprite.Position = new Vector2f(5, 3);
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

            var bubbleBounds = _bubbleSprite.GetLocalBounds();
            var spriteBounds = spriteToDrawAbove.GetLocalBounds();

            float x = position.X - windowOrigin.X - (bubbleBounds.Width / 2.0f);
            float y = position.Y - windowOrigin.Y - (bubbleBounds.Height) - (spriteBounds.Height / 2.0f) - 5.0f;

            _bubbleSprite.Position = new Vector2f(x, y);
            _bubbleSprite.Draw();
        }

        public void Draw(Vector2 position, Vector2 windowOrigin, FloatRect boundingBox)
        {
            if ((DateTime.Now - _buildTime).TotalMilliseconds >= MillisecondsToLive) return;

            var bubbleBounds = _bubbleSprite.GetLocalBounds();

            float x = position.X - windowOrigin.X - (bubbleBounds.Width / 2.0f);
            float y = position.Y - windowOrigin.Y - (bubbleBounds.Height) - (boundingBox.Height / 2.0f) - 5.0f;

            _bubbleSprite.Position = new Vector2f(x, y);
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

            _textSprite.DisplayedString = _stringBuilder.ToString();
            _stringBuilder.Clear();

            DrawBubbleSprite();
        }

        #endregion

        private void DrawBubbleSprite()
        {
            // TODO unfuck this
            /*RenderTarget originalTarget = CluwneLib.CurrentRenderTarget;
            Sprite cornerSprite = _resourceCache.GetSprite("corners");

            //Set up dimensions
            _bubbleRender.SetDimensions((int) _textSprite.Size.X + 10, (int) _textSprite.Size.Y + 10);
            _bubbleSprite.SetSize(_textSprite.Size.X + 10, _textSprite.Size.Y + 10);

            //BEGIN RENDERING
            CluwneLib.CurrentRenderTarget = _bubbleRender;
            _bubbleRender.Clear(Color.Transparent);

            //Draw black triangle at the bottom.
            var pointOneBlack = new Vector2((_bubbleRender.Width/2) - 10, _bubbleRender.Height - 10);
            var pointTwoBlack = new Vector2((_bubbleRender.Width/2) + 10, _bubbleRender.Height - 10);
            var pointThreeBlack = new Vector2((_bubbleRender.Width/2), _bubbleRender.Height);
            _bubbleRender.FilledTriangle(pointOneBlack, pointTwoBlack, pointThreeBlack, Color.Black);

            //Draw the side lines
            _bubbleRender.Line(10, 0, _bubbleRender.Width - 20, 1, Color.Black);
            _bubbleRender.Line(_bubbleRender.Width - 1, 10, 1, _bubbleRender.Height - 26, Color.Black);
            _bubbleRender.Line(10, _bubbleRender.Height - 7, _bubbleRender.Width - 20, 1, Color.Black);
            _bubbleRender.Line(0, 10, 1, _bubbleRender.Height - 26, Color.Black);

            //Fill in the middle without polluting the corners.
            _bubbleRender.FilledRectangle(3, 1, _bubbleRender.Width - 6, _bubbleRender.Height - 8, Color.White);
            _bubbleRender.FilledRectangle(1, 3, _bubbleRender.Width - 2, _bubbleRender.Height - 12, Color.White);

            //Draw the white triangle at the bottom.
            Vector2 pointOneWhite = pointOneBlack + new Vector2(1, 0);
            Vector2 pointTwoWhite = pointTwoBlack - new Vector2(1, 0);
            Vector2 pointThreeWhite = pointThreeBlack - new Vector2(0, 1);
            _bubbleRender.FilledTriangle(pointOneWhite, pointTwoWhite, pointThreeWhite, Color.White);

            //Draw the corners.
            cornerSprite.SourceBlend = AlphaBlendOperation.One;
            cornerSprite.DestinationBlend = AlphaBlendOperation.Zero;
            cornerSprite.VerticalFlip = true;
            cornerSprite.SetPosition(0, 0);
            cornerSprite.Draw();
            cornerSprite.HorizontalFlip = true;
            cornerSprite.SetPosition(_bubbleRender.Width - 16, 0);
            cornerSprite.Draw();
            cornerSprite.VerticalFlip = false;
            cornerSprite.SetPosition(_bubbleRender.Width - 16, _bubbleRender.Height - 22);
            cornerSprite.Draw();
            cornerSprite.HorizontalFlip = false;
            cornerSprite.SetPosition(0, _bubbleRender.Height - 22);
            cornerSprite.Draw();

            _textSprite.Draw();

            CluwneLib.CurrentRenderTarget = originalTarget;

            _buildTime = DateTime.Now;*/
        }

        #endregion
    }
}
