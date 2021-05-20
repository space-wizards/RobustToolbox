using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Struct |
        AttributeTargets.Enum |
        AttributeTargets.Interface,
        Inherited = false)]
    public class CopyByRefAttribute : Attribute
    {
    }
}
