using System;
using SS14.Client.GodotGlue;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Utility;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics
{
    internal class DisplayManagerGodot : DisplayManager
    {
        [Dependency] private readonly ISceneTreeHolder _sceneTreeHolder;

        public override Vector2i ScreenSize => (Vector2i)Godot.OS.WindowSize.Convert();

        private GodotSignalSubscriber0 _rootViewportSizeChangedSubscriber;

        public override void SetWindowTitle(string title)
        {
            Godot.OS.SetWindowTitle(title);
        }

        public override void Initialize()
        {
            ReloadConfig();

            _rootViewportSizeChangedSubscriber = new GodotSignalSubscriber0();
            _rootViewportSizeChangedSubscriber.Connect(_sceneTreeHolder.SceneTree.Root, "size_changed");
            _rootViewportSizeChangedSubscriber.Signal += () =>
            {
                // TODO: Uh maybe send oldSize correctly here.
                OnWindowResized?.Invoke(new WindowResizedEventArgs(Vector2i.Zero, ScreenSize));
            };
        }

        protected override void ReloadConfig()
        {
            base.ReloadConfig();

            Godot.OS.VsyncEnabled = VSync;
            Godot.OS.WindowFullscreen = WindowMode == WindowMode.Fullscreen;
        }

        public override event Action<WindowResizedEventArgs> OnWindowResized;
    }
}
