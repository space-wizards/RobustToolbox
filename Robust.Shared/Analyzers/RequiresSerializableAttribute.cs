using System;

namespace Robust.Shared.Analyzers
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequiresSerializableAttribute : Attribute { }
}
