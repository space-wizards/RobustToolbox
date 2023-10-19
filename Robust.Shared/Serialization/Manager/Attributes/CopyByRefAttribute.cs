using System;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
///     Makes a type always be copied by reference when using it as the generic parameter in
///     <see cref="ISerializationManager.CopyTo"/> and <see cref="ISerializationManager.CreateCopy"/>.
///     This means that the source instance is returned directly.
///     This attribute is not inherited.
///     <remarks>
///         Note that when calling any of the generic <see cref="ISerializationManager.CopyTo{T}"/> and
///         <see cref="ISerializationManager.CreateCopy{T}"/> methods, this attribute will only be respected
///         if the generic parameter passed to the copying methods has this attribute.
///         For example, if a copy method is called with a generic parameter T that is not annotated with this attribute,
///         but the actual type of the source parameter is annotated with this attribute, it will not be copied by ref.
///         Conversely, if the generic parameter T is annotated with this attribute, but the actual type of the source
///         is an inheritor which is not annotated with this attribute, it will still be copied by ref.
///         If the generic parameter T is a type derived from another that is annotated with the attribute,
///         but it itself is not annotated with this attribute, source will not be copied by ref as this attribute
///         is not inherited.
///         <code>
///             public class A {}
///
///             [CopyByRef]
///             public class B : A {}
///
///             public class C : B {}
///
///             public class Copier(ISerializationManager manager)
///             {
///                 var a = new A();
///                 var b = new B();
///                 var c = new C();
///
///                 // false, not copied by ref
///                 manager.CreateCopy(a) == a
///
///                 // false, not copied by ref
///                 manager.CreateCopy&lt;A&gt;(b) == b
///
///                 // true, copied by ref
///                 manager.CreateCopy(b) == b
///
///                 // false, not copied by ref
///                 manager.CreateCopy(c) == c
///
///                 // true, copied by ref
///                 manager.CreateCopy&lt;B&gt;(c) == c
///             }
///         </code>
///     </remarks>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface,
    Inherited = false)]
public sealed class CopyByRefAttribute : Attribute
{
}
