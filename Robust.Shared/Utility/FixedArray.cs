using System;
using System.Collections.Generic;
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
    /// var span = FixedArray.Alloc32&lt;object&gt;(out _);
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

    internal struct FixedArray2<T> : IEquatable<FixedArray2<T>>
    {
        public T _00;
        public T _01;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 2);

        internal FixedArray2(T x0, T x1)
        {
            _00 = x0;
            _01 = x1;
        }

        public bool Equals(FixedArray2<T> other)
        {
            return EqualityComparer<T>.Default.Equals(_00, other._00) &&
                   EqualityComparer<T>.Default.Equals(_01, other._01);
        }

        public override bool Equals(object? obj)
        {
            return obj is FixedArray2<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_00, _01);
        }
    }

    internal struct FixedArray4<T> : IEquatable<FixedArray4<T>>
    {
        public T _00;
        public T _01;
        public T _02;
        public T _03;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 4);

        internal FixedArray4(T x0, T x1, T x2, T x3)
        {
            _00 = x0;
            _01 = x1;
            _02 = x2;
            _03 = x3;
        }

        public bool Equals(FixedArray4<T> other)
        {
            return EqualityComparer<T>.Default.Equals(_00, other._00) &&
                   EqualityComparer<T>.Default.Equals(_01, other._01) &&
                   EqualityComparer<T>.Default.Equals(_02, other._02) &&
                   EqualityComparer<T>.Default.Equals(_03, other._03);
        }

        public override bool Equals(object? obj)
        {
            return obj is FixedArray4<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_00, _01, _02, _03);
        }
    }

    internal struct FixedArray8<T> : IEquatable<FixedArray8<T>>
    {
        public T _00;
        public T _01;
        public T _02;
        public T _03;
        public T _04;
        public T _05;
        public T _06;
        public T _07;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 8);

        internal FixedArray8(T x0, T x1, T x2, T x3, T x4, T x5, T x6, T x7)
        {
            _00 = x0;
            _01 = x1;
            _02 = x2;
            _03 = x3;
            _04 = x4;
            _05 = x5;
            _06 = x6;
            _07 = x7;
        }

        public bool Equals(FixedArray8<T> other)
        {
            return EqualityComparer<T>.Default.Equals(_00, other._00) &&
                   EqualityComparer<T>.Default.Equals(_01, other._01) &&
                   EqualityComparer<T>.Default.Equals(_02, other._02) &&
                   EqualityComparer<T>.Default.Equals(_03, other._03) &&
                   EqualityComparer<T>.Default.Equals(_04, other._04) &&
                   EqualityComparer<T>.Default.Equals(_05, other._05) &&
                   EqualityComparer<T>.Default.Equals(_06, other._06) &&
                   EqualityComparer<T>.Default.Equals(_07, other._07);
        }

        public override bool Equals(object? obj)
        {
            return obj is FixedArray8<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_00, _01, _02, _03, _04, _05, _06, _07);
        }
    }

    internal struct FixedArray16<T>
    {
        public T _00;
        public T _01;
        public T _02;
        public T _03;
        public T _04;
        public T _05;
        public T _06;
        public T _07;
        public T _08;
        public T _09;
        public T _10;
        public T _11;
        public T _12;
        public T _13;
        public T _14;
        public T _15;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 16);
    }

    internal struct FixedArray32<T>
    {
        public T _00;
        public T _01;
        public T _02;
        public T _03;
        public T _04;
        public T _05;
        public T _06;
        public T _07;
        public T _08;
        public T _09;
        public T _10;
        public T _11;
        public T _12;
        public T _13;
        public T _14;
        public T _15;
        public T _16;
        public T _17;
        public T _18;
        public T _19;
        public T _20;
        public T _21;
        public T _22;
        public T _23;
        public T _24;
        public T _25;
        public T _26;
        public T _27;
        public T _28;
        public T _29;
        public T _30;
        public T _31;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 32);
    }

    internal struct FixedArray64<T>
    {
        public T _00;
        public T _01;
        public T _02;
        public T _03;
        public T _04;
        public T _05;
        public T _06;
        public T _07;
        public T _08;
        public T _09;
        public T _10;
        public T _11;
        public T _12;
        public T _13;
        public T _14;
        public T _15;
        public T _16;
        public T _17;
        public T _18;
        public T _19;
        public T _20;
        public T _21;
        public T _22;
        public T _23;
        public T _24;
        public T _25;
        public T _26;
        public T _27;
        public T _28;
        public T _29;
        public T _30;
        public T _31;
        public T _32;
        public T _33;
        public T _34;
        public T _35;
        public T _36;
        public T _37;
        public T _38;
        public T _39;
        public T _40;
        public T _41;
        public T _42;
        public T _43;
        public T _44;
        public T _45;
        public T _46;
        public T _47;
        public T _48;
        public T _49;
        public T _50;
        public T _51;
        public T _52;
        public T _53;
        public T _54;
        public T _55;
        public T _56;
        public T _57;
        public T _58;
        public T _59;
        public T _60;
        public T _61;
        public T _62;
        public T _63;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 64);
    }

    internal struct FixedArray128<T>
    {
        public T _00;
        public T _01;
        public T _02;
        public T _03;
        public T _04;
        public T _05;
        public T _06;
        public T _07;
        public T _08;
        public T _09;
        public T _10;
        public T _11;
        public T _12;
        public T _13;
        public T _14;
        public T _15;
        public T _16;
        public T _17;
        public T _18;
        public T _19;
        public T _20;
        public T _21;
        public T _22;
        public T _23;
        public T _24;
        public T _25;
        public T _26;
        public T _27;
        public T _28;
        public T _29;
        public T _30;
        public T _31;
        public T _32;
        public T _33;
        public T _34;
        public T _35;
        public T _36;
        public T _37;
        public T _38;
        public T _39;
        public T _40;
        public T _41;
        public T _42;
        public T _43;
        public T _44;
        public T _45;
        public T _46;
        public T _47;
        public T _48;
        public T _49;
        public T _50;
        public T _51;
        public T _52;
        public T _53;
        public T _54;
        public T _55;
        public T _56;
        public T _57;
        public T _58;
        public T _59;
        public T _60;
        public T _61;
        public T _62;
        public T _63;
        public T _64;
        public T _65;
        public T _66;
        public T _67;
        public T _68;
        public T _69;
        public T _70;
        public T _71;
        public T _72;
        public T _73;
        public T _74;
        public T _75;
        public T _76;
        public T _77;
        public T _78;
        public T _79;
        public T _80;
        public T _81;
        public T _82;
        public T _83;
        public T _84;
        public T _85;
        public T _86;
        public T _87;
        public T _88;
        public T _89;
        public T _90;
        public T _91;
        public T _92;
        public T _93;
        public T _94;
        public T _95;
        public T _96;
        public T _97;
        public T _98;
        public T _99;
        public T _100;
        public T _101;
        public T _102;
        public T _103;
        public T _104;
        public T _105;
        public T _106;
        public T _107;
        public T _108;
        public T _109;
        public T _110;
        public T _111;
        public T _112;
        public T _113;
        public T _114;
        public T _115;
        public T _116;
        public T _117;
        public T _118;
        public T _119;
        public T _120;
        public T _121;
        public T _122;
        public T _123;
        public T _124;
        public T _125;
        public T _126;
        public T _127;

        public Span<T> AsSpan => MemoryMarshal.CreateSpan(ref _00, 128);
    }
}
