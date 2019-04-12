using System;
using Robust.Client.GodotGlue;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
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

        public override void ReloadConfig()
        {
            base.ReloadConfig();

            Godot.OS.VsyncEnabled = VSync;
            Godot.OS.WindowFullscreen = WindowMode == WindowMode.Fullscreen;
        }

        public override event Action<WindowResizedEventArgs> OnWindowResized;
    }
}
