using System;

namespace SS14.Client.Audio
{
    public abstract class AudioStream
    {
        internal abstract Godot.AudioStream GodotAudioStream { get; }

        public TimeSpan Length => TimeSpan.FromSeconds(GodotAudioStream.GetLength());
    }

    internal class GodotAudioStreamSource : AudioStream
    {
        internal override Godot.AudioStream GodotAudioStream { get; }

        public GodotAudioStreamSource(Godot.AudioStream godotAudioStream)
        {
            GodotAudioStream = godotAudioStream;
        }
    }
}
