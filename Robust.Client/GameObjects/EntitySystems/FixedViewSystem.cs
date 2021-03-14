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

        public override void Initialize()
        {
            base.Initialize();
            _display.OnWindowResized += WindowResized;
            ResizeWindow(_display.ScreenSize);
            _configManager.OnValueChanged(CVars.ViewWidth, _ => ResizeWindow(_display.ScreenSize));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _display.OnWindowResized -= WindowResized;
        }

        private void ResizeWindow(Vector2i dimensions)
        {
            if (dimensions.X == 0 || dimensions.Y == 0) return;

            var yScale = (float) dimensions.Y / EyeManager.PixelsPerMeter / _configManager.GetCVar(CVars.ViewHeight);
            var xScale = (float) dimensions.X / EyeManager.PixelsPerMeter / _configManager.GetCVar(CVars.ViewWidth);
            var scale = MathF.Min(xScale, yScale);

            _eyeManager.CurrentEye.Scale = new Vector2(scale, scale);
        }

        private void WindowResized(WindowResizedEventArgs eventArgs)
        {
            ResizeWindow(eventArgs.NewSize);
        }
    }
}
