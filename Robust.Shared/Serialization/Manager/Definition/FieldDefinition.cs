using System;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager.Definition
{
    internal sealed class FieldDefinition
    {
        public FieldDefinition(
            DataFieldBaseAttribute attr,
            object? defaultValue,
            AbstractFieldInfo fieldInfo,
            AbstractFieldInfo backingField,
            InheritanceBehavior inheritanceBehavior)
        {
            BackingField = backingField;
            Attribute = attr;
            DefaultValue = defaultValue;
            FieldInfo = fieldInfo;
            InheritanceBehavior = inheritanceBehavior;
        }

        public DataFieldBaseAttribute Attribute { get; }

        public object? DefaultValue { get; }

        public InheritanceBehavior InheritanceBehavior { get; }

        public AbstractFieldInfo BackingField { get; }

        public AbstractFieldInfo FieldInfo { get; }

        public Type FieldType => FieldInfo.FieldType;

        public object? GetValue(object? obj)
        {
            return BackingField.GetValue(obj);
        }

        public void SetValue(object? obj, object? value)
        {
            BackingField.SetValue(obj, value);
        }

        public override string ToString()
        {
            return $"{FieldInfo.Name}({Attribute})";
        }
    }
}
