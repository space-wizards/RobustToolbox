using System;
using System.IO;
using Robust.Client.Audio;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement.ResourceTypes
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
