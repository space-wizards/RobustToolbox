using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.UserInterface;
using Lidgren.Network;
using SS3D_shared;
using ClientResourceManager;

namespace SS3D.UserInterface
{
    class Listbox : GuiComponent
    {
        GUIElement ListboxMain;
        GUIElement ListboxLeft;
        GUIElement ListboxRight;

        public TextSprite selectedLabel;

        private ScrollableContainer dropDown;
        private List<string> contentStrings = new List<string>();

        public delegate void ListboxPressHandler(Label item);
        public event ListboxPressHandler ItemSelected;

        public Label currentlySelected {get; private set;}

        private Rectangle clientAreaMain;
        private Rectangle clientAreaLeft;
        private Rectangle clientAreaRight;

        public int Width = 0;

        public Listbox(Size dropDownSize, int width, List<string> initialOptions = null)
            : base()
        {
            Width = width;
            ListboxLeft = UiManager.Singleton.Skin.Elements["Controls.Button2.Left"];
            ListboxMain = UiManager.Singleton.Skin.Elements["Controls.Button2.Body"];
            ListboxRight = UiManager.Singleton.Skin.Elements["Controls.Button2.Right"];

            selectedLabel = new TextSprite("ListboxLabel", "", ResMgr.Singleton.GetFont("CALIBRI"));
            selectedLabel.Color = System.Drawing.Color.Black;

            dropDown = new ScrollableContainer("ListboxContents", dropDownSize);
            dropDown.SetVisible(false);

            if (initialOptions != null)
            {
                contentStrings = initialOptions;
                RebuildList();
            }

            Update();
        }

        private void RebuildList()
        {
            currentlySelected = null;
            dropDown.components.Clear();
            int offset = 0;
            foreach (string str in contentStrings)
            {
                ListboxItem newEntry = new ListboxItem(str, Width);
                newEntry.Position = new Point(0, offset);
                newEntry.Update();
                newEntry.Clicked += new Label.LabelPressHandler(newEntry_Clicked);
                dropDown.components.Add(newEntry);
                offset += (int)newEntry.Text.Height;
            }
        }

        void newEntry_Clicked(Label sender)
        {
            if (ItemSelected != null) ItemSelected(sender);

            currentlySelected = sender;
            selectedLabel.Text = sender.Text.Text;
            dropDown.SetVisible(false);

            ((ListboxItem)sender).selected = true;
            var notSelected = from ListboxItem item in dropDown.components
                              where item != sender
                              select item;
            foreach (ListboxItem curr in notSelected) curr.selected = false;

        }

        public override void Update()
        {
            clientAreaLeft = new Rectangle(this.position, new Size(ListboxLeft.Dimensions.Width, ListboxLeft.Dimensions.Height));
            clientAreaMain = new Rectangle(new Point(clientAreaLeft.Right, this.position.Y), new Size(Width, ListboxMain.Dimensions.Height));
            clientAreaRight = new Rectangle(new Point(clientAreaMain.Right, this.position.Y), new Size(ListboxRight.Dimensions.Width, ListboxRight.Dimensions.Height));
            clientArea = new Rectangle(this.position, new Size(clientAreaLeft.Width + clientAreaMain.Width + clientAreaRight.Width, clientAreaMain.Height));
            selectedLabel.Position = new Point(clientAreaLeft.Right, this.position.Y + (int)(clientArea.Height / 2f) - (int)(selectedLabel.Height / 2f));
            dropDown.Position = new Point(clientArea.X + (int)((clientArea.Width - dropDown.ClientArea.Width) / 2f), clientArea.Bottom);
            dropDown.Update();
        }

        public override void Render()
        {
            dropDown.Render();
            ListboxLeft.Draw(clientAreaLeft);
            ListboxMain.Draw(clientAreaMain);
            ListboxRight.Draw(clientAreaRight);
            selectedLabel.Draw();
        }

        public override void Dispose()
        {
            contentStrings.Clear();
            dropDown.Dispose();
            dropDown = null;
            selectedLabel = null;
            ListboxLeft = null;
            ListboxMain = null;
            ListboxRight = null;
            ItemSelected = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y))) //change to clientAreaRight when theres a proper skin with an arrow to the right.
            {
                dropDown.ToggleVisible();
                return true;
            }
            else if (dropDown.MouseDown(e) == true) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (dropDown.MouseUp(e) == true) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            dropDown.MouseMove(e);
            return;
        }

    }

    class ListboxItem : Label
    {
        private int width;
        public bool selected = false;

        public ListboxItem(string text, int _width)
            : base(text)
        {
            width = _width;
            drawBorder = true;
            drawBackground = true;
        }

        public override void Update()
        {
            Text.Position = position;
            ClientArea = new Rectangle(this.position, new Size(width, (int)Text.Height));
            if (selected) backgroundColor = System.Drawing.Color.DarkSlateGray;
            else backgroundColor = System.Drawing.Color.Gray;
        }
    }
}
