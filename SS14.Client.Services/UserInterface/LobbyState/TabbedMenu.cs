using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class TabbedMenu : GuiComponent
    {
        private readonly IResourceManager _resourceManager;

        private readonly List<KeyValuePair<ImageButton, TabContainer>> _tabs =
            new List<KeyValuePair<ImageButton, TabContainer>>();

        public Point TabOffset = new Point(0, 0);
        private TabContainer _activeTab;
        private Sprite botSprite;
        private Sprite midSprite;

        public Vector2D size;

        private Sprite topSprite;

        public TabbedMenu()
        {
            _resourceManager = IoCManager.Resolve<IResourceManager>();
            Update(0);
        }

        public string TopSprite
        {
            get { return topSprite != null ? topSprite.Name : null; }
            set { topSprite = _resourceManager.GetSprite(value); }
        }

        public string MidSprite
        {
            get { return midSprite != null ? midSprite.Name : null; }
            set { midSprite = _resourceManager.GetSprite(value); }
        }

        public string BotSprite
        {
            get { return botSprite != null ? botSprite.Name : null; }
            set { botSprite = _resourceManager.GetSprite(value); }
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
                curr.Key.Position = new Point(Position.X + TabOffset.X - curr.Key.ClientArea.Width,
                                              Position.Y + TabOffset.Y - prevHeight);
                prevHeight += curr.Key.ClientArea.Height;

                curr.Value.Position = Position;

                curr.Key.Update(frameTime);
            }

            if (_activeTab != null)
                _activeTab.Update(frameTime);

            ClientArea = new Rectangle(Position, new Size((int) size.X, (int) size.Y));
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
                    currTabSprite.Position =
                        new Vector2D(curr.Key.Position.X + (curr.Key.ClientArea.Width/2f - currTabSprite.Width/2f),
                                     curr.Key.Position.Y + (curr.Key.ClientArea.Height/2f - currTabSprite.Height/2f));
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

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            foreach (var curr in _tabs)
            {
                if (curr.Key.KeyDown(e)) return true;

                if (_activeTab != null)
                    if (_activeTab.KeyDown(e)) return true;
            }
            return base.KeyDown(e);
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            foreach (var curr in _tabs)
            {
                if (curr.Key.MouseWheelMove(e)) return true;

                if (_activeTab != null)
                    if (_activeTab.MouseWheelMove(e)) return true;
            }
            return base.MouseWheelMove(e);
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            foreach (var curr in _tabs)
            {
                curr.Key.MouseMove(e);

                if (_activeTab != null)
                    _activeTab.MouseMove(e);
            }
            base.MouseMove(e);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            foreach (var curr in _tabs)
            {
                if (curr.Key.MouseDown(e)) return true;

                if (_activeTab != null)
                    if (_activeTab.MouseDown(e)) return true;
            }
            return base.MouseDown(e);
        }

        public override bool MouseUp(MouseInputEventArgs e)
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