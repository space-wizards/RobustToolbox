using System.Collections.Generic;
using Robust.Client.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    public sealed class AnimationPlayerSystem : EntitySystem
    {
        private readonly List<AnimationPlayerComponent> _activeAnimations = new();

        [Dependency] private readonly IComponentFactory _compFact = default!;

        public override void FrameUpdate(float frameTime)
        {
            for (var i = _activeAnimations.Count - 1; i >= 0; i--)
            {
                var anim = _activeAnimations[i];
                if (!Update(anim, frameTime)) continue;
                _activeAnimations.RemoveSwap(i);
                anim.HasPlayingAnimation = false;
            }
        }

        internal void AddComponent(AnimationPlayerComponent component)
        {
            if (component.HasPlayingAnimation) return;
            _activeAnimations.Add(component);
            component.HasPlayingAnimation = true;
        }

        private bool Update(AnimationPlayerComponent component, float frameTime)
        {
            if (component.PlayingAnimationCount == 0 ||
                component.Deleted)
                return true;

            var remie = new RemQueue<string>();
            foreach (var (key, playback) in component.PlayingAnimations)
            {
                var keep = AnimationPlaybackShared.UpdatePlayback(component.Owner, playback, frameTime);
                if (!keep)
                {
                    remie.Add(key);
                }
            }

            foreach (var key in remie)
            {
                component.PlayingAnimations.Remove(key);
                EntityManager.EventBus.RaiseLocalEvent(component.Owner, new AnimationCompletedEvent {Uid = component.Owner, Key = key}, true);
                component.AnimationComplete(key);
            }

            return false;
        }

        /// <summary>
        ///     Start playing an animation.
        /// </summary>
        public void Play(EntityUid uid, Animation animation, string key)
        {
            var component = EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);
            Play(component, animation, key);
        }

        public void Play(EntityUid uid, AnimationPlayerComponent? component, Animation animation, string key)
        {
            component ??= EntityManager.EnsureComponent<AnimationPlayerComponent>(uid);
            Play(component, animation, key);
        }

        /// <summary>
        ///     Start playing an animation.
        /// </summary>
        public void Play(AnimationPlayerComponent component, Animation animation, string key)
        {
            AddComponent(component);
            var playback = new AnimationPlaybackShared.AnimationPlayback(animation);

#if DEBUG
            // Component networking checks
            foreach (var track in animation.AnimationTracks)
            {
                if (track is not AnimationTrackComponentProperty compTrack)
                    continue;

                if (compTrack.ComponentType == null)
                {
                    Logger.Error($"Attempted to play a component animation without any component specified.");
                    return;
                }

                if (!EntityManager.TryGetComponent(component.Owner, compTrack.ComponentType, out var animatedComp))
                {
                    Logger.Error(
                        $"Attempted to play a component animation, but the entity {ToPrettyString(component.Owner)} does not have the component to be animated: {compTrack.ComponentType}.");
                    return;
                }

                if (component.Owner.IsClientSide() || !animatedComp.NetSyncEnabled)
                    continue;

                var reg = _compFact.GetRegistration(animatedComp);

                // In principle there is nothing wrong with this, as long as the property of the component being
                // animated is not part of the networked state and setting it does not dirty the component. Hence only a
                // warning in debug mode.
                if (reg.NetID != null)
                    Logger.Warning($"Playing a component animation on a networked component {reg.Name} belonging to {ToPrettyString(component.Owner)}");
            }
#endif

            component.PlayingAnimations.Add(key, playback);
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

        public void Stop(AnimationPlayerComponent component, string key)
        {
            component.PlayingAnimations.Remove(key);
        }

        public void Stop(EntityUid uid, string key)
        {
            if (!TryComp<AnimationPlayerComponent>(uid, out var player)) return;
            player.PlayingAnimations.Remove(key);
        }

        public void Stop(EntityUid uid, AnimationPlayerComponent? component, string key)
        {
            if (!Resolve(uid, ref component, false)) return;
            component.PlayingAnimations.Remove(key);
        }
    }

    public sealed class AnimationCompletedEvent : EntityEventArgs
    {
        public EntityUid Uid { get; init; }
        public string Key { get; init; } = string.Empty;
    }
}
