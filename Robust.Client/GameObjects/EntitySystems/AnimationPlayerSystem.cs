using System.Collections.Generic;
using Robust.Client.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    public sealed class AnimationPlayerSystem : EntitySystem
    {
        private readonly List<AnimationPlayerComponent> _activeAnimations = new();

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
            if (component.PlayingAnimationCount == 0)
                return true;

            List<string>? toRemove = null;
            foreach (var (key, playback) in component.PlayingAnimations)
            {
                var keep = AnimationPlaybackShared.UpdatePlayback(component.Owner, playback, frameTime);
                if (!keep)
                {
                    toRemove ??= new List<string>();
                    toRemove.Add(key);
                }
            }

            if (toRemove != null)
            {
                foreach (var key in toRemove)
                {
                    component.PlayingAnimations.Remove(key);
                    EntityManager.EventBus.RaiseLocalEvent(component.Owner.Uid, new AnimationCompletedEvent {Uid = component.Owner.Uid, Key = key});
                    component.AnimationComplete(key);
                }
            }

            return false;
        }

        /// <summary>
        ///     Start playing an animation.
        /// </summary>
        public void Play(EntityUid uid, Animation animation, string key)
        {
            var component = ComponentManager.EnsureComponent<AnimationPlayerComponent>(EntityManager.GetEntity(uid));
            Play(component, animation, key);
        }

        /// <summary>
        ///     Start playing an animation.
        /// </summary>
        public void Play(AnimationPlayerComponent component, Animation animation, string key)
        {
            AddComponent(component);
            var playback = new AnimationPlaybackShared.AnimationPlayback(animation);

            component.PlayingAnimations.Add(key, playback);
        }

        public bool HasRunningAnimation(EntityUid uid, string key)
        {
            return ComponentManager.TryGetComponent(uid, out AnimationPlayerComponent? component) &&
                   component.PlayingAnimations.ContainsKey(key);
        }

        public bool HasRunningAnimation(AnimationPlayerComponent component, string key)
        {
            return component.PlayingAnimations.ContainsKey(key);
        }

        public void Stop(AnimationPlayerComponent component, string key)
        {
            component.PlayingAnimations.Remove(key);
        }
    }

    public sealed class AnimationCompletedEvent : EntityEventArgs
    {
        public EntityUid Uid { get; init; }
        public string Key { get; init; } = string.Empty;
    }
}
