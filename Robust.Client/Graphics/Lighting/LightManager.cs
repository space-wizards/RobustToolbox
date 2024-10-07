using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public sealed class LightManager : ILightManager
    {
        public bool Enabled { get; set; } = true;
        public bool DrawShadows { get; set; } = true;
        public bool DrawHardFov { get; set; } = true;
        public bool DrawLighting { get; set; } = true;
        public bool LockConsoleAccess { get; set; } = false;
        public Color AmbientLightColor { get; set; } = Color.FromSrgb(Color.Black);
        public bool NightVision { get; set; } = false;
        public float LightSensitivity { get; set; } = 0f;
        public Color NightVisionColor { get; set; } = new(0.1f, 0.1f, 0.1f);
    }
}
