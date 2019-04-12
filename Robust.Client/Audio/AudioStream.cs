using System;
using Robust.Client.Graphics.Clyde;

namespace Robust.Client.Audio
{
    public sealed class AudioStream
    {
        public TimeSpan Length { get; }
        internal Godot.AudioStream GodotAudioStream { get; }
        internal Clyde.Handle? ClydeHandle { get; }

        // Constructor used on headless.
        internal AudioStream()
        {
            Length = TimeSpan.Zero;
        }

        // Constructor used on Clyde.
        internal AudioStream(Clyde.Handle handle, TimeSpan length)
        {
            ClydeHandle = handle;
            Length = length;
        }

        // Constructor used on Godot.
        internal AudioStream(Godot.AudioStream godotStream)
        {
            GodotAudioStream = godotStream;
            Length = TimeSpan.FromSeconds(godotStream.GetLength());
        }
    }
}
