using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;

namespace SS14.Client.UserInterface.Components
{
    internal class TabbedMenu : GuiComponent
    {
        private readonly IResourceCache _resourceCache;

        private readonly List<KeyValuePair<ImageButton, TabContainer>> _tabs =
            new List<KeyValuePair<ImageButton, TabContainer>>();

        public Vector2i TabOffset = new Vector2i(0, 0);
        private TabContainer _activeTab;
        private string botSprite;
        private string midSprite;

        #pragma warning disable CS0649
        public Vector2 size;
        #pragma warning restore CS0649

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

        public override void Update(float frameTime)
        {
            int prevHeight = 0;

            for (int i = _tabs.Count - 1; i >= 0; i--)
            {
                KeyValuePair<ImageButton, TabContainer> curr = _tabs[i];
                curr.Key.Position = new Vector2i(Position.X + TabOffset.X - curr.Key.ClientArea.Width,
                                              Position.Y + TabOffset.Y - prevHeight);
                prevHeight += curr.Key.ClientArea.Height;

                curr.Value.Position = Position;

                curr.Key.Update(frameTime);
            }

            if (_activeTab != null)
                _activeTab.Update(frameTime);

            ClientArea = new IntRect(Position, new Vector2i((int) size.X, (int) size.Y));
        }

        public override void Render()
        {
            for (int i = _tabs.Count - 1; i >= 0; i--)
            {
                KeyValuePair<ImageButton, TabContainer> curr = _tabs[i];
                Sprite currTabSprite = curr.Value.tabSprite;

                curr.Key.Render();

                if (currTabSprite != null)
                {
                    var bounds = currTabSprite.GetLocalBounds();
                    currTabSprite.Position =
                        new Vector2(curr.Key.Position.X + (curr.Key.ClientArea.Width/2f - bounds.Width/2f),
                                     curr.Key.Position.Y + (curr.Key.ClientArea.Height/2f - bounds.Height/2f));
                    currTabSprite.Draw();
                }
            }

            if (_activeTab != null)
                _activeTab.Render();
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

        public override bool MouseWheelMove(MouseWheelEventArgs e)
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
