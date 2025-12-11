using System;
using Robust.Shared.Serialization.Manager.Attributes;
#if !ROBUST_ANALYZERS_TEST
using Robust.Shared.ViewVariables;
#endif

namespace Robust.Shared.Prototypes
{
    /// <summary>
    ///     IPrototype, when combined with <see cref="PrototypeAttribute"/>, defines a type that the game can load from
    ///     the global YAML prototypes folder during init or runtime. It's a way of defining data for the game to read
    ///     and act on.
    /// </summary>
    /// <include file='Docs.xml' path='entries/entry[@name="IPrototype"]/*'/>
    /// <seealso cref="IPrototypeManager"/>
    /// <seealso cref="PrototypeAttribute"/>
    /// <seealso cref="IInheritingPrototype"/>
    public interface IPrototype
    {
        /// <summary>
        ///     A unique ID for this prototype instance.
        ///     This will never be a duplicate, and the game will error during loading if there are multiple prototypes
        ///     with the same unique ID.
        /// </summary>
#if !ROBUST_ANALYZERS_TEST
        [ViewVariables(VVAccess.ReadOnly)]
#endif
        string ID { get; }
    }

    /// <summary>
    ///     An extension of <see cref="IPrototype"/> that allows for a prototype to have parents that it inherits data
    ///     from. This, alongside <see cref="AlwaysPushInheritanceAttribute"/> and
    ///     <see cref="NeverPushInheritanceAttribute"/>, allow data-based multiple inheritance.
    ///     <br/>
    ///     An example of this in practice is <see cref="EntityPrototype"/>.
    /// </summary>
    /// <include file='Docs.xml' path='entries/entry[@name="IPrototype"]/*'/>
    /// <seealso cref="IPrototypeManager"/>
    /// <seealso cref="PrototypeAttribute"/>
    /// <seealso cref="IPrototype"/>
    public interface IInheritingPrototype
    {
        /// <summary>
        ///     The collection of parents for this prototype. Parents' data is applied to the child in order of
        ///     specification in the array.
        /// </summary>
        string[]? Parents { get; }

        /// <summary>
        ///     Whether this prototype is "abstract". This behaves ike an abstract class, abstract prototypes are never
        ///     indexable and do not show up when enumerating prototypes, as they're just a source of data to inherit
        ///     from.
        /// </summary>
        bool Abstract { get; }
    }

    /// <summary>
    ///     Marks a field as a prototype's unique identifier. This field must always be a <c>string?</c>.
    ///     <br/>
    ///     This field is always required.
    /// </summary>
    /// <seealso cref="IPrototype"/>
    public sealed class IdDataFieldAttribute : DataFieldAttribute
    {
        public const string Name = "id";
        public IdDataFieldAttribute(int priority = 1, Type? customTypeSerializer = null) :
            base(Name, false, priority, true, false, customTypeSerializer)
        {
        }
    }

    /// <summary>
    ///     Marks a field as the parent/parents field for this prototype, as required by
    ///     <see cref="IInheritingPrototype"/>. This must either be a <c>string?</c>, or <c>string[]?</c>.
    ///     <br/>
    ///     This field is never required.
    /// </summary>
    /// <seealso cref="IInheritingPrototype"/>
    public sealed class ParentDataFieldAttribute : DataFieldAttribute
    {
        public const string Name = "parent";
        public ParentDataFieldAttribute(Type prototypeIdSerializer, int priority = 1) :
            base(Name, false, priority, false, false, prototypeIdSerializer)
        {
        }
    }

    /// <summary>
    ///     Marks a field as the abstract field for this prototype, as required by
    ///     <see cref="IInheritingPrototype"/>. This must be a <c>bool</c>.
    ///     <br/>
    ///     This field is never required.
    /// </summary>
    /// <seealso cref="IInheritingPrototype"/>
    public sealed class AbstractDataFieldAttribute : DataFieldAttribute
    {
        public const string Name = "abstract";
        public AbstractDataFieldAttribute(int priority = 1) :
            base(Name, false, priority, false, false, null)
        {
        }
    }
}
