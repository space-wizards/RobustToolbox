using SS14.Client.Interfaces.Resource;
using SS14.Client.Graphics.Sprite;
using SS14.Shared.IoC;
using System;
using SFML.Window;
using SFML.Graphics;
using System.Drawing;
using SS14.Shared.Maths;
using SColor = System.Drawing.Color;

namespace SS14.Client.Services.UserInterface.Components
{
	public class ImageButton : GuiComponent
	{
		#region Delegates

		public delegate void ImageButtonPressHandler(ImageButton sender);

		#endregion

		private readonly IResourceManager _resourceManager;
		private CluwneSprite _buttonClick;
		private CluwneSprite _buttonHover;
		private CluwneSprite _buttonNormal;

		private CluwneSprite _drawSprite;

		public ImageButton()
		{
			_resourceManager = IoCManager.Resolve<IResourceManager>();
            Color = SColor.White;
			Update(0);
		}

        public SColor Color { get; set; }

		public BlendMode BlendSettings
		{
			get
			{
				return _buttonNormal != null ? _buttonNormal.BlendSettings: BlendMode.Alpha;
			}
			set
			{
				if (_buttonNormal != null)
                    _buttonNormal.BlendSettings = value;

				if (_buttonHover != null)
                    _buttonHover.BlendSettings = value;

				if (_buttonClick != null)
                    _buttonClick.BlendSettings = value;
			}
		}

		public string ImageNormal
		{
			get
			{
				if (_buttonNormal != null) return _buttonNormal.Key;
				else return "";
			}
			set { _buttonNormal = _resourceManager.GetSprite(value); }
		}

		public string ImageHover
		{
			get
			{
				if (_buttonHover != null) return _buttonHover.Key;
				else return "";
			}
			set { _buttonHover = _resourceManager.GetSprite(value); }
		}

		public string ImageClick
		{
			get
			{
				if (_buttonClick != null) return _buttonClick.Key;
				else return "";
			}
			set { _buttonClick = _resourceManager.GetSprite(value); }
		}

		public event ImageButtonPressHandler Clicked;

		public override sealed void Update(float frameTime)
		{
			if (_drawSprite == null && _buttonNormal != null)
				_drawSprite = _buttonNormal;

			if (_drawSprite != null)
			{
				_drawSprite.Position = new Vector2( Position.X,Position.Y);
				ClientArea = new Rectangle(Position,
										   new Size((int)_drawSprite.Width, (int)_drawSprite.Height));
			}
		}

		public override void Render()
		{
			if (_drawSprite != null)
			{
				_drawSprite.Color = Color;
				_drawSprite.Position = new Vector2 (Position.X,Position.Y);
                _drawSprite.Smoothing = true;
				_drawSprite.Draw();
                _drawSprite.Color = Color;
			}
		}

		public override void Dispose()
		{
			_buttonNormal = null;
			_buttonHover = null;
			_buttonClick = null;
			Clicked = null;
			base.Dispose();
			GC.SuppressFinalize(this);
		}

		public override void MouseMove(MouseMoveEventArgs e)
		{
			if (ClientArea.Contains(new Point((int)e.X, (int)e.Y)) && _buttonHover != null)
			{
				if (_drawSprite != _buttonClick)
					_drawSprite = _buttonHover;
			}
			else
			{
				if (_drawSprite != _buttonClick)
					_drawSprite = _buttonNormal;
			}
		}

		public override bool MouseDown(MouseButtonEventArgs e)
		{
			if (ClientArea.Contains(new Point((int)e.X, (int)e.Y)))
			{
				if (_buttonClick != null) _drawSprite = _buttonClick;
				if (Clicked != null) Clicked(this);
				return true;
			}
			return false;
		}

		public override bool MouseUp(MouseButtonEventArgs e)
		{
			if (_drawSprite == _buttonClick)
				if (_buttonHover != null)
					_drawSprite = ClientArea.Contains(new Point((int)e.X, (int)e.Y))
									  ? _buttonHover
									  : _buttonNormal;
				else
					_drawSprite = _buttonNormal;
			return false;
		}
	}
}