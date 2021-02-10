using System;
using Robust.Client.Graphics;

namespace Robust.Client.Audio
{
    public sealed class AudioStream
    {
        public TimeSpan Length { get; }
        internal ClydeHandle? ClydeHandle { get; }
        public string? Name { get; }
        public int ChannelCount { get; }

        internal AudioStream(ClydeHandle handle, TimeSpan length, int channelCount, string? name = null)
        {
            ClydeHandle = handle;
            Length = length;
            ChannelCount = channelCount;
            Name = name;
        }
    }
}
