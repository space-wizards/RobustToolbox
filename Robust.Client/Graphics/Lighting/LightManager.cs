using Robust.Client.Interfaces.Graphics.Lighting;

namespace Robust.Client.Graphics.Lighting
{
    public sealed class LightManager : ILightManager
    {
        public bool Enabled { get; set; } = true;
        public bool DrawShadows { get; set; } = true;
        public bool DrawHardFov { get; set; } = true;
    }
}
