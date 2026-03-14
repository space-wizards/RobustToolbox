using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Random;

[PublicAPI]
public static partial class RandomExtensions
{
    extension<T>(T random)
        where T : IRobustRandom
    {
        /// <summary>
        ///     Have a certain chance to return a boolean.
        /// </summary>
        /// <param name="chance">The chance to pass, from 0 to 1.</param>
        [MustUseReturnValue]
        public bool Prob(float chance)
        {
            DebugTools.Assert(chance is <= 1 and >= 0, $"Chance must be in the range 0-1. It was {chance}.");

            return random.NextFloat() < chance;
        }

        /// <summary>
        ///     Creates a string populated with random symbols from <see cref="sourceRunes"/>.
        /// </summary>
        /// <param name="destination">Buffer to write into</param>
        /// <param name="sourceRunes">The source to use for random symbols.</param>
        /// <param name="length">The number of symbols to put into the destination buffer.</param>
        /// <returns>The number of chars written, which is distinct from the input length.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the generated string may not fit into the destination.
        /// </exception>
        /// <remarks>
        ///     This function is <i>somewhat</i> Unicode aware. It correctly handles surrogate pairs, but does not
        ///     support Unicode characters composed of more than one Unicode codepoint.
        /// </remarks>
        /// <seealso cref="System.Random.GetString"/>
        public int FillStringFromRunes(Span<char> destination, ReadOnlySpan<Rune> sourceRunes, int length)
        {
            // There was something fancier here, but then I realized that for internationalization reasons we probably
            // should require people just always create the larger minimum buffer size.
            if (destination.Length < length * 2)
                throw new ArgumentException("Destination buffer is not large enough for all possible strings.");

            var index = 0;

            Span<char> runeBuffer = stackalloc char[2];

            for (var i = 0; i < length; i++)
            {
                var symbol = random.Pick(sourceRunes);

                // All runes are one or two chars, so we only have two possible lengths to check for.
                var len = symbol.EncodeToUtf16(runeBuffer);

                destination[index++] = runeBuffer[0];

                if (len == 2)
                    destination[index++] = runeBuffer[1];
            }

            return index;
        }

        /// <summary>
        ///     Creates a string populated with random symbols from <see cref="sourceRunes"/>.
        /// </summary>
        /// <param name="sourceRunes">The source to use for random symbols.</param>
        /// <param name="length">The number of symbols to put into the destination buffer.</param>
        /// <returns>A string populated with randomly selected symbols.</returns>
        /// <remarks>
        ///     This function is <i>somewhat</i> Unicode aware. It correctly handles surrogate pairs, but does not
        ///     support Unicode characters composed of more than one Unicode codepoint.
        /// </remarks>
        /// <example>
        ///     static Runes = "!@#$%^&*".EnumerateRunes().ToArray();
        ///     var string = rng.FillStringFromRunes(Runes, 4);
        ///     <br/>
        ///     Console.WriteLine(string); // %*#@
        /// </example>
        /// <seealso cref="System.Random.GetString"/>
        public string FillStringFromRunes(ReadOnlySpan<Rune> sourceRunes, int length)
        {
            const int stackBufferSize = 32;

            // This is a fixed memory region on the stack, not an allocation.
            Span<char> destBuffer = stackalloc char[stackBufferSize * 2];

            // So discarding it for an array if we need to isn't a big issue.
            if (length > stackBufferSize)
                destBuffer = new char[length * 2];

            var chars = random.FillStringFromRunes(destBuffer, sourceRunes, length);

            // This is in fact how you create a string from a Span<char>, see
            // https://learn.microsoft.com/en-us/dotnet/api/system.span-1.tostring?view=net-10.0
            // Lovely OOP intent overloading right here.
            return destBuffer[0..chars].ToString();
        }
    }

    /// <summary>Picks a random element from a collection.</summary>
    [Obsolete("System.Random based APIs are deprecated.")]
    public static ref T Pick<T>(this System.Random random, ValueList<T> list)
    {
        var index = random.Next(list.Count);
        return ref list[index];
    }

    /// <summary>
    /// Picks a random element from a set and returns it.
    /// This is O(n) as it has to iterate the collection until the target index.
    /// </summary>
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static T Pick<T>(this System.Random random, ICollection<T> collection)
    {
        var index = random.Next(collection.Count);
        var i = 0;
        foreach (var t in collection)
        {
            if (i++ == index)
            {
                return t;
            }
        }

        throw new UnreachableException("This should be unreachable!");
    }

    /// <summary>
    /// Picks a random from a collection then removes it and returns it.
    /// This is O(n) as it has to iterate the collection until the target index.
    /// </summary>
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static T PickAndTake<T>(this System.Random random, ICollection<T> set)
    {
        var tile = Pick(random, set);
        set.Remove(tile);
        return tile;
    }

    /// <summary>
    ///     Generate a random number from a normal (gaussian) distribution.
    /// </summary>
    /// <param name="random">The random object to generate the number from.</param>
    /// <param name="μ">The average or "center" of the normal distribution.</param>
    /// <param name="σ">The standard deviation of the normal distribution.</param>
    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static double NextGaussian(this System.Random random, double μ = 0, double σ = 1)
    {
        // https://stackoverflow.com/a/218600
        var α = random.NextDouble();
        var β = random.NextDouble();

        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(α)) * Math.Sin(2.0 * Math.PI * β);

        return μ + σ * randStdNormal;
    }

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static Angle NextAngle(this System.Random random) => NextFloat(random) * MathF.Tau;

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static Angle NextAngle(this System.Random random, Angle minAngle, Angle maxAngle)
    {
        DebugTools.Assert(minAngle < maxAngle);
        return minAngle + (maxAngle - minAngle) * random.NextDouble();
    }

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static Vector2 NextPolarVector2(this System.Random random, float minMagnitude, float maxMagnitude)
        => random.NextAngle().RotateVec(new Vector2(random.NextFloat(minMagnitude, maxMagnitude), 0));

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static float NextFloat(this System.Random random)
    {
        return random.Next() * 4.6566128752458E-10f;
    }

    [Obsolete("Always use RobustRandom/IRobustRandom, System.Random does not provide any extra functionality.")]
    public static float NextFloat(this System.Random random, float minValue, float maxValue)
        => random.NextFloat() * (maxValue - minValue) + minValue;
}
