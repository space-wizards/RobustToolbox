using System;
using Robust.Client.Graphics.Clyde;

namespace Robust.Client.Audio
{
    public sealed class AudioStream
    {
        public TimeSpan Length { get; }
        internal Clyde.Handle? ClydeHandle { get; }
        public string Name { get; }
        public int ChannelCount { get; }

        internal AudioStream(Clyde.Handle handle, TimeSpan length, int channelCount, string name = null)
        {
            ClydeHandle = handle;
            Length = length;
            ChannelCount = channelCount;
            Name = name;
        }
    }
}
