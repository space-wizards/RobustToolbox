using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    // TODO Serialization: find a way to constrain this to DataFields only & make exclusive w/ NeverPush
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AlwaysPushInheritanceAttribute : Attribute
    {
    }
}
