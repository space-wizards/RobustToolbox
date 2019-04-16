using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects.Components.Animations
{
    /// <summary>
    ///     Plays back <see cref="Animation"/>s on entities.
    /// </summary>
    public sealed class AnimationPlayerComponent : Component
    {
        public override string Name => "AnimationPlayer";

        private readonly Dictionary<string, AnimationPlayback> _playingAnimations
            = new Dictionary<string, AnimationPlayback>();

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

            for (var i = 0; i < animation.AnimationTracks.Count; i++)
            {
                var (keyFrame, left) = animation.AnimationTracks[i].InitPlayback();
                playback.TrackPlaybacks[i] = new AnimationTrackPlayback(keyFrame, left);
            }

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

            // TODO: Get rid of this ToArray() allocation.
            foreach (var (key, playback) in _playingAnimations.ToArray())
            {
                var keep = _updatePlayback(playback, frameTime);
                if (!keep)
                {
                    _playingAnimations.Remove(key);
                }
            }
        }

        private bool _updatePlayback(AnimationPlayback playback, float frameTime)
        {
            var animation = playback.Animation;
            for (var i = 0; i < animation.AnimationTracks.Count; i++)
            {
                var track = animation.AnimationTracks[i];
                ref var trackPlayback = ref playback.TrackPlaybacks[i];

                var (keyFrame, playing) = track.AdvancePlayback(Owner, trackPlayback.KeyFrameIndex,
                    trackPlayback.KeyFrameTimePlaying, frameTime);

                trackPlayback.KeyFrameIndex = keyFrame;
                trackPlayback.KeyFrameTimePlaying = playing;
            }

            playback.PlayTime += frameTime;
            return TimeSpan.FromSeconds(playback.PlayTime) <= animation.Length;
        }

        private sealed class AnimationPlayback
        {
            public readonly Animation Animation;

            // Indices here correspond to the track indices in Animation.
            public readonly AnimationTrackPlayback[] TrackPlaybacks;

            public float PlayTime;

            public AnimationPlayback(Animation animation)
            {
                Animation = animation;
                TrackPlaybacks = new AnimationTrackPlayback[animation.AnimationTracks.Count];
            }
        }

        private struct AnimationTrackPlayback
        {
            public int KeyFrameIndex;
            public float KeyFrameTimePlaying;

            public AnimationTrackPlayback(int keyFrameIndex, float keyFrameTimePlaying)
            {
                KeyFrameIndex = keyFrameIndex;
                KeyFrameTimePlaying = keyFrameTimePlaying;
            }
        }
    }
}
