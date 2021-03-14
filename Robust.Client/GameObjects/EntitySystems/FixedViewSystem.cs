using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System;

namespace Robust.Client.GameObjects.EntitySystems
{
    internal sealed class FixedViewSystem : EntitySystem
    {
        [Dependency] private readonly IClyde _display = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            _display.OnWindowResized += WindowResized;
            _configManager.OnValueChanged(CVars.ViewWidth, _ => ResizeWindow());
            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
        }

        // We don't want to worry about re-sizing the default eye so we'll only care if it's attached to our mob (or when multi-viewport if it's any of the active eyes).

        public override void Shutdown()
        {
            base.Shutdown();
            _display.OnWindowResized -= WindowResized;
        }

        private void HandlePlayerAttached(PlayerAttachSysMessage msg)
        {
            var player = _playerManager.LocalPlayer?.ControlledEntity;
            if (player == null || !player.TryGetComponent(out EyeComponent? eyeComponent) || eyeComponent.Eye == null) return;
            _eyeManager.CurrentEye = eyeComponent.Eye;

            ResizeWindow();
        }

        private void ResizeWindow(IEye? eye = null)
        {
            if (eye == null)
            {
                var player = _playerManager.LocalPlayer?.ControlledEntity;
                if (player == null || !player.TryGetComponent(out EyeComponent? eyeComp) || eyeComp.Eye == null) return;
                eye = eyeComp.Eye;
            }

            ResizeWindow(eye, _display.ScreenSize);
        }

        // TODO: Not sure how multi-viewport stuff is being done but essentially whenevr any of them is re-sized need to pass them to this.
        // Shouldn't be too big of a refactor?
        private void ResizeWindow(IEye eye, Vector2i dimensions)
        {
            if (dimensions.X == 0 || dimensions.Y == 0) return;

            var yScale = (float) dimensions.Y / EyeManager.PixelsPerMeter / _configManager.GetCVar(CVars.ViewHeight);
            var xScale = (float) dimensions.X / EyeManager.PixelsPerMeter / _configManager.GetCVar(CVars.ViewWidth);
            var scale = MathF.Min(xScale, yScale);

            eye.Scale = new Vector2(scale, scale);
        }

        private void WindowResized(WindowResizedEventArgs eventArgs)
        {
            ResizeWindow();
        }
    }
}
