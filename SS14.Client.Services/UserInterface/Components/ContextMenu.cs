using SFML.Window;
using SS14.Client.GameObjects;
using SS14.Client.Graphics.CluwneLib;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.Maths;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SS14.Client.Services.UserInterface.Components
{
    public class ContextMenu : GuiComponent
    {
        private readonly Vector2 _buttonSize = new Vector2(150, 20);
        private readonly List<ContextMenuButton> _buttons = new List<ContextMenuButton>();
        private readonly IResourceManager _resourceManager;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private Entity _owningEntity;

        public ContextMenu(Entity entity, Vector2 creationPos, IResourceManager resourceManager,
                           IUserInterfaceManager userInterfaceManager, bool showExamine = true)
        {
            _owningEntity = entity;
            _resourceManager = resourceManager;
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
                            {ComponentMessage = "examine", EntryName = "Examine", IconName = "context_eye"}, _buttonSize,
                        _resourceManager);
                examineButton.Selected += ContextSelected;
                _buttons.Add(examineButton);
                examineButton.Update(0);
            }

            var sVarButton =
                new ContextMenuButton(
                    new ContextMenuEntry {ComponentMessage = "svars", EntryName = "SVars", IconName = "context_eye"},
                    _buttonSize, _resourceManager);
            sVarButton.Selected += ContextSelected;
            _buttons.Add(sVarButton);
            sVarButton.Update(0);

            foreach (ContextMenuEntry entry in entries)
            {
                var newButton = new ContextMenuButton(entry, _buttonSize, _resourceManager);
                newButton.Selected += ContextSelected;
                _buttons.Add(newButton);
                newButton.Update(0);
            }

            float currY = creationPos.Y;
            foreach (ContextMenuButton button in _buttons)
            {
                button.Position = new Point((int) creationPos.X, (int) currY);
                currY += _buttonSize.Y;
            }
            ClientArea = new Rectangle((int) creationPos.X, (int) creationPos.Y, (int) _buttonSize.X,
                                       _buttons.Count()*(int) _buttonSize.Y);
        }

        private void ContextSelected(ContextMenuButton sender)
        {
            if ((string) sender.UserData == "examine")
            {
                var newExamine = new ExamineWindow(new Size(300, 200), _owningEntity, _resourceManager);
                _userInterfaceManager.AddComponent(newExamine);
                newExamine.Position = new Point(ClientArea.X, ClientArea.Y);
            }
            else if ((string) sender.UserData == "svars")
            {
                var newSVars = new SVarEditWindow(new Size(350, 400), _owningEntity);
                _userInterfaceManager.AddComponent(newSVars);
                newSVars.Position = new Point(ClientArea.X, ClientArea.Y);

                _owningEntity.GetComponent<ISVarsComponent>(ComponentFamily.SVars).GetSVarsCallback +=
                    newSVars.GetSVarsCallback;
                _owningEntity.GetComponent<ISVarsComponent>(ComponentFamily.SVars).DoGetSVars();
            }
            else _owningEntity.SendMessage(this, ComponentMessageType.ContextMessage, (string) sender.UserData);
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
          CluwneLib.drawRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height,
                                                 Color.Black);
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

        #endregion

        private readonly IResourceManager _resourceManager;
        private readonly Label _textLabel;

        public Vector2 Size;
        private Color _currentColor;
		private CluwneSprite _iconSprite;

        public ContextMenuButton(ContextMenuEntry entry, Vector2 size, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            UserData = entry.ComponentMessage;
            Size = size;
            _currentColor = Color.Gray;
            _iconSprite = _resourceManager.GetSprite(entry.IconName);
            _textLabel = new Label(entry.EntryName, "CALIBRI", _resourceManager);
            _textLabel.Update(0);
        }

        public event ContextPressHandler Selected;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            ClientArea = new Rectangle(Position.X, Position.Y, (int) Size.X, (int) Size.Y);
            _textLabel.Position = new Point(ClientArea.X + (int) _iconSprite.Width + 6,
                                            ClientArea.Y + (int) (ClientArea.Height/2f) -
                                            (int) (_textLabel.ClientArea.Height/2f));
            _textLabel.Update(frameTime);
        }

        public override void Render()
        {
            base.Render();
            var iconRect = new Rectangle(ClientArea.X + 3,
                                         ClientArea.Y + (int) (ClientArea.Height/2f) - (int) (_iconSprite.Height/2f),
                                         (int) _iconSprite.Width, (int) _iconSprite.Height);
           CluwneLib.drawRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height,  _currentColor);
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

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
                if (Selected != null) Selected(this);
            return true;
        }

		public override void MouseMove(MouseMoveEventArgs e)
        {
            _currentColor = ClientArea.Contains(new Point((int) e.X, (int) e.Y))
                                ? Color.LightGray
                                : Color.Gray;
        }
    }
}