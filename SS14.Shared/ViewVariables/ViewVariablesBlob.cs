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
            ///     Value of the property.
            ///     If it's a value type that can be serialized it's literally sent over,
            ///     otherwise it's a meta token like <see cref="ServerValueTypeToken"/> or <see cref="ReferenceToken"/>.
            /// </summary>
            public object Value { get; set; }
        }
    }

    [Serializable, NetSerializable]
    public class ViewVariablesBlobEntity : ViewVariablesBlob
    {
        public List<(string qualified, string stringified)> ComponentTypes { get; set; } = new List<(string, string)>();
    }
}
