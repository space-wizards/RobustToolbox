using SS14.Client.Interfaces.Resource;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared.IoC;
using System;
using System.Drawing;
using SFML.Window;

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
			Color = Color.White;
			Update(0);
		}

		public Color Color { get; set; }

		public BlendingMode BlendingMode
		{
			get
			{
				return _buttonNormal != null ? _buttonNormal.BlendingMode : BlendingMode.None;
			}
			set
			{
				if (_buttonNormal != null)
					_buttonNormal.BlendingMode = value;

				if (_buttonHover != null)
					_buttonHover.BlendingMode = value;

				if (_buttonClick != null)
					_buttonClick.BlendingMode = value;
			}
		}

		public string ImageNormal
		{
			get
			{
				if (_buttonNormal != null) return _buttonNormal.Name;
				else return "";
			}
			set { _buttonNormal = _resourceManager.GetSprite(value); }
		}

		public string ImageHover
		{
			get
			{
				if (_buttonHover != null) return _buttonHover.Name;
				else return "";
			}
			set { _buttonHover = _resourceManager.GetSprite(value); }
		}

		public string ImageClick
		{
			get
			{
				if (_buttonClick != null) return _buttonClick.Name;
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
				_drawSprite.Position = Position;
				ClientArea = new Rectangle(Position,
										   new Size((int)_drawSprite.AABB.Width, (int)_drawSprite.AABB.Height));
			}
		}

		public override void Render()
		{
			if (_drawSprite != null)
			{
				_drawSprite.Color = Color;
				_drawSprite.Position = Position;
				_drawSprite.Draw();
				_drawSprite.Color = Color.White;
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
			if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)) && _buttonHover != null)
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
			if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
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
					_drawSprite = ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y))
									  ? _buttonHover
									  : _buttonNormal;
				else
					_drawSprite = _buttonNormal;
			return false;
		}
	}
}