using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.ViewVariables
{
    /// <summary>
    ///     Data blob that gets sent from server to client to display a VV window for a remote object.
    ///     These blobs should be requested with a specific <see cref="ViewVariablesRequest"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract class ViewVariablesBlob
    {
    }

    /// <summary>
    ///     Contains the fundamental metadata for an object, such as type, <see cref="Object.ToString"/> output, and the list of "traits".
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesBlobMetadata : ViewVariablesBlob
    {
        /// <summary>
        ///     A list of traits for this remote object.
        ///     A trait is basically saying "this object can be viewed in a specific way".
        ///     There's a trait for "this object has members that are directly accessible to VV (the usual),
        ///     A trait for objects that are <see cref="IEnumerable"/>, etc...
        ///     For flexibility, these traits can be any object that the server/client need to understand each other.
        ///     At the moment though the only thing they're used with is <see cref="ViewVariablesTraits"/>.
        /// </summary>
        /// <seealso cref="ViewVariablesManager.TraitIdsFor" />
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

    /// <summary>
    ///     Contains the VV-accessible members (fields or properties) of a remote object.
    ///     Requested by <see cref="ViewVariablesRequestMembers"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesBlobMembers : ViewVariablesBlob
    {
        /// <summary>
        ///     A list of VV-accessible the remote object has.
        /// </summary>
        public List<(string groupName, List<MemberData> groupMembers)> MemberGroups { get; set; }
            = new();

        /// <summary>
        ///     Token used to indicate "this is a reference, but I can't send the actual reference over".
        /// </summary>
        [Serializable, NetSerializable]
        [Virtual]
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
        ///     Token used to indicate "this is a prototype reference, but I can't send the actual reference over".
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class PrototypeReferenceToken : ReferenceToken
        {
            /// <summary>
            ///     The ID of the prototype.
            /// </summary>
            [CanBeNull]
            public string ID { get; set; }

            /// <summary>
            ///     The Prototype Variant identifier.
            /// </summary>
            public string Variant { get; set; }

            // ToString override so EditorDummy displays it correctly.
            public override string ToString()
            {
                return $"{Stringified} Prototype: {Variant} ID: {ID}";
            }
        }

        /// <summary>
        ///     Sent if the value is a server-side only value type.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ServerValueTypeToken
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

        [Serializable, NetSerializable]
        public sealed class ServerKeyValuePairToken
        {
            public object Key { get; set; }
            public object Value { get; set; }
        }

        /// <summary>
        ///     Wrapper for a non-serializable value-type tuple.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class ServerTupleToken : ITuple
        {
            public object[] Items { get; set; }

            public object this[int index] => Items[index];

            public int Length => Items.Length;
        }

        /// <summary>
        ///     Data for a specific property.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class MemberData
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

    /// <summary>
    ///     Contains the type names of the components of a remote <see cref="Robust.Shared.GameObjects.EntityUid"/>.
    ///     Requested by <see cref="ViewVariablesRequestEntityComponents"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesBlobEntityComponents : ViewVariablesBlob
    {
        public List<Entry> ComponentTypes { get; set; } = new();

        // This might as well be a ValueTuple but I couldn't get that to work.
        [Serializable, NetSerializable]
        public sealed class Entry : IComparable<Entry>
        {
            public int CompareTo(Entry other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                return string.Compare(Stringified, other.Stringified, StringComparison.Ordinal);
            }

            public string FullName { get; set; }
            public string Stringified { get; set; }
            public string ComponentName { get; set; }
        }
    }

    /// <summary>
    ///     Contains a list of server-side component that can be added to a remote <see cref="Robust.Shared.GameObjects.EntityUid"/>.
    ///     Requested by <see cref="ViewVariablesRequestAllValidComponents"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesBlobAllValidComponents : ViewVariablesBlob
    {
        public List<string> ComponentTypes { get; set; } = new();
    }

    /// <summary>
    ///     Contains a list of all server-side prototypes of a variant.
    ///     Requested by <see cref="ViewVariablesRequestAllPrototypes"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesBlobAllPrototypes : ViewVariablesBlob
    {
        public List<string> Prototypes { get; set; } = new();
        public string Variant { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Contains a range of a remote <see cref="IEnumerable"/>'s results.
    ///     Requested by <see cref="ViewVariablesRequestEnumerable"/>
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesBlobEnumerable : ViewVariablesBlob
    {
        /// <summary>
        ///     The list of objects inside the range specified by the
        ///     <see cref="ViewVariablesRequestEnumerable"/> used to request this blob.
        /// </summary>
        public List<object> Objects { get; set; }
    }

    /// <summary>
    ///     Base class for a "request" that can be sent to get information about the remote object of a session.
    ///     You should pass these requests to <c>IViewVariablesManagerInternal.RequestData</c>, which will return the corresponding blob over the wire.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract class ViewVariablesRequest
    {
    }

    /// <summary>
    ///     Requests the server to send us a <see cref="ViewVariablesRequestMembers"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesRequestMetadata : ViewVariablesRequest
    {
    }

    /// <summary>
    ///     Requests the server to send us a <see cref="ViewVariablesRequestMembers"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesRequestMembers : ViewVariablesRequest
    {
    }

    /// <summary>
    ///     Requests the server to send us a <see cref="ViewVariablesBlobEntityComponents"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesRequestEntityComponents : ViewVariablesRequest
    {

    }

    /// <summary>
    ///     Requests the server to send us a <see cref="ViewVariablesBlobAllValidComponents"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesRequestAllValidComponents : ViewVariablesRequest
    {
    }

    /// <summary>
    ///     Requests the server to send us a <see cref="ViewVariablesBlobAllPrototypes"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesRequestAllPrototypes : ViewVariablesRequest
    {
        public string Variant { get; }

        public ViewVariablesRequestAllPrototypes(string variant)
        {
            Variant = variant;
        }
    }

    /// <summary>
    ///     Requests the server to send us a <see cref="ViewVariablesBlobEnumerable"/> containing data in the specified range.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesRequestEnumerable : ViewVariablesRequest
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
