using System;
using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;

namespace Robust.Client.Audio.Sources;

/// <summary>
/// <see cref="IAudioInputDevice"/> backed by an OpenAL capture device (ALC_EXT_CAPTURE).
/// </summary>
internal sealed class OpenAlAudioInput : IAudioInputDevice
{
    private readonly AudioManager _master;
    private ALCaptureDevice _device;
    private bool _disposed;

    public int SampleRate { get; }

    public int AvailableSamples
    {
        get
        {
            if (_disposed)
                return 0;

            return ALC.GetInteger(_device, AlcGetInteger.CaptureSamples);
        }
    }

    public OpenAlAudioInput(AudioManager master, ALCaptureDevice device, int sampleRate)
    {
        _master = master;
        _device = device;
        SampleRate = sampleRate;
        ALC.CaptureStart(_device);
    }

    public int Read(Span<short> buffer)
    {
        if (_disposed)
            return 0;

        var toRead = Math.Min(AvailableSamples, buffer.Length);
        if (toRead <= 0)
            return 0;

        // ALC.CaptureSamples will read exactly 'toRead' samples; we clamp to availability above so it never underflows.
        ALC.CaptureSamples(_device, ref MemoryMarshal.GetReference(buffer), toRead);
        return toRead;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ALC.CaptureStop(_device);
        ALC.CaptureCloseDevice(_device);
        _device = ALCaptureDevice.Null;
    }
}
