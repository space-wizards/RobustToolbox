using SS14.Client.Audio;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared.Audio;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using System.Collections.Generic;
using System.IO;
using SS14.Client.Interfaces.Graphics;
using SS14.Shared.Utility;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class AudioSystem : EntitySystem
    {
        [Dependency] ISceneTreeHolder sceneTree;

        [Dependency] IResourceCache resourceCache;

        private IClyde _clyde;

        private uint LastPlayKey = 0;

        private readonly Dictionary<uint, PlayingGodotStream> PlayingGodotStreams =
            new Dictionary<uint, PlayingGodotStream>();

        private readonly List<PlayingClydeStream> PlayingClydeStreams = new List<PlayingClydeStream>();

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
            if (GameController.Mode == GameController.DisplayMode.Clyde)
            {
                _clyde = IoCManager.Resolve<IClyde>();
            }
        }

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<PlayAudioEntityMessage>();
            RegisterMessageType<PlayAudioGlobalMessage>();
            RegisterMessageType<PlayAudioPositionalMessage>();
        }

        public override void FrameUpdate(float frameTime)
        {
            if (GameController.Mode != GameController.DisplayMode.Clyde)
            {
                return;
            }

            // Update positions of streams every frame.
            foreach (var stream in PlayingClydeStreams)
            {
                if (!stream.Source.IsPlaying)
                {
                    stream.Source.Dispose();
                    stream.Done = true;
                    continue;
                }

                if (stream.TrackingCoordinates != null)
                {
                    stream.Source.SetPosition(stream.TrackingCoordinates.Value.ToWorld().Position);
                }
                else if (stream.TrackingEntity != null)
                {
                    stream.Source.SetPosition(stream.TrackingEntity.Transform.WorldPosition);
                }
            }

            PlayingClydeStreams.RemoveAll(p => p.Done);
        }

        /// <summary>
        ///     Play an audio file globally, without position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        public void Play(string filename, AudioParams? audioParams = null)
        {
            Play(resourceCache.GetResource<AudioResource>(new ResourcePath(filename)), audioParams);
        }

        /// <summary>
        ///     Play an audio stream globally, without position.
        /// </summary>
        /// <param name="stream">The audio stream to play.</param>
        public void Play(AudioStream stream, AudioParams? audioParams = null)
        {
            if (GameController.Mode == GameController.DisplayMode.Clyde)
            {
                var source = _clyde.CreateAudioSource(stream);
                source.SetGlobal();
                source.StartPlaying();
                var playing = new PlayingClydeStream
                {
                    Source = source,
                };
                PlayingClydeStreams.Add(playing);
                return;
            }

            if (GameController.Mode != GameController.DisplayMode.Godot)
            {
                return;
            }

            var player = new Godot.AudioStreamPlayer()
            {
                Stream = stream.GodotAudioStream,
                Playing = true,
            };
            if (audioParams != null)
            {
                var val = audioParams.Value;
                player.Bus = val.BusName;
                player.VolumeDb = val.Volume;
                //player.PitchScale = val.PitchScale;
                player.MixTarget = (Godot.AudioStreamPlayer.MixTargetEnum) val.MixTarget;
            }

            sceneTree.WorldRoot.AddChild(player);
            TrackGodotPlayer(player);
        }

        /// <summary>
        ///     Play an audio file following an entity.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        public void Play(string filename, IEntity entity, AudioParams? audioParams = null)
        {
            Play(resourceCache.GetResource<AudioResource>(new ResourcePath(filename)), entity, audioParams);
        }

        /// <summary>
        ///     Play an audio stream following an entity.
        /// </summary>
        /// <param name="stream">The audio stream to play.</param>
        /// <param name="entity">The entity "emitting" the audio.</param>
        public void Play(AudioStream stream, IEntity entity, AudioParams? audioParams = null)
        {
            if (GameController.Mode == GameController.DisplayMode.Clyde)
            {
                var source = _clyde.CreateAudioSource(stream);
                source.SetPosition(entity.Transform.WorldPosition);
                source.StartPlaying();
                var playing = new PlayingClydeStream
                {
                    Source = source,
                    TrackingEntity = entity,
                };
                PlayingClydeStreams.Add(playing);
                return;
            }

            if (GameController.Mode != GameController.DisplayMode.Godot)
            {
                return;
            }

            var parent = entity.GetComponent<IGodotTransformComponent>().SceneNode;
            var player = new Godot.AudioStreamPlayer2D()
            {
                Stream = stream.GodotAudioStream,
                Playing = true
            };
            if (audioParams != null)
            {
                var val = audioParams.Value;
                player.Bus = val.BusName;
                player.VolumeDb = val.Volume;
                //player.PitchScale = val.PitchScale;
                player.Attenuation = val.Attenuation;
                player.MaxDistance = EyeManager.PIXELSPERMETER * val.MaxDistance;
            }

            parent.AddChild(player);
            TrackGodotPlayer(player);
        }

        /// <summary>
        ///     Play an audio file at a static position.
        /// </summary>
        /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        public void Play(string filename, GridCoordinates coordinates, AudioParams? audioParams = null)
        {
            Play(resourceCache.GetResource<AudioResource>(new ResourcePath(filename)), coordinates, audioParams);
        }

        /// <summary>
        ///     Play an audio stream at a static position.
        /// </summary>
        /// <param name="filename">The audio stream to play.</param>
        /// <param name="coordinates">The coordinates at which to play the audio.</param>
        public void Play(AudioStream stream, GridCoordinates coordinates, AudioParams? audioParams = null)
        {
            if (GameController.Mode == GameController.DisplayMode.Clyde)
            {
                var source = _clyde.CreateAudioSource(stream);
                source.SetPosition(coordinates.ToWorld().Position);
                source.StartPlaying();
                var playing = new PlayingClydeStream
                {
                    Source = source,
                    TrackingCoordinates = coordinates
                };
                PlayingClydeStreams.Add(playing);
                return;
            }

            if (GameController.Mode != GameController.DisplayMode.Godot)
            {
                return;
            }

            var player = new Godot.AudioStreamPlayer2D()
            {
                Stream = stream.GodotAudioStream,
                Playing = true,
                // TODO: Handle grid and map of the coordinates.
                Position = (coordinates.Position * EyeManager.PIXELSPERMETER).Convert()
            };
            if (audioParams != null)
            {
                var val = audioParams.Value;
                player.Bus = audioParams.Value.BusName;
                player.VolumeDb = val.Volume;
                //player.PitchScale = val.PitchScale;
                player.Attenuation = val.Attenuation;
                player.MaxDistance = EyeManager.PIXELSPERMETER * val.MaxDistance;
            }

            sceneTree.WorldRoot.AddChild(player);
            TrackGodotPlayer(player);
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            if (!(message is AudioMessage msg))
            {
                return;
            }

            try
            {
                switch (message)
                {
                    case PlayAudioGlobalMessage globalmsg:
                        Play(globalmsg.FileName, globalmsg.AudioParams);
                        break;

                    case PlayAudioEntityMessage entitymsg:
                        if (!EntityManager.TryGetEntity(entitymsg.EntityUid, out var entity))
                        {
                            Logger.Error(
                                $"Server tried to play audio file {entitymsg.FileName} on entity {entitymsg.EntityUid} which does not exist.");
                            break;
                        }

                        Play(entitymsg.FileName, entity, entitymsg.AudioParams);
                        break;

                    case PlayAudioPositionalMessage posmsg:
                        Play(posmsg.FileName, posmsg.Coordinates, posmsg.AudioParams);
                        break;
                }
            }
            catch (FileNotFoundException)
            {
                Logger.Error($"Server tried to play audio file {msg.FileName} which does not exist.");
            }
        }

        private void TrackGodotPlayer(Godot.Node player)
        {
            var key = LastPlayKey++;
            var signal = new GodotGlue.GodotSignalSubscriber0();
            signal.Connect(player, "finished");
            signal.Signal += () => { CleanupAudioPlayer(key); };
            PlayingGodotStreams[key] = new PlayingGodotStream()
            {
                Player = player,
                Signal = signal
            };
        }

        private void CleanupAudioPlayer(uint key)
        {
            var stream = PlayingGodotStreams[key];
            stream.Signal.Disconnect(stream.Player, "finished");
            stream.Signal.Dispose();
            stream.Player.QueueFree();
            stream.Player.Dispose();
            PlayingGodotStreams.Remove(key);
        }

        private struct PlayingGodotStream
        {
            public Godot.Node Player;
            public GodotGlue.GodotSignalSubscriber0 Signal;
        }

        private class PlayingClydeStream
        {
            public IClydeAudioSource Source;
            public IEntity TrackingEntity;
            public GridCoordinates? TrackingCoordinates;
            public bool Done;
        }
    }
}
