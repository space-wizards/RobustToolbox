using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System.Net;
using System.Threading;

#if !__NOIPENDPOINT__
using NetEndPoint = System.Net.IPEndPoint;
#endif

namespace Lidgren.Network
{
	/// <summary>
	/// Base class for NetIncomingMessage and NetOutgoingMessage
	/// </summary>
	public partial class NetBuffer
	{
		private const string c_readOverflowError = "Trying to read past the buffer size - likely caused by mismatching Write/Reads, different size or order.";
		private const string c_writeOverflowError = "Trying to write past the end of a constrained buffer - likely caused by mismatching Write/Reads, different size or order.";

		/// <summary>
		/// Reads a boolean value (stored as a single bit) written using Write(bool)
		/// </summary>
		public bool ReadBoolean()
		{
			var retval = PeekBoolean();
			m_readPosition += 1;
			return retval;
		}
		
		/// <summary>
		/// Reads a byte
		/// </summary>
		public byte ReadByte()
		{
			var retval = PeekByte();
			m_readPosition += 8;
			return retval;
		}

		/// <summary>
		/// Reads a byte and returns true or false for success
		/// </summary>
		public bool ReadByte(out byte result)
		{
			if (m_bitLength - m_readPosition < 8)
			{
				result = 0;
				return false;
			}

			result = PeekByte();
			m_readPosition += 8;
			return true;
		}

		/// <summary>
		/// Reads a signed byte
		/// </summary>
		[CLSCompliant(false)]
		public sbyte ReadSByte()
		{
			sbyte retval = PeekSByte();
			m_readPosition += 8;
			return retval;
		}

		/// <summary>
		/// Reads 1 to 8 bits into a byte
		/// </summary>
		public byte ReadByte(int numberOfBits)
		{
			byte retval = PeekByte(numberOfBits);
			m_readPosition += numberOfBits;
			return retval;
		}

		public Stream ReadAsStream(int byteLength)
		{
			var bitLength = byteLength*8;
			var stream = new ReadOnlyStreamWrapper(this, m_readPosition, bitLength);
			m_readPosition += bitLength;
			return stream;
		}

		public Span<byte> ReadBytes(Span<byte> into)
		{
			var bytes = PeekBytes(into);
			m_readPosition += 8 * into.Length;
			return bytes;
		}

		/// <summary>
		/// Reads the specified number of bytes into a preallocated array
		/// </summary>
		/// <param name="into">The destination array</param>
		/// <param name="offset">The offset where to start writing in the destination array</param>
		/// <param name="numberOfBytes">The number of bytes to read</param>
		[Obsolete("Use Span alternative instead with slicing.")]
		public Span<byte> ReadBytes(Span<byte> into, int offset, int numberOfBytes)
		{
			var bytes = PeekBytes(into, offset, numberOfBytes);
			m_readPosition += 8 * numberOfBytes;
			return bytes;
		}

		/// <summary>
		/// Reads the specified number of bits into a preallocated array
		/// </summary>
		/// <param name="into">The destination array</param>
		/// <param name="offset">The offset where to start writing in the destination array</param>
		/// <param name="numberOfBits">The number of bits to read</param>
		[Obsolete("Use Span alternative instead with slicing.")]
		public Span<byte> ReadBits(Span<byte> into, int offset, int numberOfBits)
		{
			NetException.Assert(m_bitLength - m_readPosition >= numberOfBits, c_readOverflowError);
			NetException.Assert(offset + NetUtility.BytesToHoldBits(numberOfBits) <= into.Length);

			int numberOfWholeBytes = numberOfBits / 8;
			int extraBits = numberOfBits - (numberOfWholeBytes * 8);

			NetBitWriter.ReadBytes(m_data, numberOfWholeBytes, m_readPosition, into, offset);
			m_readPosition += (8 * numberOfWholeBytes);

			if (extraBits > 0)
				into[offset + numberOfWholeBytes] = ReadByte(extraBits);

			return into.Slice(offset,numberOfBits >> 3);
		}

		/// <summary>
		/// Reads a 16 bit signed integer written using Write(Int16)
		/// </summary>
		public Int16 ReadInt16()
		{
			var retval = PeekInt16();
			m_readPosition += 16;
			return retval;
		}

		/// <summary>
		/// Reads a 16 bit unsigned integer written using Write(UInt16)
		/// </summary>
		[CLSCompliant(false)]
		public UInt16 ReadUInt16()
		{
			var retval = PeekUInt16();
			m_readPosition += 16;
			return (ushort)retval;
		}

