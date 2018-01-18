using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    internal class Listbox : Control
    {
        public delegate void ListboxPressHandler(Label item, Listbox sender);

        private readonly List<string> _contentStrings = new List<string>();
        private readonly int _width;
        private readonly int _dropMaxHeight;
        private Box2i _clientAreaLeft;
        private Box2i _clientAreaMain;
        private Box2i _clientAreaRight;
        private ScrollableContainer _dropDown;
        private ListPanel _listPanel;

        private Sprite _listboxLeft;
        private Sprite _listboxMain;
        private Sprite _listboxRight;
        private TextSprite _selectedLabel;

        public Label CurrentlySelected { get; private set; }

        public Listbox(int dropDownLength, int width, List<string> initialOptions = null)
        {
            _width = width;
            _dropMaxHeight = dropDownLength;
            _listboxLeft = _resourceCache.GetSprite("button_left");
            _listboxMain = _resourceCache.GetSprite("button_middle");
            _listboxRight = _resourceCache.GetSprite("button_right");

            _selectedLabel = new TextSprite("", _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font)
            { FillColor = Color4.Black };

            _dropDown = new ScrollableContainer(new Vector2i(width, dropDownLength))
            {
                Visible = false,
                Alignment = Align.Bottom,
                LocalPosition = new Vector2i(),
                Parent = this
            };

            if (initialOptions != null)
            {
                _contentStrings = initialOptions;
                RebuildList();
            }
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            _dropDown.Update(frameTime);
        }

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            var listboxLeftBounds = _listboxLeft.LocalBounds;
            var listboxMainBounds = _listboxMain.LocalBounds;
            var listboxRightBounds = _listboxRight.LocalBounds;

            _clientAreaLeft = Box2i.FromDimensions(new Vector2i(), new Vector2i((int)listboxLeftBounds.Width, (int)listboxLeftBounds.Height));
            _clientAreaMain = Box2i.FromDimensions(_clientAreaLeft.Right, 0, _width, (int)listboxMainBounds.Height);
            _clientAreaRight = Box2i.FromDimensions(new Vector2i(_clientAreaMain.Right, 0), new Vector2i((int)listboxRightBounds.Width, (int)listboxRightBounds.Height));

            _clientArea = Box2i.FromDimensions(new Vector2i(), new Vector2i(_clientAreaLeft.Width + _clientAreaMain.Width + _clientAreaRight.Width, Math.Max(Math.Max(_clientAreaLeft.Height, _clientAreaRight.Height), _clientAreaMain.Height)));
        }

        /// <inheritdoc />
        protected override void OnCalcPosition()
        {
            base.OnCalcPosition();

            _selectedLabel.Position = Position + new Vector2i(_clientAreaLeft.Right, 0 + (int)(ClientArea.Height / 2f) - (int)(_selectedLabel.Height / 2f));
        }

        /// <inheritdoc />
        public override void Draw()
        {
            _listboxLeft.SetTransformToRect(_clientAreaLeft.Translated(Position));
            _listboxMain.SetTransformToRect(_clientAreaMain.Translated(Position));
            _listboxRight.SetTransformToRect(_clientAreaRight.Translated(Position));

            _listboxLeft.Draw();
            _listboxMain.Draw();
            _listboxRight.Draw();

            _selectedLabel.Draw();

            base.Draw();

            // drop down covers children, prob want a better way to do this
            _dropDown.Draw();
        }

        /// <inheritdoc />
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
        }

        /// <inheritdoc />
        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (_dropDown.MouseDown(e))
                return true;

            if (base.MouseDown(e))
                return true;

            if (ClientArea.Translated(Position).Contains(e.X, e.Y))
            {
                _dropDown.Visible = !_dropDown.Visible;

                if (_dropDown.Visible)
                    IoCManager.Resolve<IUserInterfaceManager>().SetFocus(_dropDown);

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (_dropDown.MouseUp(e))
                return true;

            return base.MouseUp(e);
        }

        /// <inheritdoc />
        public override void MouseMove(MouseMoveEventArgs e)
        {
            _dropDown.MouseMove(e);
            base.MouseMove(e);
        }

        public event ListboxPressHandler ItemSelected;

        public void AddItem(string str)
        {
            _contentStrings.Add(str);
            RebuildList();
        }

        /// <summary>
        ///     Removes all items from the listbox
        /// </summary>
        public void ClearItems()
        {
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

            var selLabel = _dropDown.Components
                .Where(a => a.GetType() == typeof(ListboxItem))
                .Select(a => new { a, b = (ListboxItem)a })
                .Where(t => string.Equals(t.b.Text, str, StringComparison.InvariantCultureIgnoreCase))
                .Select(t => t.b)
                .FirstOrDefault();

            if (selLabel != null)
                SetItem(selLabel, raiseEvent);
        }

        private void RebuildList()
        {
            CurrentlySelected = null;
            _dropDown.Container.DisposeAllChildren();

            _listPanel = new ListPanel()
            { DrawBackground = false };
            _dropDown.Container.AddControl(_listPanel);

            foreach (var newEntry in _contentStrings.Select(str => new ListboxItem(str, _width)))
            {
                newEntry.Parent = _listPanel;
                newEntry.Clicked += NewEntryClicked;
            }

            // shrink _dropDown to fit the contents.
            _dropDown.Container.DoLayout();
            var size = _dropDown.Container.GetShrinkBounds();
            _dropDown.Height = Math.Min(size.Height, _dropMaxHeight);
            _dropDown.DoLayout();
        }

        private void NewEntryClicked(Label sender, MouseButtonEventArgs e)
        {
            SetItem(sender, true);
        }

        private void SetItem(Label toSet, bool raiseEvent = false)
        {
            if (ItemSelected != null && raiseEvent) ItemSelected(toSet, this);

            CurrentlySelected = toSet;
            _selectedLabel.Text = toSet.Text;
            _dropDown.Visible = false;

            ((ListboxItem)toSet).Selected = true;
            var notSelected = _listPanel.Children
                .Cast<ListboxItem>()
                .Where(item => item != toSet);

            foreach (var item in notSelected)
            {
                item.Selected = false;
            }
        }
    }

    /// <summary>
    ///     A line entry in the listbox.
    /// </summary>
    internal class ListboxItem : Label
    {
        // TODO: Make selections work
        public bool Selected;

        public ListboxItem(string text, int maxWidth)
            : base(text, "CALIBRI")
        {
            FixedWidth = maxWidth;
            DrawBorder = true;
            DrawBackground = true;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            base.MouseMove(e);

            // mouseover color
            if (ClientArea.Translated(Position).Contains(e.X, e.Y))
                BackgroundColor = new Color4(47, 79, 79, 255);
            else
                BackgroundColor = Color4.Gray;
        }
    }
}
