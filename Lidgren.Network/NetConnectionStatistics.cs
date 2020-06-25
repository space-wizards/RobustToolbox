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
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Lidgren.Network
{
	internal enum MessageResendReason
	{
		Delay,
		HoleInSequence
	}

	/// <summary>
	/// Statistics for a NetConnection instance
	/// </summary>
	public sealed class NetConnectionStatistics
	{
		private readonly NetConnection m_connection;

		internal long m_sentPackets;
		internal long m_receivedPackets;

		internal long m_sentMessages;
		internal long m_receivedMessages;
		internal long m_droppedMessages;
		internal long m_receivedFragments;

		internal long m_sentBytes;
		internal long m_receivedBytes;

		internal long m_resentMessagesDueToDelay;
		internal long m_resentMessagesDueToHole;

		internal NetConnectionStatistics(NetConnection conn)
		{
			m_connection = conn;
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
			m_resentMessagesDueToDelay = 0;
			m_resentMessagesDueToHole = 0;
		}

		/// <summary>
		/// Gets the number of sent packets for this connection
		/// </summary>
		public long SentPackets => m_sentPackets;

		/// <summary>
		/// Gets the number of received packets for this connection
		/// </summary>
		public long ReceivedPackets => m_receivedPackets;

		/// <summary>
		/// Gets the number of sent bytes for this connection
		/// </summary>
		public long SentBytes => m_sentBytes;

		/// <summary>
		/// Gets the number of received bytes for this connection
		/// </summary>
		public long ReceivedBytes => m_receivedBytes;

		/// <summary>
        /// Gets the number of sent messages for this connection
        /// </summary>
        public long SentMessages => m_sentMessages;

		/// <summary>
        /// Gets the number of received messages for this connection
        /// </summary>
        public long ReceivedMessages => m_receivedMessages;

		/// <summary>
		/// Gets the number of resent reliable messages for this connection
		/// </summary>
		public long ResentMessages => m_resentMessagesDueToHole + m_resentMessagesDueToDelay;

		/// <summary>
		/// Gets the number of resent reliable messages for this connection due to holes
		/// </summary>
		public long ResentMessagesDueToHoles => m_resentMessagesDueToHole;

		/// <summary>
		/// Gets the number of resent reliable messages for this connection due to delays
		/// </summary>
		public long ResentMessagesDueToDelays => m_resentMessagesDueToDelay;

		/// <summary>
        /// Gets the number of dropped messages for this connection
        /// </summary>
        public long DroppedMessages => m_droppedMessages;

		
		/// <summary>
		/// Gets the number of dropped messages for this connection
		/// </summary>
		public long ReceivedFragments => m_receivedFragments;
		
		// public double LastSendRespondedTo { get { return m_connection.m_lastSendRespondedTo; } }

		/// <summary>
		/// Gets the number of withheld messages for this connection
		/// </summary>
		private int WithheldMessages
		{
			get
			{
				int numWithheld = 0;
				foreach (NetReceiverChannelBase recChan in m_connection.m_receiveChannels)
				{
					var relRecChan = recChan as NetReliableOrderedReceiver;
					if (relRecChan == null)
					{
						continue;
					}

					for (int i = 0; i < relRecChan.m_withheldMessages.Length; i++)
						if (relRecChan.m_withheldMessages[i] != null)
							numWithheld++;
				}

				return numWithheld;
			}
		}

		/// <summary>
		/// Gets the number of unsent and stored messages for this connection
		/// </summary>
		internal void GetUnsentAndStoredMessages(out int numUnsent, out int numStored)
		{
			numUnsent = 0;
			numStored = 0;
			foreach (NetSenderChannelBase sendChan in m_connection.m_sendChannels)
			{
				if (sendChan == null)
					continue;

				numUnsent += sendChan.QueuedSendsCount;

				var relSendChan = sendChan as NetReliableSenderChannel;
				if (relSendChan == null)
				{
					continue;
				}

				for (int i = 0; i < relSendChan.m_storedMessages.Length; i++)
					if (relSendChan.m_storedMessages[i].Message != null)
						numStored++;
			}
		}


#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void PacketSent(int numBytes, int numMessages)
		{
			NetException.Assert(numBytes > 0 && numMessages > 0);
			m_sentPackets++;
			m_sentBytes += numBytes;
			m_sentMessages += numMessages;
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void PacketReceived(int numBytes, int numMessages, int numFragments)
		{
			NetException.Assert(numBytes > 0 && numMessages > 0);
			m_receivedPackets++;
			m_receivedBytes += numBytes;
			m_receivedMessages += numMessages;
			m_receivedFragments += numFragments;
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void MessageResent(MessageResendReason reason)
		{
			m_connection.m_peer.Statistics.MessageResent(reason);
			if (reason == MessageResendReason.Delay)
				m_resentMessagesDueToDelay++;
			else
				m_resentMessagesDueToHole++;
		}

#if !USE_RELEASE_STATISTICS
		[Conditional("DEBUG")]
#endif
		internal void MessageDropped()
		{
			m_connection.m_peer.Statistics.MessageDropped();
			m_droppedMessages++;
		}


		/// <summary>
		/// Returns a string that represents this object
		/// </summary>
		public override string ToString()
		{
			GetUnsentAndStoredMessages(out var numUnsent, out var numStored);

			var numWithheld = WithheldMessages;

			return @$"Current MTU: {m_connection.m_currentMTU}
Sent {m_sentBytes} bytes in {m_sentMessages} messages in {m_sentPackets} packets
Received {m_receivedBytes} bytes in {m_receivedMessages} messages (of which {m_receivedFragments} fragments) in {m_receivedPackets} packets
Dropped {m_droppedMessages} messages (dupes/late/early)
Resent messages (delay): {m_resentMessagesDueToDelay}
Resent messages (holes): {m_resentMessagesDueToHole}
Unsent messages: {numUnsent}
Stored messages: {numStored}
Withheld messages: {numWithheld}";
		}

	}
}