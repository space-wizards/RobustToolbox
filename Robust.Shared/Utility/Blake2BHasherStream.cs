using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SpaceWizards.Sodium;

namespace Robust.Shared.Utility;

internal sealed class Blake2BHasherStream : Stream
{
    private readonly bool _reader;

    public readonly int OutputLength;
    public readonly Stream WrappingStream;

    public CryptoGenericHashBlake2B.State State;

    private Blake2BHasherStream(Stream wrapping, bool reader, ReadOnlySpan<byte> key, int outputLength)
    {
        OutputLength = outputLength;
        WrappingStream = wrapping;
        _reader = reader;

        CryptoGenericHashBlake2B.Init(ref State, key, outputLength);
    }

    public byte[] Finish()
    {
        var result = new byte[OutputLength];

        CryptoGenericHashBlake2B.Final(ref State, result);

        return result;
    }

    public static Blake2BHasherStream CreateReader(Stream wrapping, ReadOnlySpan<byte> key, int outputLength)
    {
        if (!wrapping.CanRead)
            throw new ArgumentException("Must pass readable stream.");

        return new Blake2BHasherStream(wrapping, true, key, outputLength);
    }

    public static Blake2BHasherStream CreateWriter(Stream wrapping, ReadOnlySpan<byte> key, int outputLength)
    {
        if (!wrapping.CanWrite)
            throw new ArgumentException("Must pass writeable stream.");

        return new Blake2BHasherStream(wrapping, false, key, outputLength);
    }

    public override void Flush()
    {
        if (!CanWrite)
            throw new InvalidOperationException();

        WrappingStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!CanRead)
            throw new InvalidOperationException();

        var read = WrappingStream.Read(buffer, offset, count);

        if (read > 0)
            CryptoGenericHashBlake2B.Update(ref State, buffer.AsSpan(offset, read));

        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        if (!CanRead)
            throw new InvalidOperationException();

        var read = WrappingStream.Read(buffer);

        if (read > 0)
            CryptoGenericHashBlake2B.Update(ref State, buffer[..read]);

        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!CanRead)
            throw new InvalidOperationException();

        var read = await WrappingStream.ReadAsync(buffer, cancellationToken);

        if (read > 0)
            CryptoGenericHashBlake2B.Update(ref State, buffer[..read].Span);

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!CanWrite)
            throw new InvalidOperationException();

        WrappingStream.Write(buffer, offset, count);
        CryptoGenericHashBlake2B.Update(ref State, buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (!CanWrite)
            throw new InvalidOperationException();

        WrappingStream.Write(buffer);
        CryptoGenericHashBlake2B.Update(ref State, buffer);
    }

    public override void WriteByte(byte value)
    {
        Span<byte> buf = stackalloc byte[1];
        buf[0] = value;
        Write(buf);
    }

    public override bool CanRead => _reader;
    public override bool CanSeek => false;
    public override bool CanWrite => !_reader;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}
