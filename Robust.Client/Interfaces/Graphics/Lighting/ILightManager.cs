using System.Collections.Generic;
using Robust.Shared;

namespace Robust.Client.Interfaces.Graphics.Lighting
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
