using System;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.Graphics
{
    internal interface IClydeAudioInternal : IClydeAudio
    {
        bool InitializePostWindowing();
        void FrameProcess(FrameEventArgs eventArgs);
        void Shutdown();
    }
}
