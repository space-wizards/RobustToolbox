using System.Collections.Generic;
using Robust.Client.Animations;
using Robust.Shared.GameObjects;
using static Robust.Client.Animations.AnimationPlaybackShared;

namespace Robust.Client.GameObjects
{
    /// <summary>
    ///     Plays back <see cref="Animation"/>s on entities.
    /// </summary>
    [RegisterComponent]
    public sealed partial class AnimationPlayerComponent : Component
    {
        // TODO: Give this component a friend someday. Way too much content shit to change atm ._.

        public int PlayingAnimationCount => PlayingAnimations.Count;

        internal readonly Dictionary<string, AnimationPlayback> PlayingAnimations
            = new();

        internal bool HasPlayingAnimation = false;
    }
}
