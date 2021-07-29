using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
#pragma warning disable 169

namespace Robust.Shared.Utility
{
    /// <summary>
    /// Fixed size array stack allocation helpers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This helper class can be used to work around the limitation that <c>stackalloc</c>
    /// cannot be used with ref types.
    /// </para>
    /// <para>
    /// To use, call it like so:
    /// <code>
    /// var span = FixedArray.Alloc32&lt;object&lt;(out _);
    /// </code>
    /// There is an <c>out</c> parameter that you should probably always discard (as shown in the example).
    /// This is so that stack space is properly allocated in your stack frame.
    /// </para>
    /// <para>
    /// Do NOT under ANY CIRCUMSTANCE return the span given up the stack in any way.
    /// You will break the GC and stack and (worst of all) I will shed you.
    /// </para>
    /// <para>
    /// This class cannot be used with variable size allocations.
    /// Just allocate a we'll-never-need-more bound like 128 and slim it down.
    /// </para>
    /// </remarks>
    [PublicAPI]
    [SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeNotEvident")]
    internal static class FixedArray
    {
        public static Span<T> Alloc2<T>(out FixedArray2<T> discard)
        {
            discard = new();
            return discard.AsSpan;
        }

        public static Span<T> Alloc4<T>(out FixedArray4<T> discard)
        {
            discard = new();
            return discard.AsSpan;
        }

        public static Span<T> Alloc8<T>(out FixedArray8<T> discard)
        {
            discard = new();
            return discard.AsSpan;
        }

        public static Span<T> Alloc16<T>(out FixedArray16<T> discard)
        {
            discard = new();
            return discard.AsSpan;
        }

        public static Span<T> Alloc32<T>(out FixedArray32<T> discard)
        {
            discard = new();
            return discard.AsSpan;
        }

        public static Span<T> Alloc64<T>(out FixedArray64<T> discard)
        {
            discard = new();
            return discard.AsSpan;
        }

        public static Span<T> Alloc128<T>(out FixedArray128<T> discard)
        {
            discard = new();
            return discard.AsSpan;
        }
    }

    internal struct FixedArray2<T>
    {
        private T _00;
        private T _01;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 2);
    }

    internal struct FixedArray4<T>
    {
        private T _00;
        private T _01;
        private T _02;
        private T _03;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 4);
    }

    internal struct FixedArray8<T>
    {
        private T _00;
        private T _01;
        private T _02;
        private T _03;
        private T _04;
        private T _05;
        private T _06;
        private T _07;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 8);
    }

    internal struct FixedArray16<T>
    {
        private T _00;
        private T _01;
        private T _02;
        private T _03;
        private T _04;
        private T _05;
        private T _06;
        private T _07;
        private T _08;
        private T _09;
        private T _10;
        private T _11;
        private T _12;
        private T _13;
        private T _14;
        private T _15;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 16);
    }

    internal struct FixedArray32<T>
    {
        private T _00;
        private T _01;
        private T _02;
        private T _03;
        private T _04;
        private T _05;
        private T _06;
        private T _07;
        private T _08;
        private T _09;
        private T _10;
        private T _11;
        private T _12;
        private T _13;
        private T _14;
        private T _15;
        private T _16;
        private T _17;
        private T _18;
        private T _19;
        private T _20;
        private T _21;
        private T _22;
        private T _23;
        private T _24;
        private T _25;
        private T _26;
        private T _27;
        private T _28;
        private T _29;
        private T _30;
        private T _31;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 32);
    }

    internal struct FixedArray64<T>
    {
        private T _00;
        private T _01;
        private T _02;
        private T _03;
        private T _04;
        private T _05;
        private T _06;
        private T _07;
        private T _08;
        private T _09;
        private T _10;
        private T _11;
        private T _12;
        private T _13;
        private T _14;
        private T _15;
        private T _16;
        private T _17;
        private T _18;
        private T _19;
        private T _20;
        private T _21;
        private T _22;
        private T _23;
        private T _24;
        private T _25;
        private T _26;
        private T _27;
        private T _28;
        private T _29;
        private T _30;
        private T _31;
        private T _32;
        private T _33;
        private T _34;
        private T _35;
        private T _36;
        private T _37;
        private T _38;
        private T _39;
        private T _40;
        private T _41;
        private T _42;
        private T _43;
        private T _44;
        private T _45;
        private T _46;
        private T _47;
        private T _48;
        private T _49;
        private T _50;
        private T _51;
        private T _52;
        private T _53;
        private T _54;
        private T _55;
        private T _56;
        private T _57;
        private T _58;
        private T _59;
        private T _60;
        private T _61;
        private T _62;
        private T _63;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 64);
    }

    internal struct FixedArray128<T>
    {
        private T _00;
        private T _01;
        private T _02;
        private T _03;
        private T _04;
        private T _05;
        private T _06;
        private T _07;
        private T _08;
        private T _09;
        private T _10;
        private T _11;
        private T _12;
        private T _13;
        private T _14;
        private T _15;
        private T _16;
        private T _17;
        private T _18;
        private T _19;
        private T _20;
        private T _21;
        private T _22;
        private T _23;
        private T _24;
        private T _25;
        private T _26;
        private T _27;
        private T _28;
        private T _29;
        private T _30;
        private T _31;
        private T _32;
        private T _33;
        private T _34;
        private T _35;
        private T _36;
        private T _37;
        private T _38;
        private T _39;
        private T _40;
        private T _41;
        private T _42;
        private T _43;
        private T _44;
        private T _45;
        private T _46;
        private T _47;
        private T _48;
        private T _49;
        private T _50;
        private T _51;
        private T _52;
        private T _53;
        private T _54;
        private T _55;
        private T _56;
        private T _57;
        private T _58;
        private T _59;
        private T _60;
        private T _61;
        private T _62;
        private T _63;
        private T _64;
        private T _65;
        private T _66;
        private T _67;
        private T _68;
        private T _69;
        private T _70;
        private T _71;
        private T _72;
        private T _73;
        private T _74;
        private T _75;
        private T _76;
        private T _77;
        private T _78;
        private T _79;
        private T _80;
        private T _81;
        private T _82;
        private T _83;
        private T _84;
        private T _85;
        private T _86;
        private T _87;
        private T _88;
        private T _89;
        private T _90;
        private T _91;
        private T _92;
        private T _93;
        private T _94;
        private T _95;
        private T _96;
        private T _97;
        private T _98;
        private T _99;
        private T _100;
        private T _101;
        private T _102;
        private T _103;
        private T _104;
        private T _105;
        private T _106;
        private T _107;
        private T _108;
        private T _109;
        private T _110;
        private T _111;
        private T _112;
        private T _113;
        private T _114;
        private T _115;
        private T _116;
        private T _117;
        private T _118;
        private T _119;
        private T _120;
        private T _121;
        private T _122;
        private T _123;
        private T _124;
        private T _125;
        private T _126;
        private T _127;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 128);
    }
}
