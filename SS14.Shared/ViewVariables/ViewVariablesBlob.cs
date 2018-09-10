using System;
using System.Collections.Generic;
using SS14.Shared.Serialization;

namespace SS14.Shared.ViewVariables
{
    /// <summary>
    ///     Data blob that gets sent from server to client to display a VV window for a remote object.
    /// </summary>
    [Serializable, NetSerializable]
    public class ViewVariablesBlob
    {
    }

    [Serializable, NetSerializable]
    public class ViewVariablesBlobMetadata : ViewVariablesBlob
    {
        public List<object> Traits { get; set; }

        /// <summary>
        ///     The pretty type name of the remote object.
        /// </summary>
        public string ObjectTypePretty { get; set; }

        /// <summary>
        ///     The assembly qualified type name of the remote object.
        /// </summary>
        public string ObjectType { get; set; }

        /// <summary>
        ///     The <see cref="Object.ToString"/> output of the remote object.
        /// </summary>
        public string Stringified { get; set; }
    }

    [Serializable, NetSerializable]
    public class ViewVariablesBlobMembers : ViewVariablesBlob
    {
        /// <summary>
        ///     A list of properties the remote object has.
        /// </summary>
        public List<PropertyData> Properties { get; set; } = new List<PropertyData>();

        /// <summary>
        ///     Token used to indicate "this is a reference, but I can't send the actual reference over".
        /// </summary>
        [Serializable, NetSerializable]
        public class ReferenceToken
        {
            /// <summary>
            ///     The <see cref="Object.ToString"/> output of the referenced object.
            /// </summary>
            public string Stringified { get; set; }

            // ToString override so EditorDummy displays it correctly.
            public override string ToString()
            {
                return Stringified;
            }
        }

        /// <summary>
        ///     Sent if the value is a server-side only value type.
        /// </summary>
        [Serializable, NetSerializable]
        public class ServerValueTypeToken
        {
            /// <summary>
            ///     The <see cref="Object.ToString"/> output of the remote object.
            /// </summary>
            public string Stringified { get; set; }

            // ToString override so EditorDummy displays it correctly.
            public override string ToString()
            {
                return Stringified;
            }
        }

        /// <summary>
        ///     Data for a specific property.
        /// </summary>
        [Serializable, NetSerializable]
        public class PropertyData
        {
            /// <summary>
            ///     Whether the property can be edited by this client.
            /// </summary>
            public bool Editable { get; set; }

            /// <summary>
            ///     Assembly qualified type name of the property.
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            ///     Pretty type name of the property.
            /// </summary>
            public string TypePretty { get; set; }

            /// <summary>
            ///     Name of the property.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            ///     Index of the property to be referenced when modifying it.
            /// </summary>
            public int PropertyIndex { get; set; }

            /// <summary>
            ///     Value of the property.
            ///     If it's a value type that can be serialized it's literally sent over,
            ///     otherwise it's a meta token like <see cref="ServerValueTypeToken"/> or <see cref="ReferenceToken"/>.
            /// </summary>
            public object Value { get; set; }
        }
    }

    [Serializable, NetSerializable]
    public class ViewVariablesBlobEntityComponents : ViewVariablesBlob
    {
        public List<Entry> ComponentTypes { get; set; } = new List<Entry>();

        // This might as well be a ValueTuple but I couldn't get that to work.
        [Serializable, NetSerializable]
        public class Entry : IComparable<Entry>
        {
            public int CompareTo(Entry other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                return string.Compare(Stringified, other.Stringified, StringComparison.Ordinal);
            }

            public string Qualified { get; set; }
            public string Stringified { get; set; }
        }
    }

    [Serializable, NetSerializable]
    public class ViewVariablesBlobEnumerable : ViewVariablesBlob
    {
        public List<object> Objects { get; set; }
    }

    [Serializable, NetSerializable]
    public abstract class ViewVariablesRequest
    {
    }

    [Serializable, NetSerializable]
    public class ViewVariablesRequestMetadata : ViewVariablesRequest
    {
    }

    [Serializable, NetSerializable]
    public class ViewVariablesRequestMembers : ViewVariablesRequest
    {
    }

    [Serializable, NetSerializable]
    public class ViewVariablesRequestEntityComponents : ViewVariablesRequest
    {

    }

    [Serializable, NetSerializable]
    public class ViewVariablesRequestEnumerable : ViewVariablesRequest
    {
        public ViewVariablesRequestEnumerable(int fromIndex, int toIndex, bool refresh)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
            Refresh = refresh;
        }

        /// <summary>
        ///     The first index to be included.
        /// </summary>
        public int FromIndex { get; }

        /// <summary>
        ///     The last index to be included.
        /// </summary>
        public int ToIndex { get; }

        /// <summary>
        ///     If true, wipe the enumerator used and clear the cached values.
        ///     This is used for the refresh button in the VV window.
        ///     We need to wipe the server side cache or else it wouldn't be a refresh.
        /// </summary>
        public bool Refresh { get; }
    }
}
