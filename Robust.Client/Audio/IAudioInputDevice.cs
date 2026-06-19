using System;

namespace Robust.Client.Audio;

/// <summary>
/// A handle to an opened microphone (OpenAL capture device) that produces mono 16-bit PCM.
/// Used by voice chat to read raw samples for encoding. Dispose to stop and release the device.
/// </summary>
public interface IAudioInputDevice : IDisposable
{
    /// <summary>
    /// The sample rate the device was opened at, in Hz.
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// Number of captured samples currently available to read from the device's internal ring buffer.
    /// </summary>
    int AvailableSamples { get; }

    /// <summary>
    /// Reads captured samples into <paramref name="buffer"/>, up to whichever is smaller of the buffer
    /// length and <see cref="AvailableSamples"/>. Does not block.
    /// </summary>
    /// <returns>The number of samples actually read.</returns>
    int Read(Span<short> buffer);
}
