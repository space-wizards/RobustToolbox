using System;
using System.IO;

namespace Lidgren.Network
{

	public partial class NetBuffer
	{

		public class AppenderStream : Stream
		{

			internal AppenderStream(NetBuffer netBuffer)
			{
				Buffer = netBuffer;
			}

			protected NetBuffer Buffer;

			protected override void Dispose(bool _)
				=> Buffer = null;

			protected void DisposedCheck()
			{
				if (Buffer == null) throw new ObjectDisposedException("Stream");
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotSupportedException();
			}

			public override long Length => throw new NotSupportedException();

			public override long Position
			{
				get => throw new NotSupportedException();
				set => throw new NotSupportedException();
			}

			public override void Flush() => DisposedCheck();

			public override int Read(byte[] buffer, int offset, int count)
				=> throw new NotSupportedException();

			public override int Read(Span<byte> buffer)
				=> throw new NotSupportedException();

			public override void SetLength(long value)
				=> throw new NotSupportedException();

			public override void Write(byte[] buffer, int offset, int count)
				=> Write(new ReadOnlySpan<byte>(buffer, offset, count));

			public override void Write(ReadOnlySpan<byte> buffer)
			{
				if (buffer == null)
				{
					throw new ArgumentNullException(nameof(buffer));
				}

				Buffer.Write(buffer);
			}

			public override bool CanRead => false;

			public override bool CanSeek => false;

			public override bool CanWrite => false;

		}

		public abstract class WrapperStream : Stream, IDisposable
		{

			protected WrapperStream(NetBuffer netBuffer, in int start, int length, bool isReadMode)
			{
				NetException.Assert(netBuffer.m_bitLength - start >= length,
					isReadMode ? c_readOverflowError : c_writeOverflowError);

				Buffer = netBuffer;
				BitOffsetStart = start;
				BitOffset = start;
				BitOffsetEnd = start + length;
			}

			protected NetBuffer Buffer;

			protected int BitOffsetStart;

			protected int BitOffsetEnd;

			protected int BitOffset;

			protected override void Dispose(bool _)
				=> Buffer = null;

			protected void DisposedCheck()
			{
				if (Buffer == null) throw new ObjectDisposedException("Stream");
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				DisposedCheck();
				switch (origin)
				{
					case SeekOrigin.Begin: break;
					case SeekOrigin.Current:
						offset += Position;
						break;
					case SeekOrigin.End:
						offset = Position - offset;
						break;
					default: throw new ArgumentOutOfRangeException(nameof(origin), origin, "SeekOrigin invalid.");
				}

				return Position = offset;
			}

			public override long Length => (BitOffsetStart - BitOffsetEnd) >> 3;

			public override long Position
			{
				get => (BitOffset - BitOffsetStart) >> 3;
				set => BitOffset = BitOffsetStart + (checked((int)value) << 3);
			}

		}

		public class ReadOnlyWrapperStream : WrapperStream
		{

			internal ReadOnlyWrapperStream(NetBuffer netBuffer, int start, int length)
				: base(netBuffer, start, length, true)
			{
			}

			public override void Flush() => DisposedCheck();

			public override int Read(byte[] buffer, int offset, int count)
				=> Read(new Span<byte>(buffer, offset, count));

			public override int Read(Span<byte> buffer)
			{
				DisposedCheck();
				var numberOfBytes = buffer.Length;
				var numberOfBits = numberOfBytes * 8;
				NetException.Assert(BitOffsetEnd - BitOffset >= numberOfBits, c_readOverflowError);
				NetBitWriter.ReadBytes(Buffer.m_data, numberOfBytes, BitOffset, buffer, 0);
				BitOffset += numberOfBits;
				return buffer.Length;
			}

			public override void SetLength(long value)
				=> throw new NotSupportedException();

			public override void Write(byte[] buffer, int offset, int count)
				=> throw new NotSupportedException();

			public override bool CanRead => BitOffset < BitOffsetEnd;

			public override bool CanSeek => true;

			public override bool CanWrite => false;

		}
		

		public class WriteOnlyWrapperStream : WrapperStream
		{

			internal WriteOnlyWrapperStream(NetBuffer netBuffer, int start, int length)
				: base(netBuffer, start, length, false)
			{
			}

			public override void Flush() => DisposedCheck();

			public override int Read(byte[] buffer, int offset, int count)
				=> throw new NotSupportedException();

			public override int Read(Span<byte> buffer)
				=> throw new NotSupportedException();

			public override void SetLength(long value)
				=> throw new NotSupportedException();

			public override void Write(byte[] buffer, int offset, int count)
				=> Write(new ReadOnlySpan<byte>(buffer, offset, count));

			public override void Write(ReadOnlySpan<byte> buffer)
			{
				if (buffer == null)
				{
					throw new ArgumentNullException(nameof(buffer));
				}

				var numberOfBytes = buffer.Length;
				var numberOfBits = numberOfBytes * 8;
				NetException.Assert(BitOffsetEnd - BitOffset >= numberOfBits, c_writeOverflowError);
				NetBitWriter.WriteBytes(buffer, 0, numberOfBytes, Buffer.m_data, BitOffset);
				BitOffset += numberOfBits;
			}

			public override bool CanRead => false;

			public override bool CanSeek => true;

			public override bool CanWrite => BitOffset < BitOffsetEnd;

		}

	}

}
