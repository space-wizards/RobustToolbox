using System;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.ViewVariables;
#if !ROBUST_ANALYZERS_TEST
using JetBrains.Annotations;
#endif

namespace Robust.Shared.Serialization.Manager.Attributes
{
    /// <summary>
    ///     Marks a field or property as being serializable/deserializable, also implying
    ///     <see cref="ViewVariablesAttribute"/> with ReadWrite permissions.
    /// </summary>
    /// <include file='Docs.xml' path='entries/entry[@name="DataDefinitionExample"]/*'/>
    /// <seealso cref="DataDefinitionAttribute"/>
    /// <seealso cref="DataRecordAttribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
#if !ROBUST_ANALYZERS_TEST
    [MeansImplicitAssignment]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    [Virtual]
#endif
    public class DataFieldAttribute : DataFieldBaseAttribute
    {
        /// <summary>
        ///     The name of this field in YAML.
        ///     If null, the name of the C# field will be used instead, with the first letter lowercased.
        /// </summary>
        public string? Tag { get; internal set; }

        /// <summary>
        ///     Whether this field being mapped is required for the object to successfully deserialize.
        ///     This will not guarantee that the field is mapped when the program is run,
        ///     it is meant to be used as metadata information.
        /// </summary>
        public readonly bool Required;

        /// <param name="tag">See <see cref="Tag"/>.</param>
        /// <param name="readOnly">See <see cref="DataFieldBaseAttribute.ReadOnly"/>.</param>
        /// <param name="priority">See <see cref="DataFieldBaseAttribute.Priority"/>.</param>
        /// <param name="required">See <see cref="Required"/>.</param>
        /// <param name="serverOnly">See <see cref="DataFieldBaseAttribute.ServerOnly"/>.</param>
        /// <param name="customTypeSerializer">See <see cref="DataFieldBaseAttribute.CustomTypeSerializer"/>.</param>
        public DataFieldAttribute(string? tag = null, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false, Type? customTypeSerializer = null) : base(readOnly, priority, serverOnly, customTypeSerializer)
        {
            Tag = tag;
            Required = required;
        }

        public override string? ToString()
        {
            return Tag;
        }
    }

    public abstract class DataFieldBaseAttribute : Attribute
    {
        /// <summary>
        ///     Defines an order datafields should be deserialized in. You rarely if ever need this functionality, and
        ///     it has no effect on serialization.
        /// </summary>
        /// <example>
        ///     <code>
        ///         [DataDefinition]
        ///         public class TestClass
        ///         {
        ///             // This field is decoded first, as it has the highest priority.
        ///             [DataField(priority: 3)]
        ///             public FooData First = new();
        ///             <br/>
        ///             // This field has the default priority of 0, and is in the middle.
        ///             [DataField]
        ///             public bool Middle = false;
        ///             <br/>
        ///             // This field has the lowest priority, so it's dead last.
        ///             [DataField(priority: -999)]
        ///             public int DeadLast = 0;
        ///         }
        ///     </code>
        /// </example>
        public readonly int Priority;

        /// <summary>
        ///     A specific <see cref="ITypeSerializer{TType,TNode}"/> implementation to use for parsing this type.
        ///     This allows you to provide custom yaml parsing logic for a field, an example of this in regular use is
        ///     <see cref="FlagSerializer{TTag}"/>.
        /// </summary>
        public readonly Type? CustomTypeSerializer;

        /// <summary>
        ///     Marks the datafield as only ever being <b>read/deserialized</b> from YAML, it will never be
        ///     written/saved.
        ///
        ///     This is useful for data that is only ever used during, say, entity setup, and shouldn't be kept in live
        ///     entities nor saved for them.
        /// </summary>
        public readonly bool ReadOnly;

        /// <summary>
        ///     Marks the datafield as server only, indicating to client code that it should not attempt to read or
        ///     write this field because it may not understand the contained data.
        ///
        ///     This is useful for working with types that only exist on the server in otherwise shared data like a
        ///     shared prototype.
        /// </summary>
        public readonly bool ServerOnly;

        /// <param name="readOnly">See <see cref="ReadOnly"/>.</param>
        /// <param name="priority">See <see cref="Priority"/>.</param>
        /// <param name="serverOnly">See <see cref="ServerOnly"/>.</param>
        /// <param name="customTypeSerializer">See <see cref="CustomTypeSerializer"/>.</param>
        protected DataFieldBaseAttribute(bool readOnly = false, int priority = 1, bool serverOnly = false, Type? customTypeSerializer = null)
        {
            ReadOnly = readOnly;
            Priority = priority;
            ServerOnly = serverOnly;
            CustomTypeSerializer = customTypeSerializer;
        }
    }

}