		/// <summary>
		/// Reads a 32 bit signed integer written using Write(Int32)
		/// </summary>
		public Int32 ReadInt32()
		{
			var retval = PeekInt32();
			m_readPosition += 32;
			return (Int32)retval;
		}

		/// <summary>
		/// Reads a 32 bit signed integer written using Write(Int32)
		/// </summary>
		[CLSCompliant(false)]
		public bool ReadInt32(out Int32 result)
		{
			if (m_bitLength - m_readPosition < 32)
			{
				result = 0;
				return false;
			}

			result = PeekInt32();
			m_readPosition += 32;
			return true;
		}

		/// <summary>
		/// Reads a signed integer stored in 1 to 32 bits, written using Write(Int32, Int32)
		/// </summary>
		public Int32 ReadInt32(int numberOfBits)
		{
			var retval = PeekInt32(numberOfBits);
			m_readPosition += numberOfBits;
			return retval;
		}

		/// <summary>
		/// Reads an 32 bit unsigned integer written using Write(UInt32)
		/// </summary>
		[CLSCompliant(false)]
		public UInt32 ReadUInt32()
		{
			var retval = PeekUInt32();
			m_readPosition += 32;
			return retval;
		}

		/// <summary>
		/// Reads an 32 bit unsigned integer written using Write(UInt32) and returns true for success
		/// </summary>
		[CLSCompliant(false)]
		public bool ReadUInt32(out UInt32 result)
		{
			if (m_bitLength - m_readPosition < 32)
			{
				result = 0;
				return false;
			}
			result = PeekUInt32();
			m_readPosition += 32;
			return true;
		}

		/// <summary>
		/// Reads an unsigned integer stored in 1 to 32 bits, written using Write(UInt32, Int32)
		/// </summary>
		[CLSCompliant(false)]
		public UInt32 ReadUInt32(int numberOfBits)
		{
			var retval = PeekUInt32(numberOfBits);
			m_readPosition += numberOfBits;
			return retval;
		}

		/// <summary>
		/// Reads a 64 bit unsigned integer written using Write(UInt64)
		/// </summary>
		[CLSCompliant(false)]
		public UInt64 ReadUInt64()
		{
			var retval = PeekUInt64();
			m_readPosition += 64;
			return retval;
		}

