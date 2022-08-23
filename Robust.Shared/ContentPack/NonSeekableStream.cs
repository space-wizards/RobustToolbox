using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Shared.ContentPack;

internal sealed class NonSeekableStream : Stream
{
    private readonly Stream _baseStream;

    public NonSeekableStream(Stream baseStream)
    {
        _baseStream = baseStream;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _baseStream.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        return _baseStream.DisposeAsync();
    }

    public override void Flush()
    {
        _baseStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _baseStream.Read(buffer, offset, count);
    }

    public override int Read(Span<byte> buffer)
    {
        return _baseStream.Read(buffer);
    }

    public override int ReadByte()
    {
        return _baseStream.ReadByte();
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        return _baseStream.ReadAsync(buffer, cancellationToken);
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
        _baseStream.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _baseStream.Write(buffer);
    }

    public override void WriteByte(byte value)
    {
        _baseStream.WriteByte(value);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        return _baseStream.WriteAsync(buffer, cancellationToken);
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => _baseStream.CanWrite;
    // .NET mingles seekability and exposing length.
    // This makes absolutely no sense but ok.
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}
