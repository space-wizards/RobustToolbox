using System;
using SS14.Client.Audio;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Utility;
using System.IO;
using SS14.Client.Interfaces.Graphics;
using SS14.Shared.IoC;

namespace SS14.Client.ResourceManagement
{
    public class AudioResource : BaseResource
    {
        public AudioStream AudioStream { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            if (!cache.ContentFileExists(path))
            {
                throw new FileNotFoundException("Content file does not exist for audio sample.");
            }

            switch (GameController.Mode)
            {
                case GameController.DisplayMode.Headless:
                    AudioStream = new AudioStream();
                    break;
                case GameController.DisplayMode.Godot:
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
                        AudioStream = new AudioStream(stream);
                    }
                    break;
                case GameController.DisplayMode.Clyde:
                    using (var fileStream = cache.ContentFileRead(path))
                    {
                        AudioStream = IoCManager.Resolve<IClyde>().LoadAudioOggVorbis(fileStream);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static implicit operator AudioStream(AudioResource res)
        {
            return res.AudioStream;
        }
    }
}
