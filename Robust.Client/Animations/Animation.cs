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
        public List<AnimationTrack> AnimationTracks { get; private set; } = new();

        public TimeSpan Length { get; set; }
    }
}
