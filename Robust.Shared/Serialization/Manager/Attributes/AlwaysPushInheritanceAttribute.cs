using System;
using System.Runtime.CompilerServices;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    //todo paul find a way to constrain this to datafields only & make exclusive w/ neverpush
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AlwaysPushInheritanceAttribute : Attribute { }
}
