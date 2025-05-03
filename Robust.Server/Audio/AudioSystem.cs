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

    public override void SetGridAudio(Entity<AudioComponent>? entity)
    {
        if (entity == null)
            return;

        base.SetGridAudio(entity);

        // Need to global override so everyone can hear it.
        _pvs.AddGlobalOverride(entity.Value.Owner);
    }

    public override void SetMapAudio(Entity<AudioComponent>? audio)
    {
        if (audio == null)
            return;

        base.SetMapAudio(audio);

        // Also need a global override because clients not near 0,0 won't get the audio.
        _pvs.AddGlobalOverride(audio.Value);
    }

    private void AddAudioFilter(EntityUid uid, AudioComponent component, Filter filter)
    {
        DebugTools.Assert(component.IncludedEntities == null);
        component.IncludedEntities = new();

        if (filter.Count == 0)
            return;

        _pvs.AddSessionOverrides(uid, filter);
        foreach (var session in filter.Recipients)
        {
            if (session.AttachedEntity is {} ent)
                component.IncludedEntities.Add(ent);
        }
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(ResolvedSoundSpecifier? specifier, Filter playerFilter, bool recordReplay, AudioParams? audioParams = null)
    {
        if (specifier is null)
            return null;

        var entity = SetupAudio(specifier, audioParams);
        AddAudioFilter(entity, entity.Comp, playerFilter);
        entity.Comp.Global = true;
        return (entity, entity.Comp);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(ResolvedSoundSpecifier? specifier, Filter playerFilter, EntityUid uid, bool recordReplay, AudioParams? audioParams = null)
    {
        if (specifier is null)
            return null;

        if (TerminatingOrDeleted(uid))
            return null;

        var entity = SetupAudio(specifier, audioParams);
        // Move it after setting it up
        XformSystem.SetCoordinates(entity, new EntityCoordinates(uid, Vector2.Zero));

        // TODO AUDIO
        // Add methods that allow for custom audio range.
        // Some methods try to reduce the audio range, resulting in a custom filter which then unnecessarily has to
        // use PVS overrides. PlayEntity with a reduced range shouldn't need PVS overrides at all.
        AddAudioFilter(entity, entity.Comp, playerFilter);

        return (entity, entity.Comp);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayPvs(ResolvedSoundSpecifier? specifier, EntityUid uid, AudioParams? audioParams = null)
    {
        if (specifier is null)
            return null;

        if (TerminatingOrDeleted(uid))
            return null;

        var entity = SetupAudio(specifier, audioParams);
        XformSystem.SetCoordinates(entity, new EntityCoordinates(uid, Vector2.Zero));

        return (entity, entity.Comp);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(ResolvedSoundSpecifier? specifier, Filter playerFilter, EntityCoordinates coordinates, bool recordReplay, AudioParams? audioParams = null)
    {
        if (specifier is null)
            return null;

        if (TerminatingOrDeleted(coordinates.EntityId))
        {
            Log.Error($"Tried to play coordinates audio on a terminating / deleted entity {ToPrettyString(coordinates.EntityId)}.  Trace: {Environment.StackTrace}");
            return null;
        }

        if (!coordinates.IsValid(EntityManager))
            return null;

        var entity = SetupAudio(specifier, audioParams);
        XformSystem.SetCoordinates(entity, coordinates);
        AddAudioFilter(entity, entity.Comp, playerFilter);

        return (entity, entity.Comp);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayPvs(ResolvedSoundSpecifier? specifier, EntityCoordinates coordinates,
        AudioParams? audioParams = null)
    {
        if (specifier is null)
            return null;

        if (TerminatingOrDeleted(coordinates.EntityId))
        {
            Log.Error($"Tried to play coordinates audio on a terminating / deleted entity {ToPrettyString(coordinates.EntityId)}.  Trace: {Environment.StackTrace}");
            return null;
        }

        if (!coordinates.IsValid(EntityManager))
            return null;

        // TODO: Transform TryFindGridAt mess + optimisation required.
        var entity = SetupAudio(specifier, audioParams);
        XformSystem.SetCoordinates(entity, coordinates);

        return (entity, entity.Comp);
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayLocal(
        SoundSpecifier? sound,
        EntityUid source,
        EntityUid? soundInitiator,
        AudioParams? audioParams = null
    )
    {
        return null;
    }

    /// <inheritdoc />
    public override (EntityUid Entity, AudioComponent Component)? PlayPredicted(SoundSpecifier? sound, EntityUid source, EntityUid? user, AudioParams? audioParams = null)
    {
        if (sound == null)
            return null;

        var audio = PlayPvs(ResolveSound(sound), source, audioParams ?? sound.Params);

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

        var audio = PlayPvs(ResolveSound(sound), coordinates, audioParams ?? sound.Params);

        if (audio == null)
            return null;

        audio.Value.Component.ExcludedEntity = user;
        return audio;
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(ResolvedSoundSpecifier? filename, ICommonSession recipient, AudioParams? audioParams = null)
    {
        return PlayGlobal(filename, Filter.SinglePlayer(recipient), false, audioParams);
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayGlobal(ResolvedSoundSpecifier? filename, EntityUid recipient, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayGlobal(filename, actor.PlayerSession, audioParams);

        return null;
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(ResolvedSoundSpecifier? filename, ICommonSession recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        return PlayEntity(filename, Filter.SinglePlayer(recipient), uid, false, audioParams);
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayEntity(ResolvedSoundSpecifier? filename, EntityUid recipient, EntityUid uid, AudioParams? audioParams = null)
    {
        if (TryComp(recipient, out ActorComponent? actor))
            return PlayEntity(filename, actor.PlayerSession, uid, audioParams);

        return null;
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(ResolvedSoundSpecifier? filename, ICommonSession recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
    {
        return PlayStatic(filename, Filter.SinglePlayer(recipient), coordinates, false, audioParams);
    }

    public override (EntityUid Entity, AudioComponent Component)? PlayStatic(ResolvedSoundSpecifier? filename, EntityUid recipient, EntityCoordinates coordinates, AudioParams? audioParams = null)
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
