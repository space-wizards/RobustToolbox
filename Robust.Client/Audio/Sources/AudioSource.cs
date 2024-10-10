using System;
using System.Numerics;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using Robust.Shared.Audio;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Audio.Sources;

internal sealed class AudioSource : BaseAudioSource
{
    /// <summary>
    /// Underlying stream to the audio.
    /// </summary>
    internal readonly AudioStream SourceStream;

#if DEBUG
    private bool _didPositionWarning;
#endif

    public AudioSource(AudioManager master, int sourceHandle, AudioStream sourceStream) : base(master, sourceHandle)
    {
        SourceStream = sourceStream;
    }

    /// <inheritdoc />
    public override Vector2 Position
    {
        get
        {
            _checkDisposed();
            AL.GetSource(SourceHandle, ALSource3f.Position, out var x, out var y, out _);
            Master._checkAlError();
            return new Vector2(x, y);
        }
        set
        {
            _checkDisposed();

            var (x, y) = value;

            if (!AreFinite(x, y))
            {
                return;
            }
#if DEBUG
            // OpenAL doesn't seem to want to play stereo positionally.
            // Log a warning if people try to.
            if (SourceStream.ChannelCount > 1 && !_didPositionWarning)
            {
                _didPositionWarning = true;
                Master.OpenALSawmill.Warning("Attempting to set position on audio source with multiple audio channels! Stream: '{0}'.  Make sure the audio is MONO, not stereo.",
                    SourceStream.Name);
                // warning isn't enough, people just ignore it :(
                DebugTools.Assert(false, $"Attempting to set position on audio source with multiple audio channels! Stream: '{SourceStream.Name}'. Make sure the audio is MONO, not stereo.");
            }
#endif

            AL.Source(SourceHandle, ALSource3f.Position, x, y, 0);
            Master._checkAlError();
        }
    }

    ~AudioSource()
    {
        Dispose(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            // We can't run this code inside the finalizer thread so tell Clyde to clear it up later.
            Master.DeleteSourceOnMainThread(SourceHandle, FilterHandle);
        }
        else
        {
            if (FilterHandle != 0)
                EFX.DeleteFilter(FilterHandle);

            AL.DeleteSource(SourceHandle);
            Master.RemoveAudioSource(SourceHandle);
            Master._checkAlError();
        }

        FilterHandle = 0;
        SourceHandle = -1;
    }
}
