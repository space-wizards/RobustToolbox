using System;
using System.Collections.Generic;
using Robust.Client.Animations;
using Robust.Shared.GameObjects;
using static Robust.Client.Animations.AnimationPlaybackShared;

namespace Robust.Client.GameObjects
{
    /// <summary>
    ///     Plays back <see cref="Animation"/>s on entities.
    /// </summary>
    public sealed class AnimationPlayerComponent : Component
    {
        // TODO: Give this component a friend someday. Way too much content shit to change atm ._.

        public override string Name => "AnimationPlayer";

        public int PlayingAnimationCount => PlayingAnimations.Count;

        internal readonly Dictionary<string, AnimationPlayback> PlayingAnimations
            = new();

        internal bool HasPlayingAnimation = false;

        /// <summary>
        ///     Start playing an animation.
        /// </summary>
        /// <param name="animation">The animation to play.</param>
        /// <param name="key">
        ///     The key for this animation play. This key can be used to stop playback short later.
        /// </param>
        public void Play(Animation animation, string key)
        {
            EntitySystem.Get<AnimationPlayerSystem>().AddComponent(this);
            var playback = new AnimationPlayback(animation);

            PlayingAnimations.Add(key, playback);
        }

        public bool HasRunningAnimation(string key)
        {
            return PlayingAnimations.ContainsKey(key);
        }

        public void Stop(string key)
        {
            PlayingAnimations.Remove(key);
        }

        /// <summary>
        /// Temporary method until the event is replaced with eventbus.
        /// </summary>
        internal void AnimationComplete(string key)
        {
            AnimationCompleted?.Invoke(key);
        }

        [Obsolete("Use AnimationCompletedEvent instead")]
        public event Action<string>? AnimationCompleted;
    }
}
