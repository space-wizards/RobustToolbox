using System;

namespace SS14.Client.Audio
{
    public abstract class AudioStream
    {
        #if GODOT
        internal abstract Godot.AudioStream GodotAudioStream { get; }

        public TimeSpan Length => TimeSpan.FromSeconds(GodotAudioStream.GetLength());
        #else
        public TimeSpan Length => throw new NotImplementedException();
        #endif
    }

    #if GODOT
    internal class GodotAudioStreamSource : AudioStream
    {
        internal override Godot.AudioStream GodotAudioStream { get; }

        public GodotAudioStreamSource(Godot.AudioStream godotAudioStream)
        {
            GodotAudioStream = godotAudioStream;
        }
    }
    #endif
}
