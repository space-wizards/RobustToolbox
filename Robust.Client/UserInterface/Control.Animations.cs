using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Animations;
using Robust.Shared.Timing;
using static Robust.Client.Animations.AnimationPlaybackShared;

namespace Robust.Client.UserInterface
{
    public partial class Control
    {
        private Dictionary<string, AnimationPlayback>? _playingAnimations;

        public Action<string>? AnimationCompleted;

        /// <summary>
        ///     Start playing an animation.
        /// </summary>
        /// <param name="animation">The animation to play.</param>
        /// <param name="key">
        ///     The key for this animation play. This key can be used to stop playback short later.
        /// </param>
        public void PlayAnimation(Animation animation, string key)
        {
            var playback = new AnimationPlayback(animation);

            _playingAnimations ??= new Dictionary<string, AnimationPlayback>();
            _playingAnimations.Add(key, playback);
        }

        public bool HasRunningAnimation(string key)
        {
            return _playingAnimations?.ContainsKey(key) ?? false;
        }

        public void StopAnimation(string key)
        {
            _playingAnimations?.Remove(key);
        }

        private void ProcessAnimations(FrameEventArgs args)
        {
            if (_playingAnimations == null || _playingAnimations.Count == 0)
            {
                return;
            }

            // TODO: Get rid of this ToArray() allocation.
            foreach (var (key, playback) in _playingAnimations.ToArray())
            {
                var keep = UpdatePlayback(this, playback, args.DeltaSeconds);
                if (!keep)
                {
                    _playingAnimations.Remove(key);
                    AnimationCompleted?.Invoke(key);
                }
            }
        }
    }
}
