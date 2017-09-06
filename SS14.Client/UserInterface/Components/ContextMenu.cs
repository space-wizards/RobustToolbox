using SFML.Graphics;
using SFML.System;
using SFML.Window;
using OpenTK;
using OpenTK.Graphics;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;
using System.Collections.Generic;
using System.Linq;
using Vector2i = SS14.Shared.Maths.Vector2i;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.UserInterface.Components
{
    public class ContextMenu : GuiComponent
    {
        private readonly Vector2 _buttonSize = new Vector2(150, 20);
        private readonly List<ContextMenuButton> _buttons = new List<ContextMenuButton>();
        private readonly IResourceCache _resourceCache;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private IEntity _owningEntity;

        public ContextMenu(IEntity entity, Vector2 creationPos, IResourceCache resourceCache,
                           IUserInterfaceManager userInterfaceManager, bool showExamine = true)
        {
            _owningEntity = entity;
            _resourceCache = resourceCache;
            _userInterfaceManager = userInterfaceManager;

            var entries = new List<ContextMenuEntry>();
            var replies = new List<ComponentReplyMessage>();

            entity.SendMessage(this, ComponentMessageType.ContextGetEntries, replies);

            if (replies.Any())
                entries =
                    (List<ContextMenuEntry>)
                    replies.First(x => x.MessageType == ComponentMessageType.ContextGetEntries).ParamsList[0];

            if (showExamine)
            {
                var examineButton =
                    new ContextMenuButton(
                        new ContextMenuEntry
                        { ComponentMessage = "examine", EntryName = "Examine", IconName = "context_eye" }, _buttonSize,
                        _resourceCache);
                examineButton.Selected += ContextSelected;
                _buttons.Add(examineButton);
                examineButton.Update(0);
            }

            foreach (ContextMenuEntry entry in entries)
            {
                var newButton = new ContextMenuButton(entry, _buttonSize, _resourceCache);
                newButton.Selected += ContextSelected;
                _buttons.Add(newButton);
                newButton.Update(0);
            }

            float currY = creationPos.Y;
            foreach (ContextMenuButton button in _buttons)
            {
                button.Position = new Vector2i((int)creationPos.X, (int)currY);
                currY += _buttonSize.Y;
            }
            ClientArea = Box2i.FromDimensions((int)creationPos.X, (int)creationPos.Y, (int)_buttonSize.X,
                                       _buttons.Count() * (int)_buttonSize.Y);
        }

        private void ContextSelected(ContextMenuButton sender)
        {
            if ((string)sender.UserData == "examine")
            {
                var newExamine = new ExamineWindow(new Vector2i(300, 200), _owningEntity, _resourceCache);
                _userInterfaceManager.AddComponent(newExamine);
                newExamine.Position = new Vector2i(ClientArea.Left, ClientArea.Top);
            }
            else
            {
                _owningEntity.SendMessage(this, ComponentMessageType.ContextMessage, (string)sender.UserData);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _userInterfaceManager.SetFocus(this);
            foreach (ContextMenuButton button in _buttons)
                button.Update(frameTime);
        }

        public override void Render()
        {
            base.Render();
            foreach (ContextMenuButton button in _buttons)
                button.Render();
            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height,
                                                   Color4.Black);
        }

        public override void Dispose()
        {
            foreach (ContextMenuButton button in _buttons)
                button.Dispose();

            _buttons.Clear();
            _owningEntity = null;

            _userInterfaceManager.RemoveFocus();
            _userInterfaceManager.RemoveComponent(this);

            base.Dispose();
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            return true;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            foreach (ContextMenuButton button in _buttons)
                button.MouseUp(e);
            Dispose();
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            foreach (ContextMenuButton button in _buttons)
                button.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            return true;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            return true;
        }
    }

    public class ContextMenuButton : GuiComponent
    {
        #region Delegates

        public delegate void ContextPressHandler(ContextMenuButton sender);

        #endregion Delegates

        private readonly IResourceCache _resourceCache;
        private readonly Label _textLabel;

        public Vector2 Size;
        private Color4 _currentColor = Color4.Gray;
        private Sprite _iconSprite;

        public ContextMenuButton(ContextMenuEntry entry, Vector2 size, IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;

            UserData = entry.ComponentMessage;
            Size = size;
            _iconSprite = _resourceCache.GetSprite(entry.IconName);
            _textLabel = new Label(entry.EntryName, "CALIBRI", _resourceCache);
            _textLabel.Update(0);
        }

        public event ContextPressHandler Selected;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var bounds = _iconSprite.GetLocalBounds();
            ClientArea = Box2i.FromDimensions(Position.X, Position.Y, (int)Size.X, (int)Size.Y);
            _textLabel.Position = new Vector2i(ClientArea.Left + (int)bounds.Width + 6,
                                            ClientArea.Top + (int)(ClientArea.Height / 2f) -
                                            (int)(_textLabel.ClientArea.Height / 2f));
            _textLabel.Update(frameTime);
        }

        public override void Render()
        {
            base.Render();
            var bounds = _iconSprite.GetLocalBounds();
            var iconRect = Box2i.FromDimensions(ClientArea.Left + 3,
                                         ClientArea.Top + (int)(ClientArea.Height / 2f) - (int)(bounds.Height / 2f),
                                         (int)bounds.Width, (int)bounds.Height);
            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, _currentColor);
            _textLabel.Render();
            _iconSprite.SetTransformToRect(iconRect);
            _iconSprite.Draw();
        }

        public override void Dispose()
        {
            _textLabel.Dispose();
            _iconSprite = null;
            Selected = null;
            base.Dispose();
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Vector2i(e.X, e.Y)))
                Selected?.Invoke(this);
            return true;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            _currentColor = ClientArea.Contains(new Vector2i(e.X, e.Y))
                                ? new Color4(211, 211, 211, 255)
                                : Color4.Gray;
        }
    }
}
