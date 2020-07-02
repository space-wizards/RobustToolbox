using System;
using System.Collections.Generic;
using System.Text;

namespace Lidgren.Network
{
	/// <summary>
	/// Example class; not very good encryption
	/// </summary>
	public class NetXorEncryption : NetEncryption
	{
		private Memory<byte> m_key;

		/// <summary>
		/// NetXorEncryption constructor
		/// </summary>
		public NetXorEncryption(NetPeer peer, Memory<byte> key)
			: base(peer)
		{
			m_key = key;
		}

		public override void SetKey(ReadOnlySpan<byte> data, int offset, int count)
		{
			m_key = new byte[count];
			data.CopyTo(m_key.Span);
		}

		/// <summary>
		/// NetXorEncryption constructor
		/// </summary>
		public NetXorEncryption(NetPeer peer, string key)
			: base(peer)
		{
			m_key = Encoding.UTF8.GetBytes(key);
		}

		/// <summary>
		/// Encrypt an outgoing message
		/// </summary>
		public override bool Encrypt(NetOutgoingMessage msg)
		{
			int numBytes = msg.LengthBytes;
			var dataSpan = msg.m_data;
			var keySpan = m_key.Span;
			for (int i = 0; i < numBytes; i++)
			{
				int offset = i % keySpan.Length;
				dataSpan[i] = (byte)(dataSpan[i] ^ keySpan[offset]);
			}
			return true;
		}

		/// <summary>
		/// Decrypt an incoming message
		/// </summary>
		public override bool Decrypt(NetIncomingMessage msg)
		{
			int numBytes = msg.LengthBytes;
			var keySpan = m_key.Span;
			var dataSpan = msg.m_data;
			for (int i = 0; i < numBytes; i++)
			{
				int offset = i % keySpan.Length;
				dataSpan[i] = (byte)(dataSpan[i] ^ keySpan[offset]);
			}
			return true;
		}
	}
}
