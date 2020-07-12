//#define UNSAFE
//#define BIGENDIAN
/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.

*/

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Lidgren.Network
{
	/// <summary>
	/// Helper class for NetBuffer to write/read bits
	/// </summary>
	public static class NetBitWriter
	{
		/// <summary>
		/// Read 1-8 bits from a buffer into a byte
		/// </summary>
		public static byte ReadByte(ReadOnlySpan<byte> fromBuffer, int numberOfBits, int readBitOffset)
		{
			NetException.Assert(((numberOfBits > 0) && (numberOfBits < 9)), "Read() can only read between 1 and 8 bits");

			int bytePtr = readBitOffset >> 3;
			int startReadAtIndex = readBitOffset - (bytePtr * 8); // (readBitOffset % 8);

			if (startReadAtIndex == 0 && numberOfBits == 8)
				return fromBuffer[bytePtr];

			// mask away unused bits lower than (right of) relevant bits in first byte
			byte returnValue = (byte)(fromBuffer[bytePtr] >> startReadAtIndex);

			int numberOfBitsInSecondByte = numberOfBits - (8 - startReadAtIndex);

			if (numberOfBitsInSecondByte < 1)
			{
				// we don't need to read from the second byte, but we DO need
				// to mask away unused bits higher than (left of) relevant bits
				return (byte)(returnValue & (255 >> (8 - numberOfBits)));
			}

			byte second = fromBuffer[bytePtr + 1];

			// mask away unused bits higher than (left of) relevant bits in second byte
			second &= (byte)(255 >> (8 - numberOfBitsInSecondByte));

			return (byte)(returnValue | (byte)(second << (numberOfBits - numberOfBitsInSecondByte)));
		}

		/// <summary>
		/// Read several bytes from a buffer
		/// </summary>
		public static void ReadBytes(ReadOnlySpan<byte> fromBuffer, int numberOfBytes, int readBitOffset, Span<byte> destination, int destinationByteOffset)
		{
			int readPtr = readBitOffset >> 3;
			int startReadAtIndex = readBitOffset - (readPtr * 8); // (readBitOffset % 8);

			if (startReadAtIndex == 0)
			{
				//Buffer.BlockCopy(fromBuffer, readPtr, destination, destinationByteOffset, numberOfBytes);
				fromBuffer.Slice(readPtr,numberOfBytes)
					.CopyTo(destination.Slice(destinationByteOffset, numberOfBytes));
				return;
			}

			int secondPartLen = 8 - startReadAtIndex;
			int secondMask = 255 >> secondPartLen;

			for (int i = 0; i < numberOfBytes; i++)
			{
				// mask away unused bits lower than (right of) relevant bits in byte
				int b = fromBuffer[readPtr] >> startReadAtIndex;

				readPtr++;

				// mask away unused bits higher than (left of) relevant bits in second byte
				int second = fromBuffer[readPtr] & secondMask;

				destination[destinationByteOffset++] = (byte)(b | (second << secondPartLen));
			}
		}

		
		/// <summary>
		/// Read several bytes from a buffer
		/// </summary>
		public static void ReadBytes(ReadOnlySpan<byte> fromBuffer, int readBitOffset, Span<byte> destination)
		{
			var destinationByteOffset = 0;
			var numberOfBytes = (fromBuffer.Length << 3 - readBitOffset) >> 3;
			int readPtr = readBitOffset >> 3;
			int startReadAtIndex = readBitOffset - (readPtr * 8); // (readBitOffset % 8);

			if (startReadAtIndex == 0)
			{
				//Buffer.BlockCopy(fromBuffer, readPtr, destination, destinationByteOffset, numberOfBytes);
				fromBuffer.Slice(readPtr,numberOfBytes)
					.CopyTo(destination.Slice(0, numberOfBytes));
				return;
			}

			int secondPartLen = 8 - startReadAtIndex;
			int secondMask = 255 >> secondPartLen;

			for (int i = 0; i < numberOfBytes; i++)
			{
				// mask away unused bits lower than (right of) relevant bits in byte
				int b = fromBuffer[readPtr] >> startReadAtIndex;

				readPtr++;

				// mask away unused bits higher than (left of) relevant bits in second byte
				int second = fromBuffer[readPtr] & secondMask;

				destination[destinationByteOffset++] = (byte)(b | (second << secondPartLen));
			}
		}

		/// <summary>
		/// Write 0-8 bits of data to buffer
		/// </summary>
		public static void WriteByte(byte source, int numberOfBits, Span<byte> destination, int destBitOffset)
		{
			if (numberOfBits == 0)
				return;

			NetException.Assert(((numberOfBits >= 0) && (numberOfBits <= 8)), "Must write between 0 and 8 bits!");

			// Mask out all the bits we dont want
			source = (byte)(source & (0xFF >> (8 - numberOfBits)));

			int p = destBitOffset >> 3;
			int bitsUsed = destBitOffset & 0x7; // mod 8
			int bitsFree = 8 - bitsUsed;
			int bitsLeft = bitsFree - numberOfBits;

			// Fast path, everything fits in the first byte
			if (bitsLeft >= 0)
			{
				int mask = (0xFF >> bitsFree) | (0xFF << (8 - bitsLeft));

				destination[p] = (byte)(
					// Mask out lower and upper bits
					(destination[p] & mask) |

					// Insert new bits
					(source << bitsUsed)
				);

				return;
			}

			destination[p] = (byte)(
				// Mask out upper bits
				(destination[p] & (0xFF >> bitsFree)) |

				// Write the lower bits to the upper bits in the first byte
				(source << bitsUsed)
			);

			p += 1;

			destination[p] = (byte)(
				// Mask out lower bits
				(destination[p] & (0xFF << (numberOfBits - bitsFree))) |

				// Write the upper bits to the lower bits of the second byte
				(source >> bitsFree)
			);
		}
		
		/// <summary>
		/// Zero a number of bits
		/// </summary>
		public static void Zero(int numberOfBits, Span<byte> destination, int destBitOffset)
		{
			var dstBytePtr = destBitOffset >> 3;
			var firstPartLen = destBitOffset & 7;
			var numberOfBytes = numberOfBits >> 3;
			var endBits = numberOfBits & 7;

			if (firstPartLen == 0)
			{
				destination.Slice(destBitOffset / 8, numberOfBytes).Fill(0);

				if (endBits <= 0)
				{
					return;
				}

				var endByteSpan = destination.Slice(numberOfBytes, 1);

				endByteSpan[0] = (byte) (endByteSpan[0] & ~(byte.MaxValue >> (8 - endBits)));
				
				return;
			}
			
			
			var lastPartLen = 8 - firstPartLen;

			destination[dstBytePtr] &= (byte)(255 >> lastPartLen);
			
			++dstBytePtr;
			
			destination.Slice(dstBytePtr, numberOfBytes-2).Fill(0);

			dstBytePtr = numberOfBytes - 2;

			destination[dstBytePtr] &= (byte)(255 << firstPartLen);
		}

		
		/// <summary>

		/// <summary>
		/// Write several whole bytes
		/// </summary>
		public static void WriteBytes(ReadOnlySpan<byte> source, int sourceByteOffset, int numberOfBytes, Span<byte> destination, int destBitOffset)
		{
			int dstBytePtr = destBitOffset >> 3;
			int firstPartLen = (destBitOffset & 7);

			if (firstPartLen == 0)
			{
				//Buffer.BlockCopy(source, sourceByteOffset, destination, dstBytePtr, numberOfBytes);
				source.Slice(sourceByteOffset,numberOfBytes)
					.CopyTo(destination.Slice(dstBytePtr, numberOfBytes));
				return;
			}

			int lastPartLen = 8 - firstPartLen;

			for (int i = 0; i < numberOfBytes; i++)
			{
				byte src = source[sourceByteOffset + i];

				// write last part of this byte
				destination[dstBytePtr] &= (byte)(255 >> lastPartLen); // clear before writing
				destination[dstBytePtr] |= (byte)(src << firstPartLen); // write first half

				dstBytePtr++;

				// write first part of next byte
				destination[dstBytePtr] &= (byte)(255 << firstPartLen); // clear before writing
				destination[dstBytePtr] |= (byte)(src >> lastPartLen); // write second half
			}
		}

		
		/// <summary>
		/// Write several whole bytes
		/// </summary>
		public static void WriteBytes(ReadOnlySpan<byte> source, Span<byte> destination, int destBitOffset)
		{
			int sourceByteOffset = 0;
			int numberOfBytes = source.Length;
			int dstBytePtr = destBitOffset >> 3;
			int firstPartLen = destBitOffset & 7;

			if (firstPartLen == 0)
			{
				//Buffer.BlockCopy(source, sourceByteOffset, destination, dstBytePtr, numberOfBytes);
				source
					.CopyTo(destination.Slice(dstBytePtr, numberOfBytes));
				return;
			}

			int lastPartLen = 8 - firstPartLen;

			for (int i = 0; i < numberOfBytes; i++)
			{
				byte src = source[sourceByteOffset + i];

				// write last part of this byte
				destination[dstBytePtr] &= (byte)(255 >> lastPartLen); // clear before writing
				destination[dstBytePtr] |= (byte)(src << firstPartLen); // write first half

				dstBytePtr++;

				// write first part of next byte
				destination[dstBytePtr] &= (byte)(255 << firstPartLen); // clear before writing
				destination[dstBytePtr] |= (byte)(src >> lastPartLen); // write second half
			}
		}

		/// <summary>
		/// Reads an unsigned 16 bit integer
		/// </summary>
		[CLSCompliant(false)]
		public static ushort ReadUInt16(ReadOnlySpan<byte> fromBuffer, int numberOfBits, int readBitOffset)
		{
			Debug.Assert(((numberOfBits > 0) && (numberOfBits <= 16)), "ReadUInt16() can only read between 1 and 16 bits");

			if (numberOfBits == 16 && ((readBitOffset & 7) == 0))
			{
				return MemoryMarshal.Read<ushort>(fromBuffer.Slice(readBitOffset / 8));
			}
			ushort returnValue;
			if (numberOfBits <= 8)
			{
				returnValue = ReadByte(fromBuffer, numberOfBits, readBitOffset);
				return returnValue;
			}
			returnValue = ReadByte(fromBuffer, 8, readBitOffset);
			numberOfBits -= 8;
			readBitOffset += 8;

			if (numberOfBits <= 8)
			{
				returnValue |= (ushort)(ReadByte(fromBuffer, numberOfBits, readBitOffset) << 8);
			}

			if (!BitConverter.IsLittleEndian)
			{
				return BinaryPrimitives.ReverseEndianness(returnValue);
			}

			return returnValue;
		}

		/// <summary>
		/// Reads the specified number of bits into an UInt32
		/// </summary>
		[CLSCompliant(false)]
		public static uint ReadUInt32(ReadOnlySpan<byte> fromBuffer, int numberOfBits, int readBitOffset)
		{
			NetException.Assert(((numberOfBits > 0) && (numberOfBits <= 32)), "ReadUInt32() can only read between 1 and 32 bits");

			if (numberOfBits == 32 && ((readBitOffset & 7) == 0))
			{
				return MemoryMarshal.Read<uint>(fromBuffer.Slice(readBitOffset / 8));
			}

			uint returnValue;
			if (numberOfBits <= 8)
			{
				returnValue = ReadByte(fromBuffer, numberOfBits, readBitOffset);
				return returnValue;
			}
			returnValue = ReadByte(fromBuffer, 8, readBitOffset);
			numberOfBits -= 8;
			readBitOffset += 8;

			if (numberOfBits <= 8)
			{
				returnValue |= (uint)(ReadByte(fromBuffer, numberOfBits, readBitOffset) << 8);
				return returnValue;
			}
			returnValue |= (uint)(ReadByte(fromBuffer, 8, readBitOffset) << 8);
			numberOfBits -= 8;
			readBitOffset += 8;

			if (numberOfBits <= 8)
			{
				uint r = ReadByte(fromBuffer, numberOfBits, readBitOffset);
				r <<= 16;
				returnValue |= r;
				return returnValue;
			}
			returnValue |= (uint)(ReadByte(fromBuffer, 8, readBitOffset) << 16);
			numberOfBits -= 8;
			readBitOffset += 8;

			returnValue |= (uint)(ReadByte(fromBuffer, numberOfBits, readBitOffset) << 24);

			if (!BitConverter.IsLittleEndian)
			{
				return BinaryPrimitives.ReverseEndianness(returnValue);
			}

			return returnValue;
		}

		//[CLSCompliant(false)]
		//public static ulong ReadUInt64(byte[] fromBuffer, int numberOfBits, int readBitOffset)

		/// <summary>
		/// Writes an unsigned 16 bit integer
		/// </summary>
		[CLSCompliant(false)]
		public static void WriteUInt16(ushort source, int numberOfBits, Span<byte> destination, int destinationBitOffset)
		{
			if (numberOfBits == 0)
			{
				return;
			}

			NetException.Assert((numberOfBits >= 0 && numberOfBits <= 16), "numberOfBits must be between 0 and 16");
			if (!BitConverter.IsLittleEndian)
			{
				source = BinaryPrimitives.ReverseEndianness(source);
			}
			

			if (numberOfBits <= 8)
			{
				WriteByte((byte)source, numberOfBits, destination, destinationBitOffset);
				return;
			}

			WriteByte((byte)source, 8, destination, destinationBitOffset);

			numberOfBits -= 8;
			
			WriteByte((byte)(source >> 8), numberOfBits, destination, destinationBitOffset + 8);
		}

		/// <summary>
		/// Writes the specified number of bits into a byte array
		/// </summary>
		[CLSCompliant(false)]
		public static int WriteUInt32(uint source, int numberOfBits, Span<byte> destination, int destinationBitOffset)
		{
			if (!BitConverter.IsLittleEndian)
			{
				source = BinaryPrimitives.ReverseEndianness(source);
			}

			int returnValue = destinationBitOffset + numberOfBits;
			if (numberOfBits <= 8)
			{
				WriteByte((byte)source, numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)source, 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			if (numberOfBits <= 8)
			{
				WriteByte((byte)(source >> 8), numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)(source >> 8), 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			if (numberOfBits <= 8)
			{
				WriteByte((byte)(source >> 16), numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)(source >> 16), 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			WriteByte((byte)(source >> 24), numberOfBits, destination, destinationBitOffset);
			return returnValue;
		}

		/// <summary>
		/// Writes the specified number of bits into a byte array
		/// </summary>
		[CLSCompliant(false)]
		public static int WriteUInt64(ulong source, int numberOfBits, Span<byte> destination, int destinationBitOffset)
		{
			if (!BitConverter.IsLittleEndian)
				source = BinaryPrimitives.ReverseEndianness(source);
			

			int returnValue = destinationBitOffset + numberOfBits;
			if (numberOfBits <= 8)
			{
				WriteByte((byte)source, numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)source, 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			if (numberOfBits <= 8)
			{
				WriteByte((byte)(source >> 8), numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)(source >> 8), 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			if (numberOfBits <= 8)
			{
				WriteByte((byte)(source >> 16), numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)(source >> 16), 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			if (numberOfBits <= 8)
			{
				WriteByte((byte)(source >> 24), numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)(source >> 24), 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			if (numberOfBits <= 8)
			{
				WriteByte((byte)(source >> 32), numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)(source >> 32), 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			if (numberOfBits <= 8)
			{
				WriteByte((byte)(source >> 40), numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)(source >> 40), 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			if (numberOfBits <= 8)
			{
				WriteByte((byte)(source >> 48), numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)(source >> 48), 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			if (numberOfBits <= 8)
			{
				WriteByte((byte)(source >> 56), numberOfBits, destination, destinationBitOffset);
				return returnValue;
			}
			
			WriteByte((byte)(source >> 56), 8, destination, destinationBitOffset);
			destinationBitOffset += 8;
			numberOfBits -= 8;

			return returnValue;
		}

		//
		// Variable size
		//

		/// <summary>
		/// Write Base128 encoded variable sized unsigned integer
		/// </summary>
		/// <returns>number of bytes written</returns>
		[CLSCompliant(false)]
		public static int WriteVariableUInt32(Span<byte> intoBuffer, int offset, uint value)
		{
			int retval = 0;
			uint num1 = value;
			while (num1 >= 0x80)
			{
				intoBuffer[offset + retval] = (byte)(num1 | 0x80);
				num1 = num1 >> 7;
				retval++;
			}
			intoBuffer[offset + retval] = (byte)num1;
			return retval + 1;
		}

		/// <summary>
		/// Reads a UInt32 written using WriteUnsignedVarInt(); will increment offset!
		/// </summary>
		[CLSCompliant(false)]
		public static uint ReadVariableUInt32(Span<byte> buffer, ref int offset)
		{
			int num1 = 0;
			int num2 = 0;
			while (true)
			{
				NetException.Assert(num2 != 0x23, "Bad 7-bit encoded integer");

				byte num3 = buffer[offset++];
				num1 |= (num3 & 0x7f) << (num2 & 0x1f);
				num2 += 7;
				if ((num3 & 0x80) == 0)
					return (uint)num1;
			}
		}
	}
}
