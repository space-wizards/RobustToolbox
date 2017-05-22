using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.UserInterface.Components
{
    public class ContextMenu : GuiComponent
    {
        private readonly Vector2f _buttonSize = new Vector2f(150, 20);
        private readonly List<ContextMenuButton> _buttons = new List<ContextMenuButton>();
        private readonly IResourceManager _resourceManager;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private Entity _owningEntity;

        public ContextMenu(Entity entity, Vector2f creationPos, IResourceManager resourceManager,
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
                button.Position = new Vector2i((int) creationPos.X, (int) currY);
                currY += _buttonSize.Y;
            }
            ClientArea = new IntRect((int) creationPos.X, (int) creationPos.Y, (int) _buttonSize.X,
                                       _buttons.Count()*(int) _buttonSize.Y);
        }

        private void ContextSelected(ContextMenuButton sender)
        {
            if ((string) sender.UserData == "examine")
            {
                var newExamine = new ExamineWindow(new Vector2i(300, 200), _owningEntity, _resourceManager);
                _userInterfaceManager.AddComponent(newExamine);
                newExamine.Position = new Vector2i(ClientArea.Left, ClientArea.Top);
            }
            else if ((string) sender.UserData == "svars")
            {
                var newSVars = new SVarEditWindow(new Vector2i(350, 400), _owningEntity);
                _userInterfaceManager.AddComponent(newSVars);
                newSVars.Position = new Vector2i(ClientArea.Left, ClientArea.Top);

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
          CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height,
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

        public Vector2f Size;
        private SFML.Graphics.Color _currentColor;
        private Sprite _iconSprite;

        public ContextMenuButton(ContextMenuEntry entry, Vector2f size, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            UserData = entry.ComponentMessage;
            Size = size;
            _currentColor = new SFML.Graphics.Color(128, 128, 128);
            _iconSprite = _resourceManager.GetSprite(entry.IconName);
            _textLabel = new Label(entry.EntryName, "CALIBRI", _resourceManager);
            _textLabel.Update(0);
        }

        public event ContextPressHandler Selected;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var bounds = _iconSprite.GetLocalBounds();
            ClientArea = new IntRect(Position.X, Position.Y, (int) Size.X, (int) Size.Y);
            _textLabel.Position = new Vector2i(ClientArea.Left + (int)bounds.Width + 6,
                                            ClientArea.Top + (int) (ClientArea.Height/2f) -
                                            (int) (_textLabel.ClientArea.Height/2f));
            _textLabel.Update(frameTime);
        }

        public override void Render()
        {
            base.Render();
            var bounds = _iconSprite.GetLocalBounds();
            var iconRect = new IntRect(ClientArea.Left + 3,
                                         ClientArea.Top + (int) (ClientArea.Height/2f) - (int) (bounds.Height/2f),
                                         (int)bounds.Width, (int)bounds.Height);
           CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height,  _currentColor);
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
            if (ClientArea.Contains(e.X, e.Y))
                if (Selected != null) Selected(this);
            return true;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            _currentColor = ClientArea.Contains(e.X, e.Y)
                                ? new SFML.Graphics.Color(211, 211, 211)
                                : new SFML.Graphics.Color(128, 128, 128);
        }
    }
}