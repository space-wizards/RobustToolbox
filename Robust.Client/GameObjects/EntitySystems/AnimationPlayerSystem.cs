using System;
using System.Collections.Generic;
using Robust.Client.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    public sealed class AnimationPlayerSystem : EntitySystem
    {
        private readonly List<Entity<AnimationPlayerComponent>> _activeAnimations = new();

        private EntityQuery<AnimationPlayerComponent> _playerQuery;
        private EntityQuery<MetaDataComponent> _metaQuery;

        [Dependency] private readonly IComponentFactory _compFact = default!;

        public override void Initialize()
        {
            base.Initialize();
            _playerQuery = GetEntityQuery<AnimationPlayerComponent>();
            _metaQuery = GetEntityQuery<MetaDataComponent>();
        }

        public override void FrameUpdate(float frameTime)
        {
            // TODO: Active or something idk.
            for (var i = 0; i < _activeAnimations.Count; i++)
            {
                var anim = _activeAnimations[i];
                var uid = anim.Owner;

                if (!_metaQuery.TryGetComponent(uid, out var metadata) ||
                    metadata.EntityPaused)
                {
                    continue;
                }

                if (!Update(uid, anim.Comp, frameTime))
                {
                    continue;
                }

                _activeAnimations.RemoveSwap(i);
                i--;
                anim.Comp.HasPlayingAnimation = false;
            }
        }

        internal void AddComponent(Entity<AnimationPlayerComponent> ent)
        {
            if (ent.Comp.HasPlayingAnimation) return;
            _activeAnimations.Add(ent);
            ent.Comp.HasPlayingAnimation = true;
        }

        private bool Update(EntityUid uid, AnimationPlayerComponent component, float frameTime)
        {
            if (component.PlayingAnimationCount == 0 ||
                component.Deleted)
            {
                return true;
            }

            var remie = new RemQueue<string>();
            foreach (var (key, playback) in component.PlayingAnimations)
            {
                var keep = AnimationPlaybackShared.UpdatePlayback(uid, playback, frameTime);
                if (!keep)
                {
                    remie.Add(key);
                }
            }

            foreach (var key in remie)
            {
                component.PlayingAnimations.Remove(key);
                EntityManager.EventBus.RaiseLocalEvent(uid, new AnimationCompletedEvent {Uid = uid, Key = key}, true);
            }

            return false;
        }

        /// <summary>
        ///     Start playing an animation.
        /// </summary>
        public void Play(EntityUid uid, Animation animation, string key)
        {
            var component = EnsureComp<AnimationPlayerComponent>(uid);
            Play(new Entity<AnimationPlayerComponent>(uid, component), animation, key);
        }

        [Obsolete("Use Play(EntityUid<AnimationPlayerComponent> ent, Animation animation, string key) instead")]
        public void Play(EntityUid uid, AnimationPlayerComponent? component, Animation animation, string key)
        {
            component ??= EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);
            Play(new Entity<AnimationPlayerComponent>(uid, component), animation, key);
        }

        /// <summary>
        ///     Start playing an animation.
        /// </summary>
        [Obsolete("Use Play(EntityUid<AnimationPlayerComponent> ent, Animation animation, string key) instead")]
        public void Play(AnimationPlayerComponent component, Animation animation, string key)
        {
            Play(new Entity<AnimationPlayerComponent>(component.Owner, component), animation, key);
        }

        public void Play(Entity<AnimationPlayerComponent> ent, Animation animation, string key)
        {
            AddComponent(ent);
            var playback = new AnimationPlaybackShared.AnimationPlayback(animation);

#if DEBUG
            // Component networking checks
            foreach (var track in animation.AnimationTracks)
            {
                if (track is not AnimationTrackComponentProperty compTrack)
                    continue;

                if (compTrack.ComponentType == null)
                {
                    Log.Error("Attempted to play a component animation without any component specified.");
                    return;
                }

                if (!EntityManager.TryGetComponent(ent, compTrack.ComponentType, out var animatedComp))
                {
                    Log.Error(
                        $"Attempted to play a component animation, but the entity {ToPrettyString(ent)} does not have the component to be animated: {compTrack.ComponentType}.");
                    return;
                }

                if (IsClientSide(ent) || !animatedComp.NetSyncEnabled)
                    continue;

                var reg = _compFact.GetRegistration(animatedComp);

                // In principle there is nothing wrong with this, as long as the property of the component being
                // animated is not part of the networked state and setting it does not dirty the component. Hence only a
                // warning in debug mode.
                if (reg.NetID != null && compTrack.Property != null)
                {
                    if (animatedComp.GetType().GetProperty(compTrack.Property) is { } property &&
                        property.HasCustomAttribute<AutoNetworkedFieldAttribute>())
                    {
                        Log.Warning($"Playing a component animation on a networked component {reg.Name} belonging to {ToPrettyString(ent)}");
                    }
                }
            }
#endif

            ent.Comp.PlayingAnimations.Add(key, playback);
        }

        public bool HasRunningAnimation(EntityUid uid, string key)
        {
            return EntityManager.TryGetComponent(uid, out AnimationPlayerComponent? component) &&
                   component.PlayingAnimations.ContainsKey(key);
        }

        public bool HasRunningAnimation(EntityUid uid, AnimationPlayerComponent? component, string key)
        {
            if (component == null)
                TryComp(uid, out component);

            return component != null && component.PlayingAnimations.ContainsKey(key);
        }

        public bool HasRunningAnimation(AnimationPlayerComponent component, string key)
        {
            return component.PlayingAnimations.ContainsKey(key);
        }

        [Obsolete]
        public void Stop(AnimationPlayerComponent component, string key)
        {
            Stop((component.Owner, component), key);
        }

        public void Stop(Entity<AnimationPlayerComponent?> entity, string key)
        {
            if (!_playerQuery.Resolve(entity.Owner, ref entity.Comp, false) ||
                !entity.Comp.PlayingAnimations.Remove(key))
            {
                return;
            }

            EntityManager.EventBus.RaiseLocalEvent(entity.Owner, new AnimationCompletedEvent {Uid = entity.Owner, Key = key}, true);
        }

        public void Stop(EntityUid uid, AnimationPlayerComponent? component, string key)
        {
            Stop((uid, component), key);
        }
    }

    /// <summary>
    /// Raised whenever an animation stops, either due to running its course or being stopped manually.
    /// </summary>
    public sealed class AnimationCompletedEvent : EntityEventArgs
    {
        public EntityUid Uid { get; init; }
        public string Key { get; init; } = string.Empty;
    }
}
