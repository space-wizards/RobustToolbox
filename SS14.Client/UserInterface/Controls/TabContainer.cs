using System;
using SS14.Client.GodotGlue;
using SS14.Client.Graphics;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.TabContainer))]
    public class TabContainer : Control
    {
        private GodotSignalSubscriber1 _onTabSelectedSubscriber;
        private GodotSignalSubscriber1 _onTabChangedSubscriber;

        public TabContainer()
        {
        }

        public TabContainer(string name) : base(name)
        {
        }

        internal TabContainer(Godot.TabContainer control) : base(control)
        {
        }

        public int CurrentTab
        {
            get => GameController.OnGodot ? (int)SceneControl.Call("get_current_tab") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Call("set_current_tab", value);
                }
            }
        }

        public TabAlignMode AlignMode
        {
            get => GameController.OnGodot ? (TabAlignMode) SceneControl.Call("get_tab_align") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Call("set_tab_align", (Godot.TabContainer.TabAlignEnum) value);
                }
            }
        }

        public bool TabsVisible
        {
            get => GameController.OnGodot ? (bool)SceneControl.Get("tabs_visible") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("tabs_visible", value);
                }
            }
        }

        public event Action<int> OnTabSelected;
        public event Action<int> OnTabChanged;

        public string GetTabTitle(int tab)
        {
            return GameController.OnGodot ? (string)SceneControl.Call("get_tab_title", tab) : default;
        }

        public void SetTabTitle(int tab, string title)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_tab_title", tab, title);
            }
        }

        public Texture GetTabIcon(int tab)
        {
            return GameController.OnGodot ? (Texture)new GodotTextureSource((Godot.Texture)SceneControl.Call("get_tab_icon", tab)) : new BlankTexture();
        }

        public void SetTabIcon(int tab, Texture icon)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("set_tab_icon", tab, icon);
            }
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.TabContainer();
        }

        protected override void SetupSignalHooks()
        {
            base.SetupSignalHooks();

            _onTabSelectedSubscriber = new GodotSignalSubscriber1();
            _onTabSelectedSubscriber.Connect(SceneControl, "tab_selected");
            _onTabSelectedSubscriber.Signal += tab => OnTabSelected?.Invoke((int) tab);

            _onTabChangedSubscriber = new GodotSignalSubscriber1();
            _onTabChangedSubscriber.Connect(SceneControl, "tab_changed");
            _onTabChangedSubscriber.Signal += tab => OnTabChanged?.Invoke((int) tab);
        }


        protected override void DisposeSignalHooks()
        {
            base.DisposeSignalHooks();

            if (_onTabSelectedSubscriber != null)
            {
                _onTabSelectedSubscriber.Disconnect(SceneControl, "tab_selected");
                _onTabSelectedSubscriber.Dispose();
                _onTabSelectedSubscriber = null;
            }

            if (_onTabChangedSubscriber != null)
            {
                _onTabChangedSubscriber.Disconnect(SceneControl, "tab_changed");
                _onTabChangedSubscriber.Dispose();
                _onTabChangedSubscriber = null;
            }
        }

        public enum TabAlignMode
        {
            Left = 0,
            Center = 1,
            Right = 2
        }
    }
}
