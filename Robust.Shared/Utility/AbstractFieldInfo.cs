using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Robust.Shared.Utility
{
    internal abstract class AbstractFieldInfo
    {
        public abstract string Name { get; }
        internal abstract MemberInfo MemberInfo { get; }
        public abstract Type FieldType { get; }
        public abstract Type? DeclaringType { get; }

        public abstract object? GetValue(object? obj);
        public abstract void SetValue(object? obj, object? value);

        public abstract T? GetAttribute<T>(bool includeBacking = false) where T : Attribute;
        public abstract IEnumerable<T> GetAttributes<T>(bool includeBacking = false) where T : Attribute;
        public abstract bool HasAttribute<T>(bool includeBacking = false) where T : Attribute;
        public abstract bool TryGetAttribute<T>([NotNullWhen(true)] out T? attribute, bool includeBacking = false) where T : Attribute;
        public abstract bool IsBackingField();
        public abstract bool HasBackingField();
        public abstract SpecificFieldInfo? GetBackingField();
        public abstract bool TryGetBackingField([NotNullWhen(true)] out SpecificFieldInfo? field);
    }
}
