using SS14.Shared;

namespace SS14.Client.Interfaces.Graphics.Lighting
{
    interface ILightManager
    {
        void Initialize();

        bool Enabled { get; set; }
        bool Deferred { get; }

        void AddLight(ILight light);
        void RemoveLight(ILight light);
    }
}
