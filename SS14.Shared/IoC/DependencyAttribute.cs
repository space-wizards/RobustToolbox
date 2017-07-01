using System;

namespace SS14.Shared.IoC
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DependencyAttribute : Attribute
    {
    }
}
