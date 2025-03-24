using System;
using System.Buffers;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SharpZstd.Interop;
using static Robust.Shared.Native.Libc;
using static SharpZstd.Interop.Zstd;

namespace Robust.Shared.Utility;

public static class ZStd
{
    public static int CompressBound(int length)
    {
        return (int)ZSTD_COMPRESSBOUND((nuint)length);
    }

    public static unsafe int Compress(
        Span<byte> into,
        ReadOnlySpan<byte> data,
        int compressionLevel = ZSTD_CLEVEL_DEFAULT)
    {
        fixed (byte* dst = into)
        fixed (byte* src = data)
        {
            var result = ZSTD_compress(dst, (nuint) into.Length, src, (nuint) data.Length, compressionLevel);
            ZStdException.ThrowIfError(result);
            return (int) result;
        }
    }

#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void InitZStd()
    {
        try
        {
            NativeLibrary.SetDllImportResolver(
                typeof(Zstd).Assembly,
                ResolveZstd
            );
        }
        catch (InvalidOperationException)
        {
            // Launcher loader probably already set this, ignore.
        }
    }

    private static IntPtr ResolveZstd(string name, Assembly assembly, DllImportSearchPath? path)
    {
        if (name == "zstd" && OperatingSystem.IsLinux())
        {
            // Try zstd.so we ship ourselves.
            if (NativeLibrary.TryLoad("zstd.so", typeof(Zstd).Assembly, null, out var handle))
                return handle;

            // Try some extra paths from the system too worst case.
            if (NativeLibrary.TryLoad("libzstd.so.1", out handle))
                return handle;

            if (NativeLibrary.TryLoad("libzstd.so", out handle))
                return handle;
        }

        return IntPtr.Zero;
    }
}

[Serializable]
internal sealed class ZStdException : Exception
{
    public ZStdException()
    {
    }

    public ZStdException(string message) : base(message)
    {
    }

    public ZStdException(string message, Exception inner) : base(message, inner)
    {
    }

    public static unsafe ZStdException FromCode(nuint code)
    {
        return new ZStdException(Marshal.PtrToStringUTF8((IntPtr)ZSTD_getErrorName(code))!);
    }

    public static void ThrowIfError(nuint code)
    {
        if (ZSTD_isError(code) != 0)
            throw FromCode(code);
    }
}

public sealed unsafe class ZStdCompressionContext : IDisposable
{
    public ZSTD_CCtx* Context { get; private set; }

    private bool Disposed => Context == null;

    public ZStdCompressionContext()
    {
        Context = ZSTD_createCCtx();
    }

    public void SetParameter(ZSTD_cParameter parameter, int value)
    {
        CheckDisposed();

        ZSTD_CCtx_setParameter(Context, parameter, value);
    }

    public int Compress(Span<byte> destination, ReadOnlySpan<byte> source, int compressionLevel = ZSTD_CLEVEL_DEFAULT)
    {
        CheckDisposed();

        fixed (byte* dst = destination)
        fixed (byte* src = source)
        {
            var ret = ZSTD_compressCCtx(
                Context,
                dst, (nuint)destination.Length,
                src, (nuint)source.Length,
                compressionLevel);

            ZStdException.ThrowIfError(ret);
            return (int)ret;
        }
    }

    public int Compress2(Span<byte> destination, ReadOnlySpan<byte> source)
    {
        CheckDisposed();

        fixed (byte* dst = destination)
        fixed (byte* src = source)
        {
            var ret = ZSTD_compress2(
                Context,
                dst, (nuint)destination.Length,
                src, (nuint)source.Length);

            ZStdException.ThrowIfError(ret);
            return (int)ret;
        }
    }

    ~ZStdCompressionContext()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (Disposed)
            return;

        ZSTD_freeCCtx(Context);
        Context = null;
        GC.SuppressFinalize(this);
    }

    private void CheckDisposed()
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(ZStdCompressionContext));
    }
}

public sealed class ZStdDecompressStream : Stream
{
    private readonly Stream _baseStream;
    private readonly bool _ownStream;
    private readonly unsafe ZSTD_DCtx* _ctx;
    private readonly byte[] _buffer;
    private int _bufferPos;
    private int _bufferSize;
    private bool _disposed;

