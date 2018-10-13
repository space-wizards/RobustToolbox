using SS14.Client.Audio;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Utility;
using System;
using System.IO;

namespace SS14.Client.ResourceManagement
{
    public class AudioResource : BaseResource
    {
        public AudioStream AudioStream { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            #if GODOT
            if (!cache.ContentFileExists(path))
            {
                throw new FileNotFoundException("Content file does not exist for audio sample.");
            }

            using (var fileStream = cache.ContentFileRead(path))
            {
                var stream = new Godot.AudioStreamOGGVorbis()
                {
                    Data = fileStream.ToArray(),
                };
                if (stream.GetLength() == 0)
                {
                    throw new InvalidDataException();
                }
                AudioStream = new GodotAudioStreamSource(stream);
            }
            #endif
        }

        public static implicit operator AudioStream(AudioResource res)
        {
            return res.AudioStream;
        }
    }
}
