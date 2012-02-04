using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using SS13_Shared;
using SS13.Modules;
using CGO;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using GorgonLibrary.Graphics;
using ClientResourceManager;

namespace SS13.UserInterface
{
    public class ContextMenu : GuiComponent
    {
        Vector2D mousePos;
        List<ContextMenuButton> buttons = new List<ContextMenuButton>();
        Entity OwningEntity;

        private readonly Vector2D ButtonSize = new Vector2D(150, 20);

        public ContextMenu(Entity entity, Vector2D creationPos, bool showExamine = true) 
            : base ()
        {
            OwningEntity = entity;

            List<ContextMenuEntry> entries = new List<ContextMenuEntry>();
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();

            entity.SendMessage(this, SS13_Shared.GO.ComponentMessageType.ContextGetEntries, replies);

            if (replies.Any())
                entries = (List<ContextMenuEntry>)replies.First(x => x.messageType == SS13_Shared.GO.ComponentMessageType.ContextGetEntries).paramsList[0];

            if (showExamine)
            {
                ContextMenuButton examineButton = new ContextMenuButton(new ContextMenuEntry { componentMessage = "examine", entryName = "Examine", iconName = "context_eye" }, ButtonSize);
                examineButton.Selected += new ContextMenuButton.ContextPressHandler(Context_Selected);
                buttons.Add(examineButton);
                examineButton.Update();
            }

            foreach (ContextMenuEntry entry in entries)
            {
                ContextMenuButton newButton = new ContextMenuButton(entry, ButtonSize);
                newButton.Selected += new ContextMenuButton.ContextPressHandler(Context_Selected);
                buttons.Add(newButton);
                newButton.Update();
            }

            float currY = creationPos.Y;
            foreach (ContextMenuButton button in buttons)
            {
                button.Position = new Point((int)creationPos.X, (int)currY);
                currY += ButtonSize.Y;
            }
            this.ClientArea = new Rectangle((int)creationPos.X, (int)creationPos.Y, (int)ButtonSize.X, (int)buttons.Count() * (int)ButtonSize.Y);
        }

        void Context_Selected(ContextMenuButton sender)
        {
            if ((string)sender.UserData == "examine")
            {
                ExamineWindow newExamine = new ExamineWindow(new Size(300, 200), OwningEntity);
                UiManager.Singleton.Components.Add(newExamine);
                newExamine.Position = new Point(clientArea.X, clientArea.Y);
            }
            else OwningEntity.SendMessage(this, SS13_Shared.GO.ComponentMessageType.ContextMessage, null, (string)sender.UserData);
        }

        public override void Update()
        {
            base.Update();
            UiManager.Singleton.SetFocus(this);
            foreach (ContextMenuButton button in buttons)
                button.Update();
        }

        public override void Render()
        {
            base.Render();
            foreach (ContextMenuButton button in buttons)
                button.Render();
            Gorgon.Screen.Rectangle(this.clientArea.X, this.clientArea.Y, this.clientArea.Width, this.clientArea.Height, Color.Black);
        }

        public override void Dispose()
        {
            foreach (ContextMenuButton button in buttons)
                button.Dispose();

            buttons.Clear();
            OwningEntity = null;

            UiManager.Singleton.RemoveFocus();
            UiManager.Singleton.Components.Remove(this);

            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return true;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            foreach (ContextMenuButton button in buttons)
                button.MouseUp(e);
            this.Dispose();
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            mousePos = e.Position;
            foreach (ContextMenuButton button in buttons)
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
        public delegate void ContextPressHandler(ContextMenuButton sender);
        public event ContextPressHandler Selected;

        private Sprite iconSprite;
        private Label textLabel;
        private Color currColor = Color.Gray;

        public Vector2D Size;

        public ContextMenuButton(ContextMenuEntry entry, Vector2D size)
            : base()
        {
            this.UserData = entry.componentMessage;
            Size = size;
            iconSprite = ResMgr.Singleton.GetSprite(entry.iconName);
            textLabel = new Label(entry.entryName);
            textLabel.Update();
        }

        public override void Update()
        {
            base.Update();
            this.clientArea = new Rectangle(this.position.X, this.position.Y, (int)Size.X, (int)Size.Y);
            textLabel.Position = new Point(this.clientArea.X + (int)iconSprite.Width + 6, this.clientArea.Y + (int)(this.clientArea.Height / 2f) - (int)(textLabel.ClientArea.Height / 2f));
            textLabel.Update();
        }

        public override void Render()
        {
            base.Render();
            Rectangle iconRect = new Rectangle(this.clientArea.X + 3, this.clientArea.Y + (int)(this.clientArea.Height / 2f) - (int)(iconSprite.Height / 2f), (int)iconSprite.Width, (int)iconSprite.Height);
            Gorgon.Screen.FilledRectangle(this.clientArea.X, this.clientArea.Y, this.clientArea.Width, this.clientArea.Height, currColor);
            textLabel.Render();
            iconSprite.Draw(iconRect);
        }

        public override void Dispose()
        {
            textLabel.Dispose();
            iconSprite = null;
            Selected = null;
            base.Dispose();
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                if (Selected != null) Selected(this);
            return true;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                currColor = Color.LightGray;
            else
                currColor = Color.Gray;
        }
    }
}
