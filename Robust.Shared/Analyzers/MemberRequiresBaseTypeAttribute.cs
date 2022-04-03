using System;
using JetBrains.Annotations;

namespace Robust.Shared.Analyzers;

[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(Attribute))]
public sealed class MemberRequiresBaseTypeAttribute : Attribute
{
    public readonly Type[] Friends;

    public MemberRequiresBaseTypeAttribute(params Type[] friends)
    {
        Friends = friends;
    }

}
