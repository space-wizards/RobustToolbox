using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Lidgren.Network;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Network
{
    /// <summary>
    ///     Callback for when the string table gets initialized on the client. This is NOT called on the server.
    /// </summary>
    public delegate void InitCallback();

    /// <summary>
    ///     Callback for when one or more entries in the string table get updated on the client.
    ///     This is NOT called on the server.
    /// </summary>
    /// <param name="entries">The entries that were updated.</param>
    public delegate void StringTableUpdateCallback(MsgStringTableEntries.Entry[] entries);

    /// <summary>
    ///     Contains a networked mapping of IDs -> Strings.
    /// </summary>
    public class StringTable
    {
        /// <summary>
        ///     The ID of the <see cref="MsgStringTableEntries"/> packet.
        ///     This packet must have a fixed ID so the system can bootstrap itself.
        /// </summary>
        private const int StringTablePacketId = 0;

        private bool _initialized = false;
        private readonly INetManager _network;
        private readonly Dictionary<int, string> _strings;
        private int _lastStringIndex;
        private InitCallback? _callback;
        private StringTableUpdateCallback? _updateCallback;

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public StringTable(INetManager network)
        {
            _network = network;
            _strings = new Dictionary<int, string>();
        }

        /// <summary>
        /// The ID of an invalid string.
        /// </summary>
        public static int InvalidStringId => -1;

        /// <summary>
        /// Initializes the string table.
        /// </summary>
        public void Initialize(InitCallback? callback = null,
            StringTableUpdateCallback? updateCallback = null)
        {
            DebugTools.Assert(!_initialized);

            _callback = callback;
            _updateCallback = updateCallback;
            _network.RegisterNetMessage<MsgStringTableEntries>(MsgStringTableEntries.NAME, ReceiveEntries,
                NetMessageAccept.Client);

            Reset();
        }

        private void ReceiveEntries(MsgStringTableEntries message)
        {
            DebugTools.Assert(_network.IsClient);

            Logger.InfoS("net", $"Received message name string table.");

            foreach (var entry in message.Entries)
            {
                var id = entry.Id;
                var str = string.IsNullOrEmpty(entry.String) ? null : entry.String;

                if (str == null)
                {
                    _strings.Remove(id);
                }
                else
                {
                    if (TryFindStringId(str, out int oldId))
                    {
                        if (oldId == id) continue;

                        _strings.Remove(oldId);
                        _strings.Add(id, str);
                    }
                    else
                    {
                        _strings.Add(id, str);
                    }
                }
            }

            if (_callback == null) return;

            if (_network.IsClient && !_initialized) _callback?.Invoke();
            _updateCallback?.Invoke(message.Entries);
        }

        /// <summary>
        ///     Resets the string table to the state right after calling Initialize().
        /// </summary>
        public void Reset()
        {
            _strings.Clear();
            _initialized = false;

            // manually register the id on the client so it can bootstrap itself with incoming table entries
            if (!TryFindStringId(MsgStringTableEntries.NAME, out _))
            {
                _strings.Add(StringTablePacketId, MsgStringTableEntries.NAME);

                if (_network.IsClient)
                {
                    _updateCallback?.Invoke(new [] {
                        new MsgStringTableEntries.Entry
                        {
                            Id = StringTablePacketId,
                            String = MsgStringTableEntries.NAME
                        }
                    });
                }
            }
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
            DebugTools.Assert(_network != null, "You need to call Initialize.");

            // The client should receive the table from the server, not add their own.
            if (_network!.IsClient)
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
        public string? GetString(int id)
        {
            return _strings.TryGetValue(id, out var str) ? str : null;
        }

        /// <summary>
        ///     Tries to get the string with the given ID.
        /// </summary>
        /// <param name="id">The ID of the string.</param>
        /// <param name="str">The string with the ID.</param>
        /// <returns>True if the table contains the ID, false if it does not.</returns>
        public bool TryGetString(int id, [NotNullWhen(true)] out string? str)
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

            if (!_network.IsRunning)
                return;

            var message = _network.CreateNetMessage<MsgStringTableEntries>();

            message.Entries = new MsgStringTableEntries.Entry[1];
            message.Entries[0].Id = id;
            message.Entries[0].String = str;

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

            var message = _network.CreateNetMessage<MsgStringTableEntries>();

            var count = _strings.Count;
            message.Entries = new MsgStringTableEntries.Entry[count];

            var i = 0;
            foreach (var kvEntries in _strings)
            {
                message.Entries[i].Id = kvEntries.Key;
                message.Entries[i].String = kvEntries.Value;
                i++;

            }

            Logger.InfoS("net",$"Sending message name string table to {channel.RemoteEndPoint.Address}.");
            _network.ServerSendMessage(message, channel);
        }
    }

    /// <summary>
    /// A net message for transmitting a string table entry to clients.
    /// </summary>
    public class MsgStringTableEntries : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.String;
        public static readonly string NAME = nameof(MsgStringTableEntries);
        public MsgStringTableEntries(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public Entry[] Entries { get; set; } = default!;

        /// <summary>
        ///     A string table entry.
        /// </summary>
        public struct Entry
        {
            /// <summary>
            ///     The string contained inside of the message.
            /// </summary>
            public string String { get; set; }


            /// <summary>
            ///     The ID of the string inside of the message.
            /// </summary>
            public int Id { get; set; }
        }

        /// <inheritdoc />
        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var count = buffer.ReadUInt32();
            Entries = new Entry[count];
            for (var i = 0; i < count; i++)
            {
                Entries[i].Id = buffer.ReadVariableInt32();
                Entries[i].String = buffer.ReadString();
            }
        }

        /// <inheritdoc />
        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            if (Entries == null)
                throw new InvalidOperationException("Entries is null!");

            buffer.Write(Entries.Length);
            foreach (var entry in Entries)
            {
                buffer.WriteVariableInt32(entry.Id);
                buffer.Write(entry.String);
            }
        }
    }
}
