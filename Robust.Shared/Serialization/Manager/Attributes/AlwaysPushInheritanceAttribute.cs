using System;
using System.Runtime.CompilerServices;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    //todo find a way to constrain this to datafields only
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AlwaysPushInheritanceAttribute : Attribute { }
}
