using System;
using System.Collections.Generic;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Client.UserInterface.Components;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using Vector2i = SS14.Shared.Maths.Vector2i;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.UserInterface.CustomControls
{
    internal class TabbedMenu : Control
    {
        private readonly List<KeyValuePair<ImageButton, TabContainer>> _tabs =
            new List<KeyValuePair<ImageButton, TabContainer>>();

        public Vector2i TabOffset = new Vector2i(0, 0);
        private TabContainer _activeTab;
        private string botSprite;
        private string midSprite;
        private string topSprite;

        public TabbedMenu()
        {
            _resourceCache = IoCManager.Resolve<IResourceCache>();
            Update(0);
        }

        public string TopSprite
        {
            get { return topSprite; }
            set { topSprite = value; }
        }

        public string MidSprite
        {
            get { return midSprite; }
            set { midSprite = value; }
        }

        public string BotSprite
        {
            get { return botSprite; }
            set { botSprite = value; }
        }

        public void SelectTab(TabContainer tab)
        {
            if (_tabs.Exists(x => x.Value == tab))
            {
                _activeTab = tab;
                _activeTab.Activated();
            }
        }

        public void RemoveTab(TabContainer remTab)
        {
            _tabs.RemoveAll(x => x.Value == remTab);
            rebuildButtonIcons();
        }

        public void AddTab(TabContainer newTab)
        {
            var newButton = new ImageButton();
            newButton.Clicked += tabButton_Clicked;

            _tabs.Add(new KeyValuePair<ImageButton, TabContainer>(newButton, newTab));
            rebuildButtonIcons();
        }

        private void tabButton_Clicked(ImageButton sender)
        {
            if (_tabs.Exists(x => x.Key == sender))
            {
                KeyValuePair<ImageButton, TabContainer> tab = _tabs.Find(x => x.Key == sender);
                SelectTab(tab.Value);
            }
        }

        private void rebuildButtonIcons()
        {
            for (int i = _tabs.Count - 1; i >= 0; i--)
            {
                KeyValuePair<ImageButton, TabContainer> curr = _tabs[i];
                if (i == _tabs.Count - 1)
                {
                    curr.Key.ImageNormal = BotSprite;
                }
                else if (i == 0)
                {
                    curr.Key.ImageNormal = TopSprite;
                }
                else
                {
                    curr.Key.ImageNormal = MidSprite;
                }
            }
        }

        protected override void OnCalcRect()
        {
            ClientArea = Box2i.FromDimensions(Position, new Vector2i(Size.X, Size.Y));

            foreach (var kvTab in _tabs)
            {
                kvTab.Key.DoLayout();
                kvTab.Value.DoLayout();
            }
        }
        
        public override void Update(float frameTime)
        {
            int prevHeight = 0;

            // update the image buttons
            for (int i = _tabs.Count - 1; i >= 0; i--)
            {
                KeyValuePair<ImageButton, TabContainer> curr = _tabs[i];
                curr.Key.Position = new Vector2i(Position.X + TabOffset.X - curr.Key.ClientArea.Width,
                                              Position.Y + TabOffset.Y - prevHeight);
                prevHeight += curr.Key.ClientArea.Height;

                curr.Value.Position = Position;

                curr.Key.Update(frameTime);
            }

            _activeTab?.Update(frameTime);
        }

        public override void Draw()
        {
            for (int i = _tabs.Count - 1; i >= 0; i--)
            {
                KeyValuePair<ImageButton, TabContainer> curr = _tabs[i];
                Sprite currTabSprite = curr.Value.TabSprite;

                curr.Key.Draw();

                if (currTabSprite != null)
                {
                    var bounds = currTabSprite.LocalBounds;
                    currTabSprite.Position =
                        new Vector2(curr.Key.Position.X + (curr.Key.ClientArea.Width / 2f - bounds.Width / 2f),
                                    curr.Key.Position.Y + (curr.Key.ClientArea.Height / 2f - bounds.Height / 2f));
                    currTabSprite.Draw();
                }
            }
        
            if (_activeTab != null)
            {
                _activeTab.DrawBorder = true;
                _activeTab.Draw();
            }
        }

        public override void Dispose()
        {
            _tabs.ForEach(b => b.Key.Clicked -= tabButton_Clicked);
            _tabs.ForEach(b => b.Key.Dispose());
            _tabs.ForEach(t => t.Value.Dispose());
            _tabs.Clear();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            foreach (var curr in _tabs)
            {
                if (curr.Key.KeyDown(e)) return true;

                if (_activeTab != null)
                    if (_activeTab.KeyDown(e)) return true;
            }
            return base.KeyDown(e);
        }

        public override bool MouseWheelMove(MouseWheelScrollEventArgs e)
        {
            foreach (var curr in _tabs)
            {
                if (curr.Key.MouseWheelMove(e)) return true;

                if (_activeTab != null)
                    if (_activeTab.MouseWheelMove(e)) return true;
            }
            return base.MouseWheelMove(e);
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            foreach (var curr in _tabs)
            {
                curr.Key.MouseMove(e);

                if (_activeTab != null)
                    _activeTab.MouseMove(e);
            }
            base.MouseMove(e);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            foreach (var curr in _tabs)
            {
                if (curr.Key.MouseDown(e)) return true;

                if (_activeTab != null)
                    if (_activeTab.MouseDown(e)) return true;
            }
            return base.MouseDown(e);
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            foreach (var curr in _tabs)
            {
                if (curr.Key.MouseUp(e)) return true;

                if (_activeTab != null)
                    if (_activeTab.MouseUp(e)) return true;
            }
            return base.MouseUp(e);
        }
    }
}
