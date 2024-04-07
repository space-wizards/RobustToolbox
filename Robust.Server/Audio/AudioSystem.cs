using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Audio.AudioLoading;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Audio.Systems;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Server.Audio;

public sealed partial class AudioSystem : SharedAudioSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;

    private readonly Dictionary<string, TimeSpan> _cachedAudioLengths = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AudioComponent, ComponentStartup>(OnAudioStartup);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownEffect();
    }

    private void OnAudioStartup(EntityUid uid, AudioComponent component, ComponentStartup args)
    {
        component.Source = new DummyAudioSource();
    }

    private void AddAudioFilter(EntityUid uid, AudioComponent component, Filter filter)
    {
        var count = filter.Count;

        if (count == 0)
            return;

        _pvs.AddSessionOverrides(uid, filter);

        var ents = new HashSet<EntityUid>(count);

        foreach (var session in filter.Recipients)
        {
            var ent = session.AttachedEntity;

            if (ent == null)
                continue;

            ents.Add(ent.Value);
        }

        DebugTools.Assert(component.IncludedEntities == null);
        component.IncludedEntities = ents;
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(string? filename, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        var entity = Spawn("Audio", MapCoordinates.Nullspace);
        var audio = SetupAudio(entity, filename, audioParams);
        AddAudioFilter(entity, audio, playerFilter);
        audio.Global = true;
        return (entity, audio);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(string? filename, Filter playerFilter, EntityUid uid, bool recordReplay, AudioParams? audioParams = null)
    {
        if (string.IsNullOrEmpty(filename))
            return null;

        if (TerminatingOrDeleted(uid))
        {
            Log.Error($"Tried to play audio on a terminating / deleted entity {ToPrettyString(uid)}. Trace: {Environment.StackTrace}");
            return null;
        }

        var entity = Spawn("Audio", new EntityCoordinates(uid, Vector2.Zero));
        var audio = SetupAudio(entity, filename, audioParams);
        AddAudioFilter(entity, audio, playerFilter);

        return (entity, audio);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayPvs(string? filename, EntityUid uid, AudioParams? audioParams = null)
    {
        if (string.IsNullOrEmpty(filename))
            return null;

        if (TerminatingOrDeleted(uid))
        {
            Log.Error($"Tried to play audio on a terminating / deleted entity {ToPrettyString(uid)}. Trace: {Environment.StackTrace}");
            return null;
        }

        var entity = Spawn("Audio", new EntityCoordinates(uid, Vector2.Zero));
        var audio = SetupAudio(entity, filename, audioParams);

        return (entity, audio);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(string? filename, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null)
    {
        if (string.IsNullOrEmpty(filename))
            return null;

        if (TerminatingOrDeleted(coordinates.EntityId))
        {
            Log.Error($"Tried to play coordinates audio on a terminating / deleted entity {ToPrettyString(coordinates.EntityId)}.  Trace: {Environment.StackTrace}");
            return null;
        }

        if (!coordinates.IsValid(EntityManager))
            return null;

        var entity = Spawn("Audio", coordinates);
        var audio = SetupAudio(entity, filename, audioParams);
        AddAudioFilter(entity, audio, playerFilter);

        return (entity, audio);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayPvs(string? filename, EntityCoordinates coordinates,
        AudioParams? audioParams = null)
    {
        if (string.IsNullOrEmpty(filename))
            return null;

        if (TerminatingOrDeleted(coordinates.EntityId))
        {
            Log.Error($"Tried to play coordinates audio on a terminating / deleted entity {ToPrettyString(coordinates.EntityId)}.  Trace: {Environment.StackTrace}");
            return null;
        }

        if (!coordinates.IsValid(EntityManager))
            return null;

        var entity = Spawn("Audio", coordinates);
        var audio = SetupAudio(entity, filename, audioParams);

        return (entity, audio);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user, AudioParams? audioParams = null)
    {
        if (sound == null)
            return null;

        var audio = PlayPvs(GetSound(sound), source, audioParams ?? sound.Params);

        if (audio == null)
            return null;

        audio.Value.Component.ExcludedEntity = user;
        return audio;
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityCoordinates coordinates, EntityUid? user, AudioParams? audioParams = null)
    {
        if (sound == null)
            return null;

        var audio = PlayPvs(GetSound(sound), coordinates, audioParams ?? sound.Params);

        if (audio == null)
            return null;

        audio.Value.Component.ExcludedEntity = user;
        return audio;
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(string? filename, ICommonSession recipient, AudioParams? audioParams = null)
    {
        return PlayGlobal(filename, Filter.SinglePlayer(recipient), false, audioParams);
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(string? filename, EntityUid recipient, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayGlobal(filename, actor.PlayerSession, audioParams);

        return null;
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(string? filename, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(filename, Filter.SinglePlayer(recipient), uid, false, audioParams);
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(string? filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayEntity(filename, actor.PlayerSession, uid, audioParams);

        return null;
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(string? filename, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return PlayStatic(filename, Filter.SinglePlayer(recipient), coordinates, false, audioParams);
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(string? filename, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayStatic(filename, actor.PlayerSession, coordinates, audioParams);

        return null;
    }

    protected override TimeSpan GetAudioLengthImpl(string filename)
    {
        // Check shipped metadata from packaging.
        if (ProtoMan.TryIndex(filename, out AudioMetadataPrototype? metadata))
            return metadata.Length;

        // Try loading audio files directly.
        // This is necessary in development and environments,
        // and when working with audio files uploaded dynamically at runtime.
        if (_cachedAudioLengths.TryGetValue(filename, out var length))
            return length;

        if (!_resourceManager.TryContentFileRead(filename, out var stream))
            throw new FileNotFoundException($"Unable to find metadata for audio file {filename}");

        using (stream)
        {
            var loadedMetadata = AudioLoader.LoadAudioMetadata(stream, filename);
            _cachedAudioLengths.Add(filename, loadedMetadata.Length);
            return loadedMetadata.Length;
        }
    }

    public override void LoadStream<T>(Entity<AudioComponent> entity, T stream)
    {
        // TODO: Yeah remove this...
    }
}
