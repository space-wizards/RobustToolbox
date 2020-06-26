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

// Uncomment the line below to get statistics in RELEASE builds
//#define USE_RELEASE_STATISTICS

using System;
using System.Text;
using System.Diagnostics;

namespace Lidgren.Network
{
	/// <summary>
	/// Statistics for a NetPeer instance
	/// </summary>
	public sealed class NetPeerStatistics
	{
		private readonly NetPeer m_peer;

		internal int m_sentPackets;
		internal int m_receivedPackets;

		internal int m_sentMessages;
		internal int m_receivedMessages;
		internal int m_receivedFragments;

		internal int m_sentBytes;
		internal int m_receivedBytes;

		internal NetPeerStatistics(NetPeer peer)
		{
			m_peer = peer;
			Reset();
		}

		internal void Reset()
		{
			m_sentPackets = 0;
			m_receivedPackets = 0;

			m_sentMessages = 0;
			m_receivedMessages = 0;
			m_receivedFragments = 0;

			m_sentBytes = 0;
			m_receivedBytes = 0;
		}

		/// <summary>
		/// Gets the number of sent packets since the NetPeer was initialized
		/// </summary>
		public int SentPackets => m_sentPackets;

		/// <summary>
		/// Gets the number of received packets since the NetPeer was initialized
		/// </summary>
		public int ReceivedPackets => m_receivedPackets;

		/// <summary>
		/// Gets the number of sent messages since the NetPeer was initialized
		/// </summary>
		public int SentMessages => m_sentMessages;

		/// <summary>
		/// Gets the number of received messages since the NetPeer was initialized
		/// </summary>
		public int ReceivedMessages => m_receivedMessages;

		/// <summary>
		/// Gets the number of received fragments since the NetPeer was initialized
		/// </summary>
		public int ReceivedFragments => m_receivedFragments;

		/// <summary>
		/// Gets the number of sent bytes since the NetPeer was initialized
		/// </summary>
		public int SentBytes => m_sentBytes;

		/// <summary>
		/// Gets the number of received bytes since the NetPeer was initialized
		/// </summary>
		public int ReceivedBytes => m_receivedBytes;

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void PacketSent(int numBytes, int numMessages)
		{
			m_sentPackets++;
			m_sentBytes += numBytes;
			m_sentMessages += numMessages;
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void PacketReceived(int numBytes, int numMessages, int numFragments)
		{
			m_receivedPackets++;
			m_receivedBytes += numBytes;
			m_receivedMessages += numMessages;
			m_receivedFragments += numFragments;
		}

		/// <summary>
		/// Returns a string that represents this object
		/// </summary>
		public override string ToString()
		{
#if DEBUG || USE_RELEASE_STATISTICS
			return @$"{m_peer.ConnectionsCount} connections
Sent {m_sentBytes} bytes in {m_sentMessages} messages in {m_sentPackets} packets
Received {m_receivedBytes} bytes in {m_receivedMessages} messages (of which {m_receivedFragments} fragments) in {m_receivedPackets} packets";
#else
			return @$"{m_peer.ConnectionsCount} connections
Sent (n/a) bytes in (n/a) messages in (n/a) packets
Received (n/a) bytes in (n/a) messages in (n/a) packets";
#endif
		}
	}
}
