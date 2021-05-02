using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Robust.Shared.Utility
{
    public class SpecificFieldInfo : AbstractFieldInfo
    {
        public override string Name { get; }

        public readonly FieldInfo FieldInfo;
        internal override MemberInfo MemberInfo => FieldInfo;

        public override Type FieldType => FieldInfo.FieldType;
        public override Type? DeclaringType => FieldInfo.DeclaringType;

        public SpecificFieldInfo(FieldInfo fieldInfo)
        {
            Name = fieldInfo.Name;
            FieldInfo = fieldInfo;
        }

        public override object? GetValue(object? obj) => FieldInfo.GetValue(obj);
        public override void SetValue(object? obj, object? value) => FieldInfo.SetValue(obj, value);

        public override T? GetAttribute<T>(bool includeBacking = false) where T : class
        {
            return FieldInfo.GetCustomAttribute<T>();
        }

        public override IEnumerable<T> GetAttributes<T>(bool includeBacking = false)
        {
            return FieldInfo.GetCustomAttributes<T>();
        }

        public override bool HasAttribute<T>(bool includeBacking = false)
        {
            return FieldInfo.HasCustomAttribute<T>();
        }

        public override bool TryGetAttribute<T>([NotNullWhen(true)] out T? attribute, bool includeBacking = false) where T : class
        {
            return FieldInfo.TryGetCustomAttribute(out attribute);
        }

        public override bool IsBackingField()
        {
            return FieldInfo.IsBackingField();
        }

        public override bool HasBackingField()
        {
            return false;
        }

        public override SpecificFieldInfo? GetBackingField()
        {
            return null;
        }

        public override bool TryGetBackingField([NotNullWhen(true)] out SpecificFieldInfo? field)
        {
            field = null;
            return false;
        }

        public static implicit operator FieldInfo(SpecificFieldInfo f) => f.FieldInfo;
        public static explicit operator SpecificFieldInfo(FieldInfo f) => new(f);

        public override string? ToString()
        {
            return FieldInfo.ToString();
        }
    }
}
