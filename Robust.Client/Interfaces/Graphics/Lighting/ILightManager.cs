namespace Robust.Client.Interfaces.Graphics.Lighting
{
    public interface ILightManager
    {
        bool Enabled { get; set; }
        bool DrawShadows { get; set; }
        bool DrawHardFov { get; set; }
    }
}
