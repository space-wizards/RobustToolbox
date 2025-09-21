using System;
using Robust.Shared.Serialization.Manager.Attributes;
#if !ROBUST_ANALYZERS_TEST
using Robust.Shared.ViewVariables;
#endif

namespace Robust.Shared.Prototypes
{
    /// <summary>
    ///     An IPrototype is a prototype that can be loaded from the global YAML prototypes.
    /// </summary>
    /// <remarks>
    ///     To use this, the prototype must be accessible through IoC with <see cref="IoCTargetAttribute"/>
    ///     and it must have a <see cref="PrototypeAttribute"/> to give it a type string.
    /// </remarks>
    public interface IPrototype
    {
        /// <summary>
        /// An ID for this prototype instance.
        /// If this is a duplicate, an error will be thrown.
        /// </summary>
#if !ROBUST_ANALYZERS_TEST
        [ViewVariables(VVAccess.ReadOnly)]
#endif
        string ID { get; }
    }

    public interface IInheritingPrototype
    {
        string[]? Parents { get; }

        bool Abstract { get; }
    }

    public sealed class IdDataFieldAttribute : DataFieldAttribute
    {
        public const string Name = "id";
        public IdDataFieldAttribute(int priority = 1, Type? customTypeSerializer = null) :
            base(Name, false, priority, true, false, customTypeSerializer)
        {
        }
    }

    public sealed class ParentDataFieldAttribute : DataFieldAttribute
    {
        public const string Name = "parent";
        public ParentDataFieldAttribute(Type prototypeIdSerializer, int priority = 1) :
            base(Name, false, priority, false, false, prototypeIdSerializer)
        {
        }
    }

    public sealed class AbstractDataFieldAttribute : DataFieldAttribute
    {
        public const string Name = "abstract";
        public AbstractDataFieldAttribute(int priority = 1) :
            base(Name, false, priority, false, false, null)
        {
        }
    }
}
