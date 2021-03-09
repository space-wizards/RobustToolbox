using System;

namespace Robust.Shared.GameObjects
{
    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitAssignment]
    public class ComponentDependencyAttribute : Attribute
    {
        public readonly string? OnAddMethodName;
        public readonly string? OnRemoveMethodName;

        public ComponentDependencyAttribute(string? onAddMethodName = null, string? onRemoveMethodName = null)
        {
            OnAddMethodName = onAddMethodName;
            OnRemoveMethodName = onRemoveMethodName;
        }
    }
}
