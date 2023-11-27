using System;
using Robust.Shared.Graphics;

namespace Robust.Client.Audio;

/// <summary>
/// Has the metadata for a particular audio stream as well as the relevant internal handle to it.
/// </summary>
public sealed class AudioStream
{
    public TimeSpan Length { get; }
    internal IClydeHandle? ClydeHandle { get; }
    public string? Name { get; }
    public string? Title { get; }
    public string? Artist { get; }
    public int ChannelCount { get; }

    internal AudioStream(IClydeHandle? handle, TimeSpan length, int channelCount, string? name = null, string? title = null, string? artist = null)
    {
        ClydeHandle = handle;
        Length = length;
        ChannelCount = channelCount;
        Name = name;
        Title = title;
        Artist = artist;
    }
}
