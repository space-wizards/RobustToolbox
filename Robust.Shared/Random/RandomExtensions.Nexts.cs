using System;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Maths;

namespace Robust.Shared.Random;

public static partial class RandomExtensions
{
    extension<T>(T random)
        where T : IRobustRandom
    {
        /// <summary>
        ///     Get a random float between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public float NextFloat(float minValue, float maxValue)
            => random.NextFloat() * (maxValue - minValue) + minValue;

        /// <summary>
        ///     Get a random float between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public float NextFloat(float maxValue) => random.NextFloat() * maxValue;

        /// <summary>
        ///     Get a random byte between 0 (inclusive) and <see cref="byte.MaxValue"/> (exclusive).
        /// </summary>
        [MustUseReturnValue]
        public byte NextByte()
            => random.NextByte(byte.MaxValue);

        /// <summary>
        ///     Get a random byte between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public byte NextByte(byte maxValue)
            => (byte)random.Next(maxValue);

        /// <summary>
        ///     Get a random byte between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public byte NextByte(byte minValue, byte maxValue)
            => (byte)random.Next(minValue, maxValue);

        /// <summary>
        ///     Get a random double between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [Obsolete("Use NextDouble instead, this method was named incorrectly.")]
        public double Next(double maxValue)
            => random.NextDouble() * maxValue;

        /// <summary>
        ///     Get a random double between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public double NextDouble(double maxValue)
            => random.NextDouble() * maxValue;

        /// <summary>
        ///     Get a random double between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public double NextDouble(double minValue, double maxValue)
            => random.NextDouble() * (maxValue - minValue) + minValue;

        /// <summary>
        ///     Get a random byte between 0 (inclusive) and <see cref="MathF.Tau"/> (exclusive).
        /// </summary>
        [MustUseReturnValue]
        public Angle NextAngle()
            => random.NextFloat() * MathF.Tau;

        /// <summary>
        ///     Get a random angle between 0 (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public Angle NextAngle(Angle maxValue)
            => random.NextFloat() * maxValue;

        /// <summary>
        ///     Get a random angle between <paramref name="minValue"/> (inclusive) and <paramref name="maxValue"/> (exclusive).
        /// </summary>
        /// <param name="minValue">Inclusive lower bound on the random value.</param>
        /// <param name="maxValue">Exclusive upper bound on the random value.</param>
        [MustUseReturnValue]
        public Angle NextAngle(Angle minValue, Angle maxValue)
            => random.NextFloat() * (maxValue - minValue) + minValue;

        /// <summary>
        ///     Random vector, created from a uniform distribution of magnitudes and angles.
        /// </summary>
        /// <param name="maxMagnitude">Max value for randomized vector magnitude (exclusive).</param>
        [MustUseReturnValue]
        public Vector2 NextVector2(float maxMagnitude = 1)
            => random.NextVector2(0, maxMagnitude);

        /// <summary>
        ///     Random vector, created from a uniform distribution of magnitudes and angles.
        /// </summary>
        /// <param name="minMagnitude">Min value for randomized vector magnitude (inclusive).</param>
        /// <param name="maxMagnitude">Max value for randomized vector magnitude (exclusive).</param>
        /// <remarks>
        ///     In general, NextVector2(1) will tend to result in vectors with smaller magnitudes than
        ///     NextVector2Box(1,1), even if you ignored any vectors with a magnitude larger than one.
        /// </remarks>
        [MustUseReturnValue]
        public Vector2 NextVector2(float minMagnitude, float maxMagnitude)
            => random.NextAngle().RotateVec(new Vector2(random.NextFloat(minMagnitude, maxMagnitude), 0));

        /// <summary>
        ///     Random vector, created from a uniform distribution of x and y coordinates lying inside some box.
        /// </summary>
        [MustUseReturnValue]
        public Vector2 NextVector2Box(float minX, float minY, float maxX, float maxY)
            => new (random.NextFloat(minX, maxX), random.NextFloat(minY, maxY));

        /// <summary>
        ///     Random vector, created from a uniform distribution of x and y coordinates lying inside some box.
        ///     Box will have coordinates starting at [-<paramref name="maxAbsX"/> , -<paramref name="maxAbsY"/>]
        ///     and ending in [<paramref name="maxAbsX"/> , <paramref name="maxAbsY"/>]
        /// </summary>
        [MustUseReturnValue]
        public Vector2 NextVector2Box(float maxAbsX = 1, float maxAbsY = 1)
            => random.NextVector2Box(-maxAbsX, -maxAbsY, maxAbsX, maxAbsY);

        /// <summary>
        ///     Generate a random number from a normal (gaussian) distribution.
        /// </summary>
        /// <param name="μ">The average or "center" of the normal distribution.</param>
        /// <param name="σ">The standard deviation of the normal distribution.</param>
        [MustUseReturnValue]
        public double NextGaussian(double μ = 0, double σ = 1)
        {
            // https://stackoverflow.com/a/218600
            var α = random.NextDouble();
            var β = random.NextDouble();

            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(α)) * Math.Sin(2.0 * Math.PI * β);

            return μ + σ * randStdNormal;
        }

    }
}
