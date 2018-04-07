using SS14.Shared;

namespace SS14.Client.Interfaces.Graphics.Lighting
{
    public interface ILightManager
    {
        void Initialize();

        bool Enabled { get; set; }

        ILight MakeLight();
        IOccluder MakeOccluder();
        void FrameUpdate(RenderFrameEventArgs args);
    }
}
