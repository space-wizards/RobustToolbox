using System;
using System.Collections.Generic;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Shared.Interfaces.Serialization;

namespace Robust.Client.Animations
{
    /// <summary>
    ///     A animation represents a way to animate something, using keyframes and such.
    /// </summary>
    /// <remarks>
    ///     An animation is a collection of <see cref="AnimationTracks"/>, which are all executed in sync.
    /// </remarks>
    /// <seealso cref="AnimationPlayerComponent"/>
    public sealed class Animation : IDeepClone
    {
        public List<AnimationTrack> AnimationTracks { get; private set; } = new();

        public TimeSpan Length { get; set; }
        public IDeepClone DeepClone()
        {
            return new Animation
            {
                AnimationTracks = IDeepClone.CloneValue(AnimationTracks)!,
                Length = IDeepClone.CloneValue(Length)
            };
        }
    }
}
