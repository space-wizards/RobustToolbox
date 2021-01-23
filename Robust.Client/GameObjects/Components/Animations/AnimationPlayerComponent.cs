using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Animations;
using Robust.Shared.GameObjects;
using static Robust.Client.Animations.AnimationPlaybackShared;

namespace Robust.Client.GameObjects.Components.Animations
{
    /// <summary>
    ///     Plays back <see cref="Animation"/>s on entities.
    /// </summary>
    public sealed class AnimationPlayerComponent : Component
    {
        public override string Name => "AnimationPlayer";

        private readonly Dictionary<string, AnimationPlayback> _playingAnimations
            = new();

        /// <summary>
        ///     Start playing an animation.
        /// </summary>
        /// <param name="animation">The animation to play.</param>
        /// <param name="key">
        ///     The key for this animation play. This key can be used to stop playback short later.
        /// </param>
        public void Play(Animation animation, string key)
        {
            var playback = new AnimationPlayback(animation);

            _playingAnimations.Add(key, playback);
        }

        public bool HasRunningAnimation(string key)
        {
            return _playingAnimations.ContainsKey(key);
        }

        public void Stop(string key)
        {
            _playingAnimations.Remove(key);
        }

        internal void Update(float frameTime)
        {
            if (_playingAnimations.Count == 0)
            {
                return;
            }

            List<string>? toRemove = null;
            // TODO: Get rid of this ToArray() allocation.
            foreach (var (key, playback) in _playingAnimations.ToArray())
            {
                var keep = UpdatePlayback(Owner, playback, frameTime);
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
                    _playingAnimations.Remove(key);
                    AnimationCompleted?.Invoke(key);
                }
            }
        }

        public event Action<string>? AnimationCompleted;
    }
}
