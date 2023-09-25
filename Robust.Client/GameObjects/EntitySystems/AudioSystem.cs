using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading.Tasks;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.ResourceManagement.ResourceTypes;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed class AudioSystem : SharedAudioSystem
{
    [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IClientResourceCache _resourceCache = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IParallelManager _parMan = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
    [Dependency] private readonly IAudioInternal _audio = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <summary>
    /// Per-tick cache of relevant streams.
    /// </summary>
    private readonly List<(EntityUid Entity, AudioComponent Component, TransformComponent Xform)> _streams = new();

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private float _maxRayLength;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<AudioComponent, ComponentStartup>(OnAudioStartup);
        SubscribeLocalEvent<AudioComponent, ComponentShutdown>(OnAudioShutdown);
        SubscribeLocalEvent<AudioComponent, EntityPausedEvent>(OnAudioPaused);
        SubscribeLocalEvent<AudioComponent, EntityUnpausedEvent>(OnAudioUnpaused);

        CfgManager.OnValueChanged(CVars.AudioRaycastLength, OnRaycastLengthChanged, true);
    }

    /// <summary>
    /// Sets the volume for the entire game.
    /// </summary>
    public void SetMasterVolume(float value)
    {
        _audio.SetMasterVolume(value);
    }

    public override void Shutdown()
    {
        CfgManager.UnsubValueChanged(CVars.AudioRaycastLength, OnRaycastLengthChanged);
        base.Shutdown();
    }

    private void OnAudioPaused(EntityUid uid, AudioComponent component, ref EntityPausedEvent args)
    {
        // TODO: OpenAL scrubbing through audio.
    }

    private void OnAudioUnpaused(EntityUid uid, AudioComponent component, ref EntityUnpausedEvent args)
    {
        // TODO: OpenAL scrubbing through audio.
    }

    private void OnAudioStartup(EntityUid uid, AudioComponent component, ComponentStartup args)
    {
        if (!Timing.IsFirstTimePredicted || !TryGetAudio(component.FileName, out var audioResource))
            return;

        var source = _audio.CreateAudioSource(audioResource);

        if (source == null)
            return;

        component.Source = source;
    }

    private void OnAudioShutdown(EntityUid uid, AudioComponent component, ComponentShutdown args)
    {
        // Breaks with prediction?
        component.Source.Dispose();
    }

    private void OnRaycastLengthChanged(float value)
    {
        _maxRayLength = value;
    }

    public override void FrameUpdate(float frameTime)
    {
        var eye = _eyeManager.CurrentEye;
        _audio.SetRotation(eye.Rotation);
        _audio.SetPosition(eye.Position.Position);

        var ourPos = eye.Position;
        var opts = new ParallelOptions { MaxDegreeOfParallelism = _parMan.ParallelProcessCount };

        var query = AllEntityQuery<AudioComponent, TransformComponent>();
        _streams.Clear();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            _streams.Add((uid, comp, xform));
        }

        try
        {
            Parallel.ForEach(_streams, opts, comp => ProcessStream(comp.Entity, comp.Component, comp.Xform, ourPos));
        }
        catch (Exception e)
        {
            Log.Error($"Caught exception while processing entity streams.");
            _runtimeLog.LogException(e, $"{nameof(AudioSystem)}.{nameof(FrameUpdate)}");
        }
        finally
        {
            foreach (var stream in _streams)
            {
                var (entity, comp, _) = stream;

                if (comp.Done)
                {
                    QueueDel(entity);
                }
            }
        }
    }

    private void ProcessStream(EntityUid entity, AudioComponent component, TransformComponent xform, MapCoordinates listener)
    {
        if (!component.Playing)
        {
            component.Done = true;
            return;
        }

        // If it's global but on another map (that isn't nullspace) then stop playing it.
        if (component.Global)
        {
            if (xform.MapID != MapId.Nullspace && listener.MapId != xform.MapID)
            {
                component.Playing = false;
                return;
            }

            // Resume playing.
            component.StartPlaying();
            return;
        }

        // Non-global sounds, stop playing if on another map.
        // Not relevant to us.
        if (listener.MapId != xform.MapID)
        {
            component.Playing = false;
            return;
        }

        var mapPos = xform.MapPosition;

        // Max distance check
        var delta = mapPos.Position - listener.Position;
        var distance = delta.Length();

        // Out of range so just clip it for us.
        if (distance > component.MaxDistance)
        {
            // Still keeps the source playing, just with no volume.
            component.Source.Gain = 0f;
            return;
        }

        // Update audio occlusion
        var occlusion = GetOcclusion(entity, listener, delta, distance);
        component.Occlusion = occlusion;

        // Update attenuation dependent volume.
        component.Gain = GetPositionalVolume(component, distance);

        // Update audio positions.
        component.Position = mapPos.Position;

        // Make race cars go NYYEEOOOOOMMMMM
        if (_physicsQuery.TryGetComponent(entity, out var physicsComp))
        {
            // This actually gets the tracked entity's xform & iterates up though the parents for the second time. Bit
            // inefficient.
            var velocity = _physics.GetMapLinearVelocity(entity, physicsComp, xform, _xformQuery, _physicsQuery);
            component.Velocity = velocity;
        }
    }

    internal float GetOcclusion(EntityUid entity, MapCoordinates listener, Vector2 delta, float distance)
    {
        float occlusion = 0;

        if (distance > 0.1)
        {
            var rayLength = MathF.Min(distance, _maxRayLength);
            var ray = new CollisionRay(listener.Position, delta / distance, OcclusionCollisionMask);
            occlusion = _physics.IntersectRayPenetration(listener.MapId, ray, rayLength, entity);
        }

        return occlusion;
    }

    internal float GetPositionalVolume(AudioComponent component, float distance)
    {
        // OpenAL also limits the distance to <= AL_MAX_DISTANCE, but since we cull
        // sources that are further away than stream.MaxDistance, we don't do that.
        distance = MathF.Max(component.ReferenceDistance, distance);
        float gain;

        // Technically these are formulas for gain not decibels but EHHHHHHHH.
        switch (component.Attenuation)
        {
            case Attenuation.Default:
                gain = 1f;
                break;
            // You thought I'd implement clamping per source? Hell no that's just for the overall OpenAL setting
            // I didn't even wanna implement this much for linear but figured it'd be cleaner.
            case Attenuation.InverseDistanceClamped:
            case Attenuation.InverseDistance:
                gain = component.ReferenceDistance
                       / (component.ReferenceDistance
                          + component.RolloffFactor * (distance - component.ReferenceDistance));

                break;
            case Attenuation.LinearDistanceClamped:
            case Attenuation.LinearDistance:
                gain = 1f
                        - component.RolloffFactor
                        * (distance - component.ReferenceDistance)
                        / (component.MaxDistance - component.ReferenceDistance);

                break;
            case Attenuation.ExponentDistanceClamped:
            case Attenuation.ExponentDistance:
                gain = MathF.Pow(distance / component.ReferenceDistance, -component.RolloffFactor);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    $"No implemented attenuation for {component.Attenuation}");
        }

        var volume = MathF.Pow(10, component.Volume / 10);
        var actualGain = MathF.Max(0f, volume * gain);
        return actualGain;
    }

    private bool TryGetAudio(string filename, [NotNullWhen(true)] out AudioResource? audio)
    {
        if (_resourceCache.TryGetResource(new ResPath(filename), out audio))
            return true;

        Log.Error($"Server tried to play audio file {filename} which does not exist.");
        return false;
    }

    private bool TryCreateAudioSource(AudioStream stream, [NotNullWhen(true)] out IAudioSource? source)
    {
        if (!Timing.IsFirstTimePredicted)
        {
            source = null;
            Log.Error($"Tried to create audio source outside of prediction!");
            DebugTools.Assert(false);
            return false;
        }

        source = _audio.CreateAudioSource(stream);
        return source != null;
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user)
    {
        if (Timing.IsFirstTimePredicted || sound == null)
            return PlayEntity(sound, Filter.Local(), source, false);

        return null; // uhh Lets hope predicted audio never needs to somehow store the playing audio....
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user)
    {
        if (Timing.IsFirstTimePredicted || sound == null)
            return PlayStatic(sound, Filter.Local(), coordinates, false);

        return null;
    }

    /// <summary>
    ///     Play an audio file globally, without position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayGlobal(string filename, AudioParams? audioParams = null, bool recordReplay = true)
    {
        /* left here just in case uhh yeah idk how replays handle clientside entity spawns.
        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioGlobalMessage
            {
                FileName = filename,
                AudioParams = audioParams ?? AudioParams.Default
            });
        }
        */

        return TryGetAudio(filename, out var audio) ? PlayGlobal(audio, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream globally, without position.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayGlobal(AudioStream stream, AudioParams? audioParams = null)
    {
        if (!TryCreateAudioSource(stream, out var source))
        {
            Log.Error($"Error setting up global audio for {stream.Name}: {0}", Environment.StackTrace);
            return null;
        }

        source.Global = true;

        return CreateAndStartPlayingStream(source, audioParams, stream);
    }



    /// <summary>
    ///     Play an audio file following an entity.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="entity">The entity "emitting" the audio.</param>
    private (EntityUid Entity, AudioComponent Component)? PlayEntity(string filename, EntityUid entity, AudioParams? audioParams = null, bool recordReplay = true)
    {
        /*
        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioEntityMessage
            {
                FileName = filename,
                NetEntity = GetNetEntity(entity),
                AudioParams = audioParams ?? AudioParams.Default
            });
        }
        */

        return TryGetAudio(filename, out var audio) ? PlayEntity(audio, entity, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream following an entity.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="entity">The entity "emitting" the audio.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayEntity(AudioStream stream, EntityUid entity, AudioParams? audioParams = null)
    {
        if (!TryCreateAudioSource(stream, out var source))
        {
            Log.Error($"Error setting up entity audio for {stream.Name} / {ToPrettyString(entity)}: {0}", Environment.StackTrace);
            return null;
        }

        var playing = CreateAndStartPlayingStream(source, audioParams, stream);
        _xformSys.SetCoordinates(playing.Entity, new EntityCoordinates(entity, Vector2.Zero));

        return playing;
    }

    /// <summary>
    ///     Play an audio file at a static position.
    /// </summary>
    /// <param name="filename">The resource path to the OGG Vorbis file to play.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="fallbackCoordinates">The map or grid coordinates at which to play the audio when coordinates are invalid.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayStatic(string filename, EntityCoordinates coordinates, AudioParams? audioParams = null, bool recordReplay = true)
    {
        /*
        if (recordReplay && _replayRecording.IsRecording)
        {
            _replayRecording.RecordReplayMessage(new PlayAudioPositionalMessage
            {
                FileName = filename,
                Coordinates = GetNetCoordinates(coordinates),
                AudioParams = audioParams ?? AudioParams.Default
            });
        }
        */

        return TryGetAudio(filename, out var audio) ? PlayStatic(audio, coordinates, audioParams) : default;
    }

    /// <summary>
    ///     Play an audio stream at a static position.
    /// </summary>
    /// <param name="stream">The audio stream to play.</param>
    /// <param name="coordinates">The coordinates at which to play the audio.</param>
    /// <param name="audioParams"></param>
    private (EntityUid Entity, AudioComponent Component)? PlayStatic(AudioStream stream, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        if (!TryCreateAudioSource(stream, out var source))
        {
            Log.Error($"Error setting up coordinates audio for {stream.Name} / {coordinates}: {0}", Environment.StackTrace);
            return null;
        }

        var playing = CreateAndStartPlayingStream(source, audioParams, stream);
        _xformSys.SetCoordinates(playing.Entity, coordinates);
        return playing;
    }

    /// <inheritdoc />
    protected override (EntityUid Entity, AudioComponent Component)? PlayGlobal(string filename, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        return PlayGlobal(filename, audioParams);
    }

    /// <inheritdoc />
    protected override (EntityUid Entity, AudioComponent Component)? PlayEntity(string filename, Filter playerFilter, EntityUid entity, bool recordReplay, AudioParams? audioParams = null)
    {
        return PlayEntity(filename, entity, audioParams);
    }

    /// <inheritdoc />
    protected override (EntityUid Entity, AudioComponent Component)? PlayStatic(string filename, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null)
    {
        return PlayStatic(filename, coordinates, audioParams);
    }

    /// <inheritdoc />
    protected override (EntityUid Entity, AudioComponent Component)? PlayGlobal(string filename, ICommonSession recipient, AudioParams? audioParams = null)
    {
        return PlayGlobal(filename, audioParams);
    }

    /// <inheritdoc />
    protected override (EntityUid Entity, AudioComponent Component)? PlayGlobal(string filename, EntityUid recipient, AudioParams? audioParams = null)
    {
        return PlayGlobal(filename, audioParams);
    }

    /// <inheritdoc />
    protected override (EntityUid Entity, AudioComponent Component)? PlayEntity(string filename, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(filename, uid, audioParams);
    }

    /// <inheritdoc />
    protected override (EntityUid Entity, AudioComponent Component)? PlayEntity(string filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(filename, uid, audioParams);
    }

    /// <inheritdoc />
    protected override (EntityUid Entity, AudioComponent Component)? PlayStatic(string filename, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return PlayStatic(filename, coordinates, audioParams);
    }

    /// <inheritdoc />
    protected override (EntityUid Entity, AudioComponent Component)? PlayStatic(string filename, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return PlayStatic(filename, coordinates, audioParams);
    }

    private (EntityUid Entity, AudioComponent Component) CreateAndStartPlayingStream(IAudioSource source, AudioParams? audioParams, AudioStream stream)
    {
        var audioP = audioParams ?? AudioParams.Default;
        ApplyAudioParams(audioP, source, stream);
        source.StartPlaying();

        var entity = Spawn(AudioEntity, MapCoordinates.Nullspace);
        var comp = Comp<AudioComponent>(entity);
        comp.Params = audioP;

        return (entity, comp);
    }

    /// <summary>
    /// Applies the audioparams to the underlying audio source.
    /// </summary>
    private void ApplyAudioParams(AudioParams audioParams, IAudioSource source, AudioStream audio)
    {
        if (audioParams.Variation.HasValue)
            source.Pitch = audioParams.PitchScale
                           * (float) RandMan.NextGaussian(1, audioParams.Variation.Value);
        else
            source.Pitch = audioParams.PitchScale;

        source.Volume = audioParams.Volume;
        source.RolloffFactor = audioParams.RolloffFactor;
        source.MaxDistance = audioParams.MaxDistance;
        source.ReferenceDistance = audioParams.ReferenceDistance;
        source.Looping = audioParams.Loop;

        // TODO clamp the offset inside of SetPlaybackPosition() itself.
        var offset = audioParams.PlayOffsetSeconds;
        offset = Math.Clamp(offset, 0f, (float) audio.Length.TotalSeconds);
        source.PlaybackPosition = offset;
    }
}
