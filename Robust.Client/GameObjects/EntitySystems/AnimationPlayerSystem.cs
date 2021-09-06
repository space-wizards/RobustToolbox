using System.Collections.Generic;
using System.Linq;
using Robust.Client.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    internal sealed class AnimationPlayerSystem : EntitySystem
    {
        private readonly List<AnimationPlayerComponent> _activeAnimations = new();

        public override void FrameUpdate(float frameTime)
        {
            for (var i = _activeAnimations.Count - 1; i >= 0; i--)
            {
                var anim = _activeAnimations[i];
                if (!Update(anim, frameTime)) continue;
                _activeAnimations.RemoveAt(i);
            }
        }

        internal void AddComponent(AnimationPlayerComponent component)
        {
            if (_activeAnimations.Contains(component)) return;
            _activeAnimations.Add(component);
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
    }

    public sealed class AnimationCompletedEvent : EntityEventArgs
    {
        public EntityUid Uid { get; init; }
        public string Key { get; init; } = string.Empty;
    }
}
