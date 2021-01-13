using System;
using System.Collections.Generic;
using Robust.Client.GameObjects.Components.Animations;

namespace Robust.Client.Animations
{
    /// <summary>
    ///     A animation represents a way to animate something, using keyframes and such.
    /// </summary>
    /// <remarks>
    ///     An animation is a collection of <see cref="AnimationTracks"/>, which are all executed in sync.
    /// </remarks>
    /// <seealso cref="AnimationPlayerComponent"/>
    public sealed class Animation
    {
        public readonly List<AnimationTrack> AnimationTracks = new();

        public TimeSpan Length { get; set; }
    }
}
