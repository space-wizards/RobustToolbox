using System;
using System.Buffers;
using System.IO;

namespace Robust.Shared.Utility;

// NOTE: ReadOnlySequence<T> is NOT content safe due to tearing. Maybe. It looks like that. Idk.

/// <summary>
/// Stream that effectively writes to a <see cref="ReadOnlySequence{T}"/> of bytes.
/// Less allocations over <see cref="MemoryStream"/> in some scenarios.
/// </summary>
internal sealed class SequenceMemoryStream : Stream
{
    // private long _length;
    private readonly int _segmentLength;
    private readonly SequenceSegment _startSegment;
    private SequenceSegment _curSegment;
    private int _curSegmentWritten;

    public SequenceMemoryStream(int segmentLength = 512 * 1024)
    {
        _segmentLength = segmentLength;
        _startSegment = _curSegment = new SequenceSegment(segmentLength, 0);
    }

    public ReadOnlySequence<byte> AsSequence => new(_startSegment, 0, _curSegment, _curSegmentWritten);

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        while (true)
        {
            var curSegmentSpan = _curSegment.Buffer.AsSpan(_curSegmentWritten);
            if (curSegmentSpan.Length >= buffer.Length)
            {
                buffer.CopyTo(curSegmentSpan);
                _curSegmentWritten += buffer.Length;
                return;
            }

            buffer[..curSegmentSpan.Length].CopyTo(curSegmentSpan);
            buffer = buffer[curSegmentSpan.Length..];

            var newSegment = new SequenceSegment(_segmentLength, _curSegment.RunningIndex + _segmentLength);
            _curSegment.Append(newSegment);
            _curSegment = newSegment;
            _curSegmentWritten = 0;
        }
    }

    public override void WriteByte(byte value)
    {
        Span<byte> buffer = stackalloc byte[1];
        buffer[0] = value;
        Write(buffer);
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _curSegment.RunningIndex + _curSegmentWritten;

    public override long Position
    {
        get => Length;
        set => throw new NotSupportedException();
    }

    private sealed class SequenceSegment : ReadOnlySequenceSegment<byte>
    {
        public readonly byte[] Buffer;

        public SequenceSegment(int segmentLength, long runningIndex)
        {
            Buffer = GC.AllocateUninitializedArray<byte>(segmentLength);
            RunningIndex = runningIndex;
            Memory = Buffer;
        }

        public void Append(SequenceSegment segment) => Next = segment;
    }
}
