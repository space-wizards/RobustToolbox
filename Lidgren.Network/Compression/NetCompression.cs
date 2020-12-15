namespace Lidgren.Network.Compression
{
    public abstract class NetCompression
    {
        /// <summary>
        /// Compress an outgoing message in place
        /// </summary>
        public abstract bool Compress(NetOutgoingMessage msg);

        /// <summary>
        /// Decompress an incoming message in place
        /// </summary>
        public abstract bool Decompress(NetIncomingMessage msg);
    }
}
