using System;

namespace Robust.Shared.Serialization.Manager.Attributes;

/// <summary>
///     Makes a type always be copied by reference when using it as the generic type in
///     <see cref="ISerializationManager.CopyTo"/> and <see cref="ISerializationManager.CreateCopy"/>.
///     This means that the source instance is returned directly.
///     <remarks>
///         Note that when calling any of the generic <see cref="ISerializationManager.CopyTo{T}"/> and
///         <see cref="ISerializationManager.CreateCopy{T}"/> methods, this attribute will only be respected
///         if the generic type passed to the copying methods has this attribute.
///         For example, if a copy method is called with a generic type T that is not annotated with this attribute,
///         but the actual type of the source parameter IS annotated with this attribute, it will not be copied by ref.
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
