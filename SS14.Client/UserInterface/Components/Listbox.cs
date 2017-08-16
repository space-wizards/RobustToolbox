using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.ResourceManagement;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    internal class Listbox : GuiComponent
    {
        #region Delegates

        public delegate void ListboxPressHandler(Label item, Listbox sender);

        #endregion

        private readonly List<string> _contentStrings = new List<string>();
        private readonly IResourceCache _resourceCache;
        private readonly int _width;
        private Box2i _clientAreaLeft;
        private Box2i _clientAreaMain;
        private Box2i _clientAreaRight;
        private ScrollableContainer _dropDown;

        private Sprite _listboxLeft;
        private Sprite _listboxMain;
        private Sprite _listboxRight;
        private TextSprite _selectedLabel;

        public Listbox(int dropDownLength, int width, IResourceCache resourceCache,
                       List<string> initialOptions = null)
        {
            _resourceCache = resourceCache;

            _width = width;
            _listboxLeft = _resourceCache.GetSprite("button_left");
            _listboxMain = _resourceCache.GetSprite("button_middle");
            _listboxRight = _resourceCache.GetSprite("button_right");

            _selectedLabel = new TextSprite("ListboxLabel", "", _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font)
                                 {Color = Color.Black};

            _dropDown = new ScrollableContainer("ListboxContents", new Vector2i(width, dropDownLength), _resourceCache);
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

        /// <summary>
        /// Removes all items from the listbox
        /// </summary>
        public void ClearItems() {
            _contentStrings.Clear();
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
            str = str ?? "str";

            ListboxItem selLabel = (from a in _dropDown.components
                                    where a.GetType() == typeof (ListboxItem)
                                    let b = (ListboxItem) a
                                    where b.Text.Text.ToLowerInvariant() == str.ToLowerInvariant()
                                    select b).FirstOrDefault();

            if (selLabel != null)
            {
                SetItem(selLabel, raiseEvent);
            }
        }

        private void RebuildList()
        {
            CurrentlySelected = null;
            _dropDown.components.Clear();
            int offset = 0;
            foreach (
                ListboxItem newEntry in _contentStrings.Select(str => new ListboxItem(str, _width, _resourceCache)))
            {
                newEntry.Position = new Vector2i(0, offset);
                newEntry.Update(0);
                newEntry.Clicked += NewEntryClicked;
                _dropDown.components.Add(newEntry);
                offset += (int) newEntry.Text.Height;
            }
        }

        private void NewEntryClicked(Label sender, MouseButtonEventArgs e)
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
            var listboxLeftBounds = _listboxLeft.GetLocalBounds();
            var listboxMainBounds = _listboxMain.GetLocalBounds();
            var listboxRightBounds = _listboxRight.GetLocalBounds();
            _clientAreaLeft = Box2i.FromDimensions(Position, new Vector2i((int)listboxLeftBounds.Width, (int)listboxLeftBounds.Height));
            _clientAreaMain = Box2i.FromDimensions(_clientAreaLeft.Right, Position.Y,
                                          _width, (int)listboxMainBounds.Height);
            _clientAreaRight = Box2i.FromDimensions(new Vector2i(_clientAreaMain.Right, Position.Y),
                                             new Vector2i((int)listboxRightBounds.Width, (int)listboxRightBounds.Height));
            ClientArea = Box2i.FromDimensions(Position,
                                       new Vector2i(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width,
                                                Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height),
                                                         _clientAreaMain.Height)));
            _selectedLabel.Position = new Vector2i(_clientAreaLeft.Right,
                                                Position.Y + (int) (ClientArea.Height/2f) -
                                                (int) (_selectedLabel.Height/2f));
            _dropDown.Position = new Vector2i(ClientArea.Left + (int) ((ClientArea.Width - _dropDown.ClientArea.Width)/2f),
                                           ClientArea.Bottom);
            _dropDown.Update(frameTime);
        }

        public override void Render()
        {
            _dropDown.Render();
            _listboxLeft.SetTransformToRect(_clientAreaLeft);
            _listboxMain.SetTransformToRect(_clientAreaMain);
            _listboxRight.SetTransformToRect(_clientAreaRight);
            _listboxLeft.Draw();
            _listboxMain.Draw();
            _listboxRight.Draw();
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

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
                //change to clientAreaRight when theres a proper skin with an arrow to the right.
            {
                var UiMgr = IoCManager.Resolve<IUserInterfaceManager>();
                _dropDown.ToggleVisible();
                if (_dropDown.IsVisible()) UiMgr.SetFocus(_dropDown);
                return true;
            }

            return _dropDown.MouseDown(e);
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return _dropDown.MouseUp(e);
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            _dropDown.MouseMove(e);
        }
    }

    internal class ListboxItem : Label
    {
        private readonly int _width;
        public bool Selected;

        public ListboxItem(string text, int width, IResourceCache resourceCache)
            : base(text, "CALIBRI", resourceCache)
        {
            _width = width;
            DrawBorder = true;
            DrawBackground = true;
        }

        public override void Update(float frameTime)
        {
            Text.Position = Position;
            ClientArea = Box2i.FromDimensions(Position, new Vector2i(_width, (int) Text.Height));
            BackgroundColor = Selected ? new Color(47, 79, 79) : new Color(128, 128, 128);
        }
    }
}
