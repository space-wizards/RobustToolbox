using SS14.Shared;

namespace SS14.Client.Interfaces.Graphics.Lighting
{
    interface ILightManager
    {
        void Initialize();

        bool Enabled { get; set; }

        ILight MakeLight();
        IOccluder MakeOccluder();
        void FrameProcess(FrameEventArgs args);
    }
}
