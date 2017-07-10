using System.Collections.Generic;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network
{
    /// <summary>
    ///     Contains a networked mapping of IDs -> Strings.
    /// </summary>
    public class StringTable
    {
        private INetManager _network;
        private readonly Dictionary<int, string> _strings;
        private int _lastStringIndex;

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public StringTable()
        {
            _strings = new Dictionary<int, string>();
        }

        /// <summary>
        /// The ID of an invalid string.
        /// </summary>
        public static int InvalidStringId => -1;

        /// <summary>
        /// Initializes the string table.
        /// </summary>
        public void Initialize(INetManager network)
        {
            _network = network;
            _network.RegisterNetMessage<MsgStringTableEntry>(MsgStringTableEntry.NAME, (int)MsgStringTableEntry.ID, message =>
            {
                if (_network.IsServer) // Server does not receive entries from clients.
                    return;

                var entry = (MsgStringTableEntry)message;
                var id = entry.EntryId;
                var str = string.IsNullOrEmpty(entry.EntryString) ? null : entry.EntryString;

                if (str == null)
                {
                    _strings.Remove(id);
                }
                else
                {
                    if (!_strings.ContainsKey(id))
                        _strings.Add(id, str);
                    else
                        _strings[id] = str;
                }
            });
        }

        /// <summary>
        ///     Adds a string to the table. The ID is generated automatically.
        /// </summary>
        /// <param name="str">The string to add.</param>
        /// <returns>The ID of the added string.</returns>
        public int AddString(string str)
        {
            // The client should receive the table from the server, not add their own.
            if (_network.IsClient)
                return -1;

            if (TryFindStringId(str, out int oldId))
                return oldId; // no point in storing dupe strings

            do // find next available key
            {
                // the indexer always moves forward, so if a key is deleted the ID is never re-filled.
                _lastStringIndex++;

                if (_strings.ContainsKey(_lastStringIndex))
                    continue;

                _strings.Add(_lastStringIndex, str);
                BroadcastTableUpdate(_lastStringIndex, str);
                return _lastStringIndex;
            } while (true);
        }

        /// <summary>
        ///     Adds a string with the given ID. If the string already exists with another ID,
        ///     the existing string will be deleted.
        ///     NOTE: You should be using AddString(), unless you know what you are doing, and
        ///     know how this method can break things.
        /// </summary>
        /// <param name="id">The ID the string has to use.</param>
        /// <param name="str">The string to add.</param>
        /// <returns>The ID of the added string.</returns>
        public void AddStringFixed(int id, string str)
        {
            // The client should receive the table from the server, not add their own.
            if (_network.IsClient)
                return;

            // remove existing string, if any
            if (TryFindStringId(str, out int oldId))
                if (oldId != id)
                    _strings.Remove(oldId);
                else
                    return; // same string, no need to do anything.

            _strings.Add(id, str);
            BroadcastTableUpdate(id, str);
        }

        /// <summary>
        ///     Gets the string with the given ID.
        /// </summary>
        /// <param name="id">THe ID of the string to get.</param>
        /// <returns>The string with the given ID, or null.</returns>
        public string GetString(int id)
        {
            return _strings.TryGetValue(id, out string str) ? str : null;
        }

        /// <summary>
        ///     Tries to get the string with the given ID.
        /// </summary>
        /// <param name="id">The ID of the string.</param>
        /// <param name="str">The string with the ID.</param>
        /// <returns>True if the table contains the ID, false if it does not.</returns>
        public bool TryGetString(int id, out string str)
        {
            return _strings.TryGetValue(id, out str);
        }

        /// <summary>
        ///     Tries to find the ID of the given string.
        /// </summary>
        /// <param name="str">The string to find.</param>
        /// <param name="id">The found ID of the string.</param>
        /// <returns>True if the table contains the string, false if it does not.</returns>
        public bool TryFindStringId(string str, out int id)
        {
            // AddString needs to guarantee there are no duplicate strings.
            foreach (var kvs in _strings)
            {
                if (kvs.Value != str)
                    continue;

                id = kvs.Key;
                return true;
            }
            id = 0;
            return false;
        }

        private void BroadcastTableUpdate(int id, string str)
        {
            if (_network.IsClient)
                return;

            var message = _network.CreateNetMessage<MsgStringTableEntry>();

            message.EntryId = id;
            message.EntryString = str;

            _network.ServerSendToAll(message);
        }

        /// <summary>
        ///     Sends the full table to a channel.
        /// </summary>
        /// <param name="channel">The channel that will receive the table.</param>
        public void SendFullTable(INetChannel channel)
        {
            if (_network.IsClient)
                return;

            var message = _network.CreateNetMessage<MsgStringTableEntry>();

            foreach (var kvEntries in _strings)
            {
                message.EntryId = kvEntries.Key;
                message.EntryString = kvEntries.Value;

                _network.ServerSendMessage(message, channel);
            }
        }
    }

    /// <summary>
    /// A net message for transmitting a string table entry to clients.
    /// </summary>
    public class MsgStringTableEntry : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.StringTableEntry;
        public static readonly MsgGroups GROUP = MsgGroups.String;

        public static readonly string NAME = ID.ToString();
        public MsgStringTableEntry(INetChannel channel) : base(NAME, GROUP, ID) { }
        #endregion

        /// <summary>
        /// The string contained inside of the message.
        /// </summary>
        public string EntryString { get; set; }

        /// <summary>
        /// The ID of the string inside of the message.
        /// </summary>
        public int EntryId { get; set; }

        /// <inheritdoc />
        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            EntryId = buffer.ReadVariableInt32();
            EntryString = buffer.ReadString();
        }

        /// <inheritdoc />
        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.WriteVariableInt32(EntryId);
            buffer.Write(EntryString);
        }
    }
}