		/// <summary>
		/// Reads a 64 bit signed integer written using Write(Int64)
		/// </summary>
		public Int64 ReadInt64()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 64, c_readOverflowError);
			unchecked
			{
				ulong retval = ReadUInt64();
				long longRetval = (long)retval;
				return longRetval;
			}
		}

		/// <summary>
		/// Reads an unsigned integer stored in 1 to 64 bits, written using Write(UInt64, Int32)
		/// </summary>
		[CLSCompliant(false)]
		public UInt64 ReadUInt64(int numberOfBits)
		{
			var retval = PeekUInt64(numberOfBits);
			m_readPosition += numberOfBits;
			return retval;
		}

		/// <summary>
		/// Reads a signed integer stored in 1 to 64 bits, written using Write(Int64, Int32)
		/// </summary>
		public Int64 ReadInt64(int numberOfBits)
		{
			NetException.Assert(((numberOfBits > 0) && (numberOfBits <= 64)), "ReadInt64(bits) can only read between 1 and 64 bits");
			return (long)ReadUInt64(numberOfBits);
		}

		/// <summary>
		/// Reads a 32 bit floating point value written using Write(Single)
		/// </summary>
		public float ReadFloat()
		{
			return ReadSingle();
		}

		/// <summary>
		/// Reads a 32 bit floating point value written using Write(Single)
		/// </summary>
		public float ReadSingle()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 32, c_readOverflowError);

			var retval = PeekSingle();
			m_readPosition += 32;
			return retval;
		}

		/// <summary>
		/// Reads a 32 bit floating point value written using Write(Single)
		/// </summary>
		public bool ReadSingle(out float result)
		{
			if (m_bitLength - m_readPosition < 32)
			{
				result = 0.0f;
				return false;
			}

			result = ReadSingle();
			return true;
		}

		/// <summary>
		/// Reads a 64 bit floating point value written using Write(Double)
		/// </summary>
		public double ReadDouble()
		{
			NetException.Assert(m_bitLength - m_readPosition >= 64, c_readOverflowError);

			var res = PeekDouble();
			m_readPosition += 64;
			return res;
		}

		//
		// Variable bit count
		//

		/// <summary>
		/// Reads a variable sized UInt32 written using WriteVariableUInt32()
		/// </summary>
		[CLSCompliant(false)]
		public uint ReadVariableUInt32()
		{
			int num1 = 0;
			int num2 = 0;
			while (m_bitLength - m_readPosition >= 8)
			{
				byte num3 = this.ReadByte();
				num1 |= (num3 & 0x7f) << num2;
				num2 += 7;
				if ((num3 & 0x80) == 0)
					return (uint)num1;
			}

			// ouch; failed to find enough bytes; malformed variable length number?
			return (uint)num1;
		}

		/// <summary>
		/// Reads a variable sized UInt32 written using WriteVariableUInt32() and returns true for success
		/// </summary>
		[CLSCompliant(false)]
		public bool ReadVariableUInt32(out uint result)
		{
			int num1 = 0;
			int num2 = 0;
			while (m_bitLength - m_readPosition >= 8)
			{
				byte num3;
				if (ReadByte(out num3) == false)
				{
					result = 0;
					return false;
				}
				num1 |= (num3 & 0x7f) << num2;
				num2 += 7;
				if ((num3 & 0x80) == 0)
				{
					result = (uint)num1;
					return true;
				}
			}
			result = (uint)num1;
			return false;
		}

		/// <summary>
		/// Reads a variable sized Int32 written using WriteVariableInt32()
		/// </summary>
		public int ReadVariableInt32()
		{
			uint n = ReadVariableUInt32();
			return (int)(n >> 1) ^ -(int)(n & 1); // decode zigzag
		}

		/// <summary>
		/// Reads a variable sized Int64 written using WriteVariableInt64()
		/// </summary>
		public Int64 ReadVariableInt64()
		{
			UInt64 n = ReadVariableUInt64();
			return (Int64)(n >> 1) ^ -(long)(n & 1); // decode zigzag
		}

		/// <summary>
		/// Reads a variable sized UInt32 written using WriteVariableInt64()
		/// </summary>
		[CLSCompliant(false)]
		public UInt64 ReadVariableUInt64()
		{
			UInt64 num1 = 0;
			int num2 = 0;
			while (m_bitLength - m_readPosition >= 8)
			{
				//if (num2 == 0x23)
				//	throw new FormatException("Bad 7-bit encoded integer");

				byte num3 = this.ReadByte();
				num1 |= ((UInt64)num3 & 0x7f) << num2;
				num2 += 7;
				if ((num3 & 0x80) == 0)
					return num1;
			}

			// ouch; failed to find enough bytes; malformed variable length number?
			return num1;
		}

		/// <summary>
		/// Reads a 32 bit floating point value written using WriteSignedSingle()
		/// </summary>
		/// <param name="numberOfBits">The number of bits used when writing the value</param>
		/// <returns>A floating point value larger or equal to -1 and smaller or equal to 1</returns>
		public float ReadSignedSingle(int numberOfBits)
		{
			uint encodedVal = ReadUInt32(numberOfBits);
			int maxVal = (1 << numberOfBits) - 1;
			return ((float)(encodedVal + 1) / (float)(maxVal + 1) - 0.5f) * 2.0f;
		}

		/// <summary>
		/// Reads a 32 bit floating point value written using WriteUnitSingle()
		/// </summary>
		/// <param name="numberOfBits">The number of bits used when writing the value</param>
		/// <returns>A floating point value larger or equal to 0 and smaller or equal to 1</returns>
		public float ReadUnitSingle(int numberOfBits)
		{
			uint encodedVal = ReadUInt32(numberOfBits);
			int maxVal = (1 << numberOfBits) - 1;
			return (float)(encodedVal + 1) / (float)(maxVal + 1);
		}

		/// <summary>
		/// Reads a 32 bit floating point value written using WriteRangedSingle()
		/// </summary>
		/// <param name="min">The minimum value used when writing the value</param>
		/// <param name="max">The maximum value used when writing the value</param>
		/// <param name="numberOfBits">The number of bits used when writing the value</param>
		/// <returns>A floating point value larger or equal to MIN and smaller or equal to MAX</returns>
		public float ReadRangedSingle(float min, float max, int numberOfBits)
		{
			float range = max - min;
			int maxVal = (1 << numberOfBits) - 1;
			float encodedVal = (float)ReadUInt32(numberOfBits);
			float unit = encodedVal / (float)maxVal;
			return min + (unit * range);
		}

		/// <summary>
		/// Reads a 32 bit integer value written using WriteRangedInteger()
		/// </summary>
		/// <param name="min">The minimum value used when writing the value</param>
		/// <param name="max">The maximum value used when writing the value</param>
		/// <returns>A signed integer value larger or equal to MIN and smaller or equal to MAX</returns>
		public int ReadRangedInteger(int min, int max)
		{
			uint range = (uint)(max - min);
			int numBits = NetUtility.BitsToHoldUInt(range);

			uint rvalue = ReadUInt32(numBits);
			return (int)(min + rvalue);
		}

	        /// <summary>
	        /// Reads a 64 bit integer value written using WriteRangedInteger() (64 version)
	        /// </summary>
	        /// <param name="min">The minimum value used when writing the value</param>
	        /// <param name="max">The maximum value used when writing the value</param>
	        /// <returns>A signed integer value larger or equal to MIN and smaller or equal to MAX</returns>
	        public long ReadRangedInteger(long min, long max)
	        {
	            ulong range = (ulong)(max - min);
	            int numBits = NetUtility.BitsToHoldUInt64(range);
	
	            ulong rvalue = ReadUInt64(numBits);
	            return min + (long)rvalue;
	        }

		/// <summary>
		/// Reads a string written using Write(string)
		/// </summary>
		public string ReadString()
		{
			int byteLen = (int)ReadVariableUInt32();

			if (byteLen <= 0)
				return String.Empty;

			if ((ulong)(m_bitLength - m_readPosition) < ((ulong)byteLen * 8))
			{
				// not enough data
#if DEBUG
				
				throw new NetException(c_readOverflowError);
#else
				m_readPosition = m_bitLength;
				return null; // unfortunate; but we need to protect against DDOS
#endif
			}

			if ((m_readPosition & 7) == 0)
			{
				// read directly
				string retval = Encoding.UTF8.GetString(m_data.Slice( m_readPosition >> 3, byteLen));
				m_readPosition += (8 * byteLen);
				return retval;
			}

			var bytes = ReadBytes(stackalloc byte[byteLen]);
			return Encoding.UTF8.GetString(bytes);
		}

		/// <summary>
		/// Reads a string written using Write(string) and returns true for success
		/// </summary>
		public bool ReadString(out string result)
		{
			uint byteLen;
			if (ReadVariableUInt32(out byteLen) == false)
			{
				result = String.Empty;
				return false;
			}

			if (byteLen <= 0)
			{
				result = String.Empty;
				return true;
			}

			if (m_bitLength - m_readPosition < (byteLen * 8))
			{
				result = String.Empty;
				return false;
			}

			if ((m_readPosition & 7) == 0)
			{
				// read directly
				result = Encoding.UTF8.GetString(m_data.Slice(m_readPosition >> 3, (int)byteLen));
				m_readPosition += (8 * (int)byteLen);
				return true;
			}

			var bytes = ReadBytes(stackalloc byte[(int)byteLen]);
			result = Encoding.UTF8.GetString(bytes);
			return true;
		}

		/// <summary>
		/// Reads a value, in local time comparable to NetTime.Now, written using WriteTime() for the connection supplied
		/// </summary>
		public double ReadTime(NetConnection connection, bool highPrecision)
		{
			double remoteTime = (highPrecision ? ReadDouble() : (double)ReadSingle());

			if (connection == null)
				throw new NetException("Cannot call ReadTime() on message without a connected sender (ie. unconnected messages)");

			// lets bypass NetConnection.GetLocalTime for speed
			return remoteTime - connection.m_remoteTimeOffset;
		}

		/// <summary>
		/// Reads a stored IPv4 endpoint description
		/// </summary>
		public NetEndPoint ReadIPEndPoint()
		{
			byte len = ReadByte();
			var addressBytes = ReadBytes(stackalloc byte[len]);
			int port = (int)ReadUInt16();

			var address = NetUtility.CreateAddressFromBytes(addressBytes);
			return new NetEndPoint(address, port);
		}

		/// <summary>
		/// Pads data with enough bits to reach a full byte. Decreases cpu usage for subsequent byte writes.
		/// </summary>
		public void SkipPadBits()
		{
			m_readPosition = ((m_readPosition + 7) >> 3) * 8;
		}

		/// <summary>
		/// Pads data with enough bits to reach a full byte. Decreases cpu usage for subsequent byte writes.
		/// </summary>
		public void ReadPadBits()
		{
			m_readPosition = ((m_readPosition + 7) >> 3) * 8;
		}

		/// <summary>
		/// Pads data with the specified number of bits.
		/// </summary>
		public void SkipPadBits(int numberOfBits)
		{
			m_readPosition += numberOfBits;
		}
	}
}
