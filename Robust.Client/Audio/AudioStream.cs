using System;
using Robust.Client.Graphics;

namespace Robust.Client.Audio;

public sealed class AudioStream
{
    public TimeSpan Length { get; }
    internal ClydeHandle? ClydeHandle { get; }
    public string? Name { get; }
    public string? Title { get; }
    public string? Artist { get; }
    public int ChannelCount { get; }

    internal AudioStream(ClydeHandle handle, TimeSpan length, int channelCount, string? name = null, string? title = null, string? artist = null)
    {
        ClydeHandle = handle;
        Length = length;
        ChannelCount = channelCount;
        Name = name;
        Title = title;
        Artist = artist;
    }
}
