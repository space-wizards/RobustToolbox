using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using CGO;
using ClientInterfaces;
using ClientInterfaces.GOC;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using GorgonLibrary.Graphics;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    public class ContextMenu : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private readonly Vector2D _buttonSize = new Vector2D(150, 20);
        private readonly List<ContextMenuButton> _buttons = new List<ContextMenuButton>();
        private IEntity _owningEntity;

        public ContextMenu(IEntity entity, Vector2D creationPos, IResourceManager resourceManager, IUserInterfaceManager userInterfaceManager, bool showExamine = true) 
        {
            _owningEntity = entity;
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;

            var entries = new List<ContextMenuEntry>();
            var replies = new List<ComponentReplyMessage>();

            entity.SendMessage(this, SS13_Shared.GO.ComponentMessageType.ContextGetEntries, replies);

            if (replies.Any())
                entries = (List<ContextMenuEntry>)replies.First(x => x.MessageType == SS13_Shared.GO.ComponentMessageType.ContextGetEntries).ParamsList[0];

            if (showExamine)
            {
                var examineButton = new ContextMenuButton(new ContextMenuEntry { ComponentMessage = "examine", EntryName = "Examine", IconName = "context_eye" }, _buttonSize, _resourceManager);
                examineButton.Selected += ContextSelected;
                _buttons.Add(examineButton);
                examineButton.Update();
            }

            foreach (var entry in entries)
            {
                var newButton = new ContextMenuButton(entry, _buttonSize, _resourceManager);
                newButton.Selected += ContextSelected;
                _buttons.Add(newButton);
                newButton.Update();
            }

            var currY = creationPos.Y;
            foreach (var button in _buttons)
            {
                button.Position = new Point((int)creationPos.X, (int)currY);
                currY += _buttonSize.Y;
            }
            ClientArea = new Rectangle((int)creationPos.X, (int)creationPos.Y, (int)_buttonSize.X, _buttons.Count() * (int)_buttonSize.Y);
        }

        void ContextSelected(ContextMenuButton sender)
        {
            if ((string)sender.UserData == "examine")
            {
                var newExamine = new ExamineWindow(new Size(300, 200), _owningEntity, _resourceManager);
                _userInterfaceManager.AddComponent(newExamine);
                newExamine.Position = new Point(ClientArea.X, ClientArea.Y);
            }
            else _owningEntity.SendMessage(this, SS13_Shared.GO.ComponentMessageType.ContextMessage, (string)sender.UserData);
        }

        public override void Update()
        {
            base.Update();
            _userInterfaceManager.SetFocus(this);
            foreach (var button in _buttons)
                button.Update();
        }

        public override void Render()
        {
            base.Render();
            foreach (var button in _buttons)
                button.Render();
            Gorgon.CurrentRenderTarget.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, Color.Black);
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

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return true;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            foreach (var button in _buttons)
                button.MouseUp(e);
            Dispose();
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            foreach (var button in _buttons)
                button.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            return true;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            return true;
        }
    }

    public class ContextMenuButton : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private readonly Label _textLabel;
        private Sprite _iconSprite;
        private Color _currentColor;

        public delegate void ContextPressHandler(ContextMenuButton sender);
        public event ContextPressHandler Selected;

        public Vector2D Size;

        public ContextMenuButton(ContextMenuEntry entry, Vector2D size, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            UserData = entry.ComponentMessage;
            Size = size;
            _currentColor = Color.Gray;
            _iconSprite = _resourceManager.GetSprite(entry.IconName);
            _textLabel = new Label(entry.EntryName, "CALIBRI", _resourceManager);
            _textLabel.Update();
        }

        public override void Update()
        {
            base.Update();
            ClientArea = new Rectangle(Position.X, Position.Y, (int)Size.X, (int)Size.Y);
            _textLabel.Position = new Point(ClientArea.X + (int)_iconSprite.Width + 6, ClientArea.Y + (int)(ClientArea.Height / 2f) - (int)(_textLabel.ClientArea.Height / 2f));
            _textLabel.Update();
        }

        public override void Render()
        {
            base.Render();
            var iconRect = new Rectangle(ClientArea.X + 3, ClientArea.Y + (int)(ClientArea.Height / 2f) - (int)(_iconSprite.Height / 2f), (int)_iconSprite.Width, (int)_iconSprite.Height);
            Gorgon.CurrentRenderTarget.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, _currentColor);
            _textLabel.Render();
            _iconSprite.Draw(iconRect);
        }

        public override void Dispose()
        {
            _textLabel.Dispose();
            _iconSprite = null;
            Selected = null;
            base.Dispose();
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                if (Selected != null) Selected(this);
            return true;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            _currentColor = ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)) ? Color.LightGray : Color.Gray;
        }
    }
}
