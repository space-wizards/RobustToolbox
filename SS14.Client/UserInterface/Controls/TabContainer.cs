using System;
#if GODOT
using SS14.Client.GodotGlue;
#endif
using SS14.Client.Graphics;

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.TabContainer))]
    #endif
    public class TabContainer : Control
    {
        #if GODOT
        private new Godot.TabContainer SceneControl;

        private GodotSignalSubscriber1 _onTabSelectedSubscriber;
        private GodotSignalSubscriber1 _onTabChangedSubscriber;
        #endif

        public TabContainer()
        {
        }

        public TabContainer(string name) : base(name)
        {
        }

        #if GODOT
        internal TabContainer(Godot.TabContainer control) : base(control)
        {
        }
        #endif

        public int CurrentTab
        {
            #if GODOT
            get => SceneControl.CurrentTab;
            set => SceneControl.CurrentTab = value;
            #else
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
            #endif
        }

        public TabAlignMode AlignMode
        {
            #if GODOT
            get => (TabAlignMode) SceneControl.TabAlign;
            set => SceneControl.TabAlign = (Godot.TabContainer.TabAlignEnum) value;
            #else
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
            #endif
        }

        public bool TabsVisible
        {
            #if GODOT
            get => SceneControl.TabsVisible;
            set => SceneControl.TabsVisible = value;
            #else
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
            #endif
        }

        public event Action<int> OnTabSelected;
        public event Action<int> OnTabChanged;

        public string GetTabTitle(int tab)
        {
            #if GODOT
            return SceneControl.GetTabTitle(tab);
            #else
            throw new NotImplementedException();
            #endif
        }

        public void SetTabTitle(int tab, string title)
        {
            #if GODOT
            SceneControl.SetTabTitle(tab, title);
            #endif
        }

        public Texture GetTabIcon(int tab)
        {
            #if GODOT
            return new GodotTextureSource(SceneControl.GetTabIcon(tab));
            #else
            throw new NotImplementedException();
            #endif
        }

        public void SetTabIcon(int tab, Texture icon)
        {
            #if GODOT
            SceneControl.SetTabIcon(tab, icon);
            #endif
        }

        #if GODOT
        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.TabContainer();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(SceneControl = (Godot.TabContainer) control);
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
        #endif

        public enum TabAlignMode
        {
            Left = 0,
            Center = 1,
            Right = 2
        }
    }
}
