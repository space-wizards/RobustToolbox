using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Robust.Shared.Utility
{
    public class SpecificPropertyInfo : AbstractFieldInfo
    {
        public override string Name { get; }

        public readonly PropertyInfo PropertyInfo;
        internal override MemberInfo MemberInfo => PropertyInfo;

        public override Type FieldType => PropertyInfo.PropertyType;
        public override Type? DeclaringType => PropertyInfo.DeclaringType;

        public SpecificPropertyInfo(PropertyInfo propertyInfo)
        {
            Name = propertyInfo.Name;
            PropertyInfo = propertyInfo;
        }

        public override object? GetValue(object? obj) => PropertyInfo.GetValue(obj);
        public override void SetValue(object? obj, object? value) => PropertyInfo.SetValue(obj, value);

        public override T? GetAttribute<T>(bool includeBacking = false) where T : class
        {
            if (includeBacking && TryGetBackingField(out var backing))
            {
                return PropertyInfo.GetCustomAttribute<T>() ?? backing.GetAttribute<T>(includeBacking);
            }

            return PropertyInfo.GetCustomAttribute<T>(includeBacking);
        }

        public override IEnumerable<T> GetAttributes<T>(bool includeBacking = false)
        {
            foreach (var attribute in PropertyInfo.GetCustomAttributes<T>())
            {
                yield return attribute;
            }

            if (includeBacking && TryGetBackingField(out var backing))
            {
                foreach (var attribute in backing.GetAttributes<T>(includeBacking))
                {
                    yield return attribute;
                }
            }
        }

        public override bool HasAttribute<T>(bool includeBacking = false)
        {
            if (includeBacking && TryGetBackingField(out var backing))
            {
                return PropertyInfo.HasCustomAttribute<T>() || backing.HasAttribute<T>(includeBacking);
            }

            return PropertyInfo.HasCustomAttribute<T>();
        }

        public override bool TryGetAttribute<T>([NotNullWhen(true)] out T? attribute, bool includeBacking = false) where T : class
        {
            return (attribute = GetAttribute<T>(includeBacking)) != null;
        }

        public override bool IsBackingField()
        {
            return false;
        }

        public override bool HasBackingField()
        {
            return DeclaringType?.HasBackingField(Name) ?? false;
        }

        public override SpecificFieldInfo? GetBackingField()
        {
            return DeclaringType?.GetBackingField(Name);
        }

        public override bool TryGetBackingField([NotNullWhen(true)] out SpecificFieldInfo? field)
        {
            return (field = GetBackingField()) != null;
        }

        public bool IsMostOverridden(Type type)
        {
            if (DeclaringType == type)
            {
                return true;
            }

            var setBase = PropertyInfo.SetMethod?.GetBaseDefinition();
            var getBase = PropertyInfo.GetMethod?.GetBaseDefinition();
            var currentType = type;
            var relevantProperties = type.GetAllProperties().Where(p => p.Name == PropertyInfo.Name).ToList();
            while (currentType != null)
            {
                foreach (var property in relevantProperties)
                {
                    if(property.DeclaringType != currentType) continue;

                    if (setBase != null && setBase == property.SetMethod?.GetBaseDefinition())
                    {
                        return property == PropertyInfo;
                    }

                    if (getBase != null && getBase == property.GetMethod?.GetBaseDefinition())
                    {
                        return property == PropertyInfo;
                    }
                }

                currentType = currentType.BaseType;
            }

            return false;
        }

        public static implicit operator PropertyInfo(SpecificPropertyInfo f) => f.PropertyInfo;
        public static explicit operator SpecificPropertyInfo(PropertyInfo f) => new(f);

        public override string? ToString()
        {
            return PropertyInfo.ToString();
        }
    }
}