    public unsafe ZStdDecompressStream(Stream baseStream, bool ownStream = true)
    {
        _baseStream = baseStream;
        _ownStream = ownStream;
        _ctx = ZSTD_createDCtx();
        _buffer = ArrayPool<byte>.Shared.Rent((int)ZSTD_DStreamInSize());
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        _disposed = true;
        ZSTD_freeDCtx(_ctx);

        if (disposing)
        {
            if (_ownStream)
                _baseStream.Dispose();

            ArrayPool<byte>.Shared.Return(_buffer);
        }
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        _baseStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int ReadByte()
    {
        Span<byte> buf = stackalloc byte[1];
        return Read(buf) == 0 ? -1 : buf[0];
    }

    public override unsafe int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        do
        {
            if (_bufferSize == 0 || _bufferPos == _bufferSize)
            {
                _bufferPos = 0;
                _bufferSize = _baseStream.Read(_buffer);

                if (_bufferSize == 0)
                    return 0;
            }

            fixed (byte* inputPtr = _buffer)
            fixed (byte* outputPtr = buffer)
            {
                var outputBuf = new ZSTD_outBuffer { dst = outputPtr, pos = 0, size = (nuint)buffer.Length };
                var inputBuf = new ZSTD_inBuffer { src = inputPtr, pos = (nuint)_bufferPos, size = (nuint)_bufferSize };
                var ret = ZSTD_decompressStream(_ctx, &outputBuf, &inputBuf);

                _bufferPos = (int)inputBuf.pos;
                ZStdException.ThrowIfError(ret);

                if (outputBuf.pos > 0)
                    return (int)outputBuf.pos;
            }
        } while (true);
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        do
        {
            if (_bufferSize == 0 || _bufferPos == _bufferSize)
            {
                _bufferPos = 0;
                _bufferSize = await _baseStream.ReadAsync(_buffer, cancellationToken);

                if (_bufferSize == 0)
                    return 0;
            }

            var ret = DecompressChunk(this, buffer.Span);
            if (ret > 0)
                return (int)ret;

        } while (true);

        static unsafe nuint DecompressChunk(ZStdDecompressStream stream, Span<byte> buffer)
        {
            fixed (byte* inputPtr = stream._buffer)
            fixed (byte* outputPtr = buffer)
            {
                ZSTD_outBuffer outputBuf = default;
                outputBuf.dst = outputPtr;
                outputBuf.pos = 0;
                outputBuf.size = (nuint)buffer.Length;
                ZSTD_inBuffer inputBuf = default;
                inputBuf.src = inputPtr;
                inputBuf.pos = (nuint)stream._bufferPos;
                inputBuf.size = (nuint)stream._bufferSize;

                var ret = ZSTD_decompressStream(stream._ctx, &outputBuf, &inputBuf);

                stream._bufferPos = (int)inputBuf.pos;
                ZStdException.ThrowIfError(ret);

                return outputBuf.pos;
            }
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ZStdDecompressStream));
    }
}

public sealed class ZStdCompressStream : Stream
{
    private readonly Stream _baseStream;
    private readonly bool _ownStream;
    public ZStdCompressionContext Context { get; }
    private readonly byte[] _buffer;
    private int _bufferPos;
    private bool _disposed;
    private bool _hasSession;

    public ZStdCompressStream(Stream baseStream, bool ownStream = true)
    {
        Context = new ZStdCompressionContext();
        _baseStream = baseStream;
        _ownStream = ownStream;
        _buffer = ArrayPool<byte>.Shared.Rent((int)ZSTD_CStreamOutSize());
    }

    public override void Flush()
    {
        FlushInternal(ZSTD_EndDirective.ZSTD_e_flush);
    }

    public void FlushEnd()
    {
        _hasSession = false;
        FlushInternal(ZSTD_EndDirective.ZSTD_e_end);
    }

    private unsafe void FlushInternal(ZSTD_EndDirective directive)
    {
        fixed (byte* outPtr = _buffer)
        {
            ZSTD_outBuffer outBuf = default;
            outBuf.size = (nuint)_buffer.Length;
            outBuf.pos = (nuint)_bufferPos;
            outBuf.dst = outPtr;

            ZSTD_inBuffer inBuf = default;

            while (true)
            {
                var err = ZSTD_compressStream2(Context.Context, &outBuf, &inBuf, directive);
                ZStdException.ThrowIfError(err);
                _bufferPos = (int)outBuf.pos;

                _baseStream.Write(_buffer.AsSpan(0, (int)outBuf.pos));
                _bufferPos = 0;
                outBuf.pos = 0;

                if (err == 0)
                    break;
            }
        }

        _baseStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(buffer.AsSpan(offset, count));
    }

    public override unsafe void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();

        _hasSession = true;

        fixed (byte* outPtr = _buffer)
        fixed (byte* inPtr = buffer)
        {
            ZSTD_outBuffer outBuf = default;
            outBuf.size = (nuint)_buffer.Length;
            outBuf.pos = (nuint)_bufferPos;
            outBuf.dst = outPtr;

            ZSTD_inBuffer inBuf = default;
            inBuf.pos = 0;
            inBuf.size = (nuint)buffer.Length;
            inBuf.src = inPtr;

            while (true)
            {
                var err = ZSTD_compressStream2(Context.Context, &outBuf, &inBuf, ZSTD_EndDirective.ZSTD_e_continue);
                ZStdException.ThrowIfError(err);
                _bufferPos = (int)outBuf.pos;

                if (inBuf.pos >= inBuf.size)
                    break;

                // Not all input data consumed. Flush output buffer and continue.
                _baseStream.Write(_buffer.AsSpan(0, (int)outBuf.pos));
                _bufferPos = 0;
                outBuf.pos = 0;
            }
        }
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_disposed)
            return;

        if (disposing)
        {
            if (_hasSession)
                FlushEnd();

            if (_ownStream)
                _baseStream.Dispose();

            ArrayPool<byte>.Shared.Return(_buffer);
            Context.Dispose();
        }

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ZStdCompressStream));
    }
}
