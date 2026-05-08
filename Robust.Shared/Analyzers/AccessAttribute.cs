using System;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

/// <summary>
/// <para>
///     Access is a way to describe how other classes are allowed to use a field in more precise terms than "can use"
///     and "cannot use". Think of it like friend classes from C++.
/// </para>
/// <para>
///     Access controls field, method, and property usage for three different kinds of users:
///     Self (the declaring class), Friend (classes explicitly named as friends by the access attribute),
///     and Other (classes that aren't the other two). Using <see cref="AccessPermissions"/> you can define what
///     operations each of the users is allowed.
/// </para>
/// </summary>
/// <example>
/// <code>
///     [RegisterComponent]
///     // Allow the system with utility functions for this component to modify it.
///     [Access(typeof(MySystem))]
///     public sealed class MyComponent : Component
///     {
///         public int Counter;
///     }
///     <br/>
///     public sealed class MySystem : EntitySystem
///     {
///         public void AddToCounter(Entity&lt;MyComponent&gt; entity)
///         {
///             // Works, we're a friend of the other type.
///             entity.Comp.Counter += 1;
///         }
///     }
///     <br/>
///     public sealed class OtherSystem : EntitySystem
///     {
///         public void AddToCounter(Entity&lt;MyComponent&gt; entity)
///         {
///             // Error RS2008: Tried to perform write access to member 'Counter' in type 'MyComponent', despite read access.
///             entity.Comp.Counter += 1;
///         }
///     }
/// </code>
/// </example>
/// <seealso cref="AccessPermissions"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct
                | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor)]
public sealed class AccessAttribute : Attribute
{
    /// <summary>
    ///     The list of types considered "friends" of the type with this attribute.
    ///     These types get elevated permissions.
    /// </summary>
    /// <seealso cref="Friend"/>
    public readonly Type[] Friends;

    public const AccessPermissions SelfDefaultPermissions = AccessPermissions.ReadWriteExecute;
    public const AccessPermissions FriendDefaultPermissions = AccessPermissions.ReadWriteExecute;
    public const AccessPermissions OtherDefaultPermissions = AccessPermissions.Read;

    /// <summary>
    ///     Access permissions for the type itself, or the type containing the member.
    /// </summary>
    public AccessPermissions Self   { get; set; }  = SelfDefaultPermissions;
    /// <summary>
    ///     Access permissions for types specified as <see cref="Friends"/>.
    /// </summary>
    public AccessPermissions Friend { get; set; }  = FriendDefaultPermissions;
    /// <summary>
    ///     Access permissions for types that aren't <see cref="Self"/> and aren't <see cref="Friend"/>.
    /// </summary>
    public AccessPermissions Other  { get; set;  } = OtherDefaultPermissions;

    public AccessAttribute(params Type[] friends)
    {
        Friends = friends;
    }
}
