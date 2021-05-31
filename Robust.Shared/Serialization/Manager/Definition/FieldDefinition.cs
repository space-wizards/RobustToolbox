using System;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Attributes.Deserializer;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager.Definition
{
    public class FieldDefinition
    {
        public FieldDefinition(
            DataFieldAttribute attr,
            object? defaultValue,
            AbstractFieldInfo fieldInfo,
            AbstractFieldInfo backingField,
            InheritanceBehavior inheritanceBehavior,
            IDataFieldDeserializer deserializer)
        {
            BackingField = backingField;
            Attribute = attr;
            DefaultValue = defaultValue;
            FieldInfo = fieldInfo;
            InheritanceBehavior = inheritanceBehavior;
            Deserializer = deserializer;
        }

        public DataFieldAttribute Attribute { get; }

        public object? DefaultValue { get; }

        public InheritanceBehavior InheritanceBehavior { get; }

        internal AbstractFieldInfo BackingField { get; }

        internal AbstractFieldInfo FieldInfo { get; }

        public Type FieldType => FieldInfo.FieldType;

        internal IDataFieldDeserializer Deserializer { get; }
    }
}
