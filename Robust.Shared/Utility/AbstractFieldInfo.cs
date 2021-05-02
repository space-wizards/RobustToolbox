using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Robust.Shared.Utility
{
    public abstract class AbstractFieldInfo
    {
        public abstract string Name { get; }
        internal abstract MemberInfo MemberInfo { get; }
        public abstract Type FieldType { get; }
        public abstract Type? DeclaringType { get; }

        public abstract object? GetValue(object? obj);
        public abstract void SetValue(object? obj, object? value);

        public abstract T? GetCustomAttribute<T>() where T : Attribute;
        public abstract IEnumerable<T> GetCustomAttributes<T>() where T : Attribute;
        public abstract bool HasCustomAttribute<T>() where T : Attribute;
        public abstract bool TryGetCustomAttribute<T>([NotNullWhen(true)] out T? attribute) where T : Attribute;
        public abstract bool IsBackingField();
        public abstract bool HasBackingField();
        public abstract SpecificFieldInfo? GetBackingField();
        public abstract bool TryGetBackingField([NotNullWhen(true)] out SpecificFieldInfo? field);
    }
}