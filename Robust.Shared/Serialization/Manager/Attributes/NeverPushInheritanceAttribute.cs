using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    //todo paul find a way to constrain this to datafields only & make exclusive w/ alwayspush
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]

    public class NeverPushInheritanceAttribute : Attribute
    {

    }
}
