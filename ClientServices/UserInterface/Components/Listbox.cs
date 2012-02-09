using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using ClientInterfaces;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class Listbox : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        Sprite ListboxMain;
        Sprite ListboxLeft;
        Sprite ListboxRight;

        public TextSprite selectedLabel;

        private ScrollableContainer dropDown;
        private List<string> contentStrings = new List<string>();

        public delegate void ListboxPressHandler(Label item);
        public event ListboxPressHandler ItemSelected;

        public Label currentlySelected {get; private set;}

        private Rectangle clientAreaMain;
        private Rectangle clientAreaLeft;
        private Rectangle clientAreaRight;

        public int Width;

        public Listbox(Size dropDownSize, int width, IResourceManager resourceManager, List<string> initialOptions = null)
        {
            _resourceManager = resourceManager;

            Width = width;
            ListboxLeft = _resourceManager.GetSprite("button_left");
            ListboxMain = _resourceManager.GetSprite("button_middle");
            ListboxRight = _resourceManager.GetSprite("button_right");

            selectedLabel = new TextSprite("ListboxLabel", "", _resourceManager.GetFont("CALIBRI"));
            selectedLabel.Color = System.Drawing.Color.Black;

            dropDown = new ScrollableContainer("ListboxContents", dropDownSize, _resourceManager);
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
                ListboxItem newEntry = new ListboxItem(str, Width, _resourceManager);
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

            ((ListboxItem)sender).Selected = true;
            var notSelected = from ListboxItem item in dropDown.components
                              where item != sender
                              select item;
            foreach (ListboxItem curr in notSelected) curr.Selected = false;

        }

        public override void Update()
        {
            clientAreaLeft = new Rectangle(this.Position, new Size((int)ListboxLeft.Width, (int)ListboxLeft.Height));
            clientAreaMain = new Rectangle(new Point(clientAreaLeft.Right, this.Position.Y), new Size(Width, (int)ListboxMain.Height));
            clientAreaRight = new Rectangle(new Point(clientAreaMain.Right, this.Position.Y), new Size((int)ListboxRight.Width, (int)ListboxRight.Height));
            ClientArea = new Rectangle(this.Position, new Size(clientAreaLeft.Width + clientAreaMain.Width + clientAreaRight.Width, clientAreaMain.Height));
            selectedLabel.Position = new Point(clientAreaLeft.Right, this.Position.Y + (int)(ClientArea.Height / 2f) - (int)(selectedLabel.Height / 2f));
            dropDown.Position = new Point(ClientArea.X + (int)((ClientArea.Width - dropDown.ClientArea.Width) / 2f), ClientArea.Bottom);
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
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y))) //change to clientAreaRight when theres a proper skin with an arrow to the right.
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
        }

    }

    class ListboxItem : Label
    {
        private readonly int _width;
        public bool Selected;

        public ListboxItem(string text, int width, IResourceManager resourceManager)
            : base(text, resourceManager)
        {
            _width = width;
            DrawBorder = true;
            DrawBackground = true;
        }

        public override void Update()
        {
            Text.Position = Position;
            ClientArea = new Rectangle(Position, new Size(_width, (int)Text.Height));
            BackgroundColor = Selected ? Color.DarkSlateGray : Color.Gray;
        }
    }
}
