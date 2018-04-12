using SS14.Client.Audio;
using SS14.Client.Interfaces.ResourceManagement;
using System.IO;

namespace SS14.Client.ResourceManagement
{
    public class AudioResource : BaseResource
    {
        public AudioStream AudioStream { get; private set; }

        public override void Load(IResourceCache cache, string diskPath)
        {
            if (!File.Exists(diskPath))
            {
                throw new FileNotFoundException(diskPath);
            }

            var data = File.ReadAllBytes(diskPath);
            var stream = new Godot.AudioStreamOGGVorbis()
            {
                Data = data
            };
            if (stream.GetLength() == 0)
            {
                throw new InvalidDataException();
            }
            AudioStream = new GodotAudioStreamSource(stream);
            Shared.Log.Logger.Debug($"{stream.GetLength()}");
        }

        public static implicit operator AudioStream(AudioResource res)
        {
            return res.AudioStream;
        }
    }
}
