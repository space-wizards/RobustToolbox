using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class Listbox : GuiComponent
    {
        #region Delegates

        public delegate void ListboxPressHandler(Label item, Listbox sender);

        #endregion

        private readonly List<string> _contentStrings = new List<string>();
        private readonly IResourceManager _resourceManager;
        private readonly int _width;
        private Rectangle _clientAreaLeft;
        private Rectangle _clientAreaMain;
        private Rectangle _clientAreaRight;
        private ScrollableContainer _dropDown;

        private Sprite _listboxLeft;
        private Sprite _listboxMain;
        private Sprite _listboxRight;
        private TextSprite _selectedLabel;

        public Listbox(int dropDownLength, int width, IResourceManager resourceManager,
                       List<string> initialOptions = null)
        {
            _resourceManager = resourceManager;

            _width = width;
            _listboxLeft = _resourceManager.GetSprite("button_left");
            _listboxMain = _resourceManager.GetSprite("button_middle");
            _listboxRight = _resourceManager.GetSprite("button_right");

            _selectedLabel = new TextSprite("ListboxLabel", "", _resourceManager.GetFont("CALIBRI"))
                                 {Color = Color.Black};

            _dropDown = new ScrollableContainer("ListboxContents", new Size(width, dropDownLength), _resourceManager);
            _dropDown.SetVisible(false);

            if (initialOptions != null)
            {
                _contentStrings = initialOptions;
                RebuildList();
            }

            Update(0);
        }

        public Label CurrentlySelected { get; private set; }
        public event ListboxPressHandler ItemSelected;

        public void AddItem(string str)
        {
            _contentStrings.Add(str);
            RebuildList();
        }

        public void RemoveItem(string str)
        {
            if (!_contentStrings.Contains(str)) return;

            _contentStrings.Remove(str);
            RebuildList();
        }

        public void SelectItem(string str, bool raiseEvent = false)
        {
            ListboxItem selLabel = (from a in _dropDown.components
                                    where a.GetType() == typeof (ListboxItem)
                                    let b = (ListboxItem) a
                                    where b.Text.Text.ToLowerInvariant() == str.ToLowerInvariant()
                                    select b).FirstOrDefault();

            if (selLabel != null)
                SetItem(selLabel, raiseEvent);
        }

        private void RebuildList()
        {
            CurrentlySelected = null;
            _dropDown.components.Clear();
            int offset = 0;
            foreach (
                ListboxItem newEntry in _contentStrings.Select(str => new ListboxItem(str, _width, _resourceManager)))
            {
                newEntry.Position = new Point(0, offset);
                newEntry.Update(0);
                newEntry.Clicked += NewEntryClicked;
                _dropDown.components.Add(newEntry);
                offset += (int) newEntry.Text.Height;
            }
        }

        private void NewEntryClicked(Label sender, MouseInputEventArgs e)
        {
            SetItem(sender, true);
        }

        private void SetItem(Label toSet, bool raiseEvent = false)
        {
            if (ItemSelected != null && raiseEvent) ItemSelected(toSet, this);

            CurrentlySelected = toSet;
            _selectedLabel.Text = toSet.Text.Text;
            _dropDown.SetVisible(false);

            ((ListboxItem) toSet).Selected = true;
            IEnumerable<ListboxItem> notSelected = from ListboxItem item in _dropDown.components
                                                   where item != toSet
                                                   select item;
            foreach (ListboxItem curr in notSelected) curr.Selected = false;
        }

        public override sealed void Update(float frameTime)
        {
            _clientAreaLeft = new Rectangle(Position, new Size((int) _listboxLeft.Width, (int) _listboxLeft.Height));
            _clientAreaMain = new Rectangle(new Point(_clientAreaLeft.Right, Position.Y),
                                            new Size(_width, (int) _listboxMain.Height));
            _clientAreaRight = new Rectangle(new Point(_clientAreaMain.Right, Position.Y),
                                             new Size((int) _listboxRight.Width, (int) _listboxRight.Height));
            ClientArea = new Rectangle(Position,
                                       new Size(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width,
                                                Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height),
                                                         _clientAreaMain.Height)));
            _selectedLabel.Position = new Point(_clientAreaLeft.Right,
                                                Position.Y + (int) (ClientArea.Height/2f) -
                                                (int) (_selectedLabel.Height/2f));
            _dropDown.Position = new Point(ClientArea.X + (int) ((ClientArea.Width - _dropDown.ClientArea.Width)/2f),
                                           ClientArea.Bottom);
            _dropDown.Update(frameTime);
        }

        public override void Render()
        {
            _dropDown.Render();
            _listboxLeft.Draw(_clientAreaLeft);
            _listboxMain.Draw(_clientAreaMain);
            _listboxRight.Draw(_clientAreaRight);
            _selectedLabel.Draw();
        }

        public override void Dispose()
        {
            _contentStrings.Clear();
            _dropDown.Dispose();
            _dropDown = null;
            _selectedLabel = null;
            _listboxLeft = null;
            _listboxMain = null;
            _listboxRight = null;
            ItemSelected = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
                //change to clientAreaRight when theres a proper skin with an arrow to the right.
            {
                var UiMgr = IoCManager.Resolve<IUserInterfaceManager>();
                _dropDown.ToggleVisible();
                if (_dropDown.IsVisible()) UiMgr.SetFocus(_dropDown);
                return true;
            }

            return _dropDown.MouseDown(e);
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return _dropDown.MouseUp(e);
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            _dropDown.MouseMove(e);
        }
    }

    internal class ListboxItem : Label
    {
        private readonly int _width;
        public bool Selected;

        public ListboxItem(string text, int width, IResourceManager resourceManager)
            : base(text, "CALIBRI", resourceManager)
        {
            _width = width;
            DrawBorder = true;
            DrawBackground = true;
        }

        public override void Update(float frameTime)
        {
            Text.Position = Position;
            ClientArea = new Rectangle(Position, new Size(_width, (int) Text.Height));
            BackgroundColor = Selected ? Color.DarkSlateGray : Color.Gray;
        }
    }
}