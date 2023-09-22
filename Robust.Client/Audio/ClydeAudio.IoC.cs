using System.Threading;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client.Audio;

internal partial class AudioManager
{
    [Shared.IoC.Dependency] private readonly IConfigurationManager _cfg = default!;
    [Shared.IoC.Dependency] private readonly IEyeManager _eyeManager = default!;
    [Shared.IoC.Dependency] private readonly ILogManager _logMan = default!;

    private Thread? _gameThread;

    public void InitializePostWindowing()
    {
        _gameThread = Thread.CurrentThread;
        return _initializeAudio();
    }

    public void FrameProcess(FrameEventArgs eventArgs)
    {
        _updateAudio();
    }

    public void Shutdown()
    {
        _shutdownAudio();
    }

    private bool IsMainThread()
    {
        return Thread.CurrentThread == _gameThread;
    }
}
