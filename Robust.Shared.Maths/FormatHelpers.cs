using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

// Leaving this in this namespace because I only need this class here to work around maths being a separate assembly.
// ReSharper disable once CheckNamespace
namespace Robust.Shared.Utility;

/// <summary>
/// Helpers for dealing with string formatting and related things.
/// </summary>
public static class FormatHelpers
{
    /// <summary>
    /// Format a string interpolation into a given buffer. If the buffer is not large enough, the result is truncated.
    /// </summary>
    /// <remarks>
    /// Assuming everything you're formatting with implements <see cref="ISpanFormattable"/>, this should be zero-alloc.
    /// </remarks>
    /// <param name="buffer">The buffer to format into.</param>
    /// <param name="handler">String interpolation handler to implement buffer formatting logic.</param>
    /// <returns>The amount of chars written into the buffer.</returns>
    public static int FormatInto(
        Span<char> buffer,
        [InterpolatedStringHandlerArgument("buffer")] ref BufferInterpolatedStringHandler handler)
    {
        return handler.Length;
    }

    /// <summary>
    /// Tries to format a string interpolation into a given buffer.
    /// If the buffer is not large enough, the result is truncated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is intended to be used as an easy implementation of <see cref="ISpanFormattable"/>.
    /// </para>
    /// <para>
    /// Assuming everything you're formatting with implements <see cref="ISpanFormattable"/>,
    /// this should be zero-alloc on release.
    /// </para>
    /// </remarks>
    /// <param name="buffer">The buffer to format into.</param>
    /// <param name="charsWritten">The amount of chars written into the buffer.</param>
    /// <param name="handler">String interpolation handler to implement buffer formatting logic.</param>
    /// <returns>False if the formatting failed due to lack of space and was truncated.</returns>
    public static bool TryFormatInto(
        Span<char> buffer,
        out int charsWritten,
        [InterpolatedStringHandlerArgument("buffer")] ref BufferInterpolatedStringHandler handler)
    {
        charsWritten = handler.Length;
        return !handler.Truncated;
    }

    /// <summary>
    /// Format a string interpolation into a given memory buffer.
    /// If the buffer is not large enough, the result is truncated.
    /// </summary>
    /// <param name="buffer">The memory buffer to format into.</param>
    /// <param name="handler">String interpolation handler to implement buffer formatting logic.</param>
    /// <returns>The region of memory filled by the formatting operation.</returns>
    public static Memory<char> FormatIntoMem(
        Memory<char> buffer,
        [InterpolatedStringHandlerArgument("buffer")] ref MemoryBufferInterpolatedStringHandler handler)
    {
        return buffer[..handler.Handler.Length];
    }

    /// <summary>
    /// Copy the contents of a <see cref="StringBuilder"/> into a memory buffer, giving back the subregion used.
    /// If the buffer is not enough space, the result is truncated.
    /// </summary>
    /// <param name="builder">The <see cref="StringBuilder"/> to copy from</param>
    /// <param name="memory">The memory buffer to copy into.</param>
    /// <returns>The memory region actually used by the copied data.</returns>
    public static Memory<char> BuilderToMemory(StringBuilder builder, Memory<char> memory)
    {
        var truncLength = Math.Min(builder.Length, memory.Length);
        builder.CopyTo(0, memory.Span, truncLength);
        return memory[..truncLength];
    }
}

/// <summary>
/// Interpolated string handler used by <see cref="FormatHelpers.FormatInto"/>.
/// </summary>
[InterpolatedStringHandler]
public ref struct BufferInterpolatedStringHandler
{
    private Span<char> _buffer;
    internal int Length;
    internal bool Truncated;

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public BufferInterpolatedStringHandler(int literalLength, int formattedCount, Span<char> buffer)
    {
        _buffer = buffer;
        Length = 0;
        Truncated = false;
    }

    public void AppendLiteral(string literal)
    {
        AppendString(literal);
    }

    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        AppendString(value);
    }

    [SuppressMessage("ReSharper", "MergeCastWithTypeCheck")]
    public void AppendFormatted<T>(T value)
    {
        if (value is ISpanFormattable)
        {
            // JIT is able to avoid boxing due to call structure.
            ((ISpanFormattable)value).TryFormat(_buffer, out var written, default, null);
            Advance(written);
            return;
        }

        var str = value?.ToString();
        if (str != null)
            AppendString(str);
    }

    [SuppressMessage("ReSharper", "MergeCastWithTypeCheck")]
    public void AppendFormatted<T>(T value, string format)
    {
        string? str;
        if (value is IFormattable)
        {
            if (value is ISpanFormattable)
            {
                // JIT is able to avoid boxing due to call structure.
                Truncated |= !((ISpanFormattable)value).TryFormat(_buffer, out var written, format, null);
                Advance(written);
                return;
            }

            // JIT is able to avoid boxing due to call structure.
            str = ((IFormattable)value).ToString(format, null);
        }
        else
        {
            str = value?.ToString();
        }

        if (str != null)
            AppendString(str);
    }

    private void AppendString(ReadOnlySpan<char> value)
    {
        var copyLength = value.Length;
        if (copyLength > _buffer.Length)
        {
            copyLength = _buffer.Length;
            Truncated = true;
        }

        value[..copyLength].CopyTo(_buffer);
        Advance(copyLength);
    }

    private void Advance(int amount)
    {
        _buffer = _buffer[amount..];
        Length += amount;
    }
}

/// <summary>
/// Interpolated string handler used by <see cref="FormatHelpers.FormatIntoMem"/>.
/// </summary>
/// <remarks>
/// Only exists as workaround for https://youtrack.jetbrains.com/issue/RIDER-78472.
/// </remarks>
[InterpolatedStringHandler]
public ref struct MemoryBufferInterpolatedStringHandler
{
    public BufferInterpolatedStringHandler Handler;

    public MemoryBufferInterpolatedStringHandler(int literalLength, int formattedCount, Memory<char> buffer)
    {
        Handler = new BufferInterpolatedStringHandler(literalLength, formattedCount, buffer.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string literal) => Handler.AppendLiteral(literal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(ReadOnlySpan<char> value) => Handler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value) => Handler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, string format) => Handler.AppendFormatted(value, format);
}
