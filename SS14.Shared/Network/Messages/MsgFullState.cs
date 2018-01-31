using ICSharpCode.SharpZipLib.GZip;
using Lidgren.Network;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using System.IO;

namespace SS14.Shared.Network.Messages
{
    public class MsgFullState : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Entity;
        public static readonly string NAME = nameof(MsgFullState);
        public MsgFullState(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public GameState State { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            int length = buffer.ReadInt32();
            byte[] stateData = Decompress(buffer.ReadBytes(length));
            using (var stateStream = new MemoryStream(stateData))
            {
                var serializer = IoCManager.Resolve<ISS14Serializer>();
                State = serializer.Deserialize<GameState>(stateStream);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            byte[] stateData = Compress(State.GetSerializedDataBuffer());

            buffer.Write(stateData.Length);
            buffer.Write(stateData);
        }

        #region Compression

        /// <summary>
        /// Compresses a decompressed state data byte array into a compressed one.
        /// </summary>
        /// <param name="stateData">full state data</param>
        /// <returns></returns>
        private static byte[] Compress(byte[] stateData)
        {
            using (var compressedDataStream = new MemoryStream())
            {
                using (var gzip = new GZipOutputStream(compressedDataStream))
                {
                    gzip.Write(stateData, 0, stateData.Length);
                }
                return compressedDataStream.ToArray();
            }
        }

        /// <summary>
        /// Decompresses a compressed state data byte array into a decompressed one.
        /// </summary>
        /// <param name="compressedStateData">compressed state data</param>
        /// <returns></returns>
        private static byte[] Decompress(byte[] compressedStateData)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (var compressedStream = new MemoryStream(compressedStateData))
            using (var stream = new GZipInputStream(compressedStream))
            {
                const int size = 2048;
                var buffer = new byte[size];
                using (var memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    } while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        #endregion Compression
    }
}
