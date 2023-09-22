using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;

namespace Robust.Client.Audio;

/// <summary>
/// Headless client audio.
/// </summary>
internal sealed class HeadlessAudioManager : SharedAudioManager, IClydeAudioInternal
{
    public void InitializePostWindowing()
    {
        return;
    }

    public void Shutdown()
    {
    }

    public void FlushALDisposeQueues()
    {
        return;
    }

    public IClydeAudioSource CreateAudioSource(AudioResource audioResource)
    {
        return DummyAudioSource.Instance;
    }
}
