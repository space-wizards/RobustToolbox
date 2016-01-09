using SS14.Client.Interfaces.Resource;
using System;
using System.Drawing;
using SS14.Client.Graphics.Sprite;
using SFML.Window;
using SFML.Graphics;
using SS14.Client.Graphics;
using Color = SFML.Graphics.Color;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class JobSelectButton : GuiComponent
    {
        #region Delegates

        public delegate void JobButtonPressHandler(JobSelectButton sender);

        #endregion

        private readonly IResourceManager _resourceManager;
        public bool Available = true;
        public bool Selected;
        private Rectangle _buttonArea;

        private Sprite _buttonSprite;
        private TextSprite _descriptionTextSprite;
        private Sprite _jobSprite;

        public JobSelectButton(string text, string spriteName, string description, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            _buttonSprite = _resourceManager.GetSprite("job_button");
            _jobSprite = _resourceManager.GetSprite(spriteName);

            _descriptionTextSprite = new TextSprite("JobButtonDescLabel" + text, text + ":\n" + description,
                                                    _resourceManager.GetFont("CALIBRI"))
                                         {
                                             Color = Color.Black,
                                             ShadowColor = new Color(105, 105, 105),
                                             Shadowed = true,
                                             //ShadowOffset = new Vector2(1, 1)
                                         };

            Update(0);
        }

        public event JobButtonPressHandler Clicked;

        public override sealed void Update(float frameTime)
        {
            var bounds = _buttonSprite.GetLocalBounds();
            _buttonArea = new Rectangle(new Point(Position.X, Position.Y),
                                        new Size((int)bounds.Width, (int)bounds.Height));
            ClientArea = new Rectangle(new Point(Position.X, Position.Y),
                                       new Size((int)bounds.Width + (int) _descriptionTextSprite.Width + 2,
                                                (int)bounds.Height));
            _descriptionTextSprite.Position = new Point(_buttonArea.Right + 2, _buttonArea.Top);
        }

        public override void Render()
        {
            if (!Available)
            {
                _buttonSprite.Color = new Color(128, 0, 0); 
            }
            else if (Selected)
            {
                _buttonSprite.Color = new Color(0, 128, 64);
             
            }
            _buttonSprite.SetTransformToRect(_buttonArea);
            _jobSprite.SetTransformToRect(_buttonArea);
            _buttonSprite.Draw();
            _jobSprite.Draw();
            _descriptionTextSprite.Draw();
            _buttonSprite.Color = Color.White;
        }

        public override void Dispose()
        {
            _descriptionTextSprite = null;
            _buttonSprite = null;
            _jobSprite = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (!Available) return false;
            if (_buttonArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                if (Clicked != null) Clicked(this);
                Selected = true;
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }
    }
}