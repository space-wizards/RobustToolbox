using System;
using System.Runtime.InteropServices;
using SpaceWizards.Sodium;

namespace Robust.Shared.Random;

public partial interface IRobustRandom
{
    /// <summary>
    ///     Creates a new IRobustRandom, seeding it using operating system facilities.
    ///     In other words, this returns a differently seeded randomizer every time.
    /// </summary>
    /// <returns>The newly constructed randomizer.</returns>
    public static IDedicatedRandom CreateRandom()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return new RobustRandom();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    ///     Creates a new IRobustRandom, seeding it with the provided integer.
    /// </summary>
    /// <param name="seed">The integer seed to use.</param>
    /// <returns>The newly constructed randomizer.</returns>
    public static IDedicatedRandom CreateSeeded(int seed)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return new RobustRandom(seed);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    ///     Creates a new RobustRandom, seeding it from the given existing randomizer.
    /// </summary>
    /// <param name="source">The randomizer to obtain a new seed from (by calling <see cref="IRobustRandom.Next()"/>)</param>
    /// <returns>The newly constructed randomizer, and its seed.</returns>
    public static (IDedicatedRandom random, int seed) CreateSeededWith(IRobustRandom source)
    {
        var seed = source.Next();

        return (CreateSeeded(seed), seed);
    }

    public static (IDedicatedRandom random, int seed) CreateSeededFromHashable<T>(Span<T> inputs)
        where T : unmanaged
    {
        // Create a byte span so we can hash it
        var bytes = MemoryMarshal.AsBytes(inputs);

        // 16 is the minimum for this algorithm, we only use 4 bytes of it for the seed right now.
        Span<byte> outputBuffer = stackalloc byte[16];

        CryptoGenericHashBlake2B.Hash(outputBuffer, bytes, ReadOnlySpan<byte>.Empty);
        var seed = BitConverter.ToInt32(outputBuffer[0..4]);

        return (CreateSeeded(seed), seed);
    }

    public static (IDedicatedRandom, int seed) CreateSeededFromHashable<T>(T input)
        where T : unmanaged
    {
        return CreateSeededFromHashable([input]);
    }
}
