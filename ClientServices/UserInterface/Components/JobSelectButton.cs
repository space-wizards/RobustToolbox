using System;
using System.Drawing;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
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
                                             ShadowColor = Color.DimGray,
                                             Shadowed = true,
                                             ShadowOffset = new Vector2D(1, 1)
                                         };

            Update(0);
        }

        public event JobButtonPressHandler Clicked;

        public override sealed void Update(float frameTime)
        {
            _buttonArea = new Rectangle(new Point(Position.X, Position.Y),
                                        new Size((int) _buttonSprite.Width, (int) _buttonSprite.Height));
            ClientArea = new Rectangle(new Point(Position.X, Position.Y),
                                       new Size((int) _buttonSprite.Width + (int) _descriptionTextSprite.Width + 2,
                                                (int) _buttonSprite.Height));
            _descriptionTextSprite.Position = new Point(_buttonArea.Right + 2, _buttonArea.Top);
        }

        public override void Render()
        {
            if (!Available)
            {
                _buttonSprite.Color = Color.DarkRed;
            }
            else if (Selected)
            {
                _buttonSprite.Color = Color.DarkSeaGreen;
            }
            _buttonSprite.Draw(_buttonArea);
            _jobSprite.Draw(_buttonArea);
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

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (!Available) return false;
            if (_buttonArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                if (Clicked != null) Clicked(this);
                Selected = true;
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}