using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    // TODO Serialization: find a way to constrain this to DataField only & make exclusive w/ AlwaysPush
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NeverPushInheritanceAttribute : Attribute
    {
    }
}
