using System;
using SS14.Client.GodotGlue;
using SS14.Client.Graphics;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.TabContainer))]
    public class TabContainer : Control
    {
        private new Godot.TabContainer SceneControl;

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
            get => SceneControl.CurrentTab;
            set => SceneControl.CurrentTab = value;
        }

        public TabAlignMode AlignMode
        {
            get => (TabAlignMode) SceneControl.TabAlign;
            set => SceneControl.TabAlign = (Godot.TabContainer.TabAlignEnum) value;
        }

        public bool TabsVisible
        {
            get => SceneControl.TabsVisible;
            set => SceneControl.TabsVisible = value;
        }

        public event Action<int> OnTabSelected;
        public event Action<int> OnTabChanged;

        public string GetTabTitle(int tab)
        {
            return SceneControl.GetTabTitle(tab);
        }

        public void SetTabTitle(int tab, string title)
        {
            SceneControl.SetTabTitle(tab, title);
        }

        public Texture GetTabIcon(int tab)
        {
            return new GodotTextureSource(SceneControl.GetTabIcon(tab));
        }

        public void SetTabIcon(int tab, Texture icon)
        {
            SceneControl.SetTabIcon(tab, icon);
        }

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

        public enum TabAlignMode
        {
            Left = Godot.TabContainer.TabAlignEnum.Left,
            Center = Godot.TabContainer.TabAlignEnum.Center,
            Right = Godot.TabContainer.TabAlignEnum.Right
        }
    }
}
