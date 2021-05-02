using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager.DataDefinition
{
    public partial class DataDefinition
    {
        private readonly DeserializeDelegate _deserialize;
        private readonly PopulateDelegateSignature _populate;
        private readonly SerializeDelegateSignature _serialize;
        private readonly CopyDelegateSignature _copy;

        public DataDefinition(Type type)
        {
            Type = type;

            var fieldDefs = GetFieldDefinitions();

            Duplicates = fieldDefs
                .Where(f =>
                    fieldDefs.Count(df => df.Attribute.Tag == f.Attribute.Tag) > 1)
                .Select(f => f.Attribute.Tag)
                .Distinct()
                .ToArray();

            var fields = fieldDefs;

            fields.Sort((a, b) => b.Attribute.Priority.CompareTo(a.Attribute.Priority));

            BaseFieldDefinitions = fields.ToImmutableArray();
            DefaultValues = fieldDefs.Select(f => f.DefaultValue).ToArray();

            _deserialize = EmitDeserializationDelegate();
            _populate = EmitPopulateDelegate();
            _serialize = EmitSerializeDelegate();
            _copy = EmitCopyDelegate();

            FieldAccessors = new AccessField<object, object?>[BaseFieldDefinitions.Length];

            for (var i = 0; i < BaseFieldDefinitions.Length; i++)
            {
                var fieldDefinition = BaseFieldDefinitions[i];
                FieldAccessors[i] = EmitFieldAccessor(fieldDefinition);
            }

            FieldAssigners = new AssignField<object, object?>[BaseFieldDefinitions.Length];

            for (var i = 0; i < BaseFieldDefinitions.Length; i++)
            {
                var fieldDefinition = BaseFieldDefinitions[i];
                FieldAssigners[i] = EmitFieldAssigner(fieldDefinition);
            }
        }

        public Type Type { get; }

        private string[] Duplicates { get; }
        private object?[] DefaultValues { get; }

        private AccessField<object, object?>[] FieldAccessors { get; }
        private AssignField<object, object?>[] FieldAssigners { get; }

        internal ImmutableArray<FieldDefinition> BaseFieldDefinitions { get; }

        public DeserializationResult Populate(object target, DeserializedFieldEntry[] fields)
        {
            return _populate(target, fields, DefaultValues);
        }

        public DeserializationResult Populate(
            object target,
            MappingDataNode mapping,
            ISerializationManager serialization,
            ISerializationContext? context,
            bool skipHook)
        {
            var fields = _deserialize(mapping, serialization, context, skipHook);
            return _populate(target, fields, DefaultValues);
        }

        public MappingDataNode Serialize(
            object obj,
            ISerializationManager serialization,
            ISerializationContext? context,
            bool alwaysWrite)
        {
            return _serialize(obj, serialization, context, alwaysWrite, DefaultValues);
        }

        public object Copy(
            object source,
            object target,
            ISerializationManager serialization,
            ISerializationContext? context)
        {
            return _copy(source, target, serialization, context);
        }

        public ValidationNode Validate(
            ISerializationManager serialization,
            MappingDataNode mapping,
            ISerializationContext? context)
        {
            var validatedMapping = new Dictionary<ValidationNode, ValidationNode>();

            foreach (var (key, val) in mapping.Children)
            {
                if (key is not ValueDataNode valueDataNode)
                {
                    validatedMapping.Add(new ErrorNode(key, "Key not ValueDataNode."), new InconclusiveNode(val));
                    continue;
                }

                var field = BaseFieldDefinitions.FirstOrDefault(f => f.Attribute.Tag == valueDataNode.Value);
                if (field == null)
                {
                    var error = new ErrorNode(
                        key,
                        $"Field \"{valueDataNode.Value}\" not found in \"{Type}\".",
                        false);

                    validatedMapping.Add(error, new InconclusiveNode(val));
                    continue;
                }

                var keyValidated = serialization.ValidateNode(typeof(string), key, context);
                ValidationNode valValidated = field.Attribute.CustomTypeSerializer != null
                    ? serialization.ValidateNodeWith(field.FieldType,
                        field.Attribute.CustomTypeSerializer, val, context)
                    : serialization.ValidateNode(field.FieldType, val, context);

                validatedMapping.Add(keyValidated, valValidated);
            }

            return new ValidatedMappingNode(validatedMapping);
        }

        public bool CanCallWith(object obj) => Type.IsInstanceOfType(obj);

        public bool TryGetDuplicates([NotNullWhen(true)] out string[] duplicates)
        {
            duplicates = Duplicates;
            return duplicates.Length > 0;
        }

        private List<FieldDefinition> GetFieldDefinitions()
        {
            var dummyObject = Activator.CreateInstance(Type) ?? throw new NullReferenceException();
            var fieldDefinitions = new List<FieldDefinition>();

            foreach (var abstractFieldInfo in Type.GetAllPropertiesAndFields())
            {
                if (abstractFieldInfo.IsBackingField())
                {
                    continue;
                }

                if (!abstractFieldInfo.TryGetAttribute(out DataFieldAttribute? dataField, true))
                {
                    continue;
                }

                var backingField = abstractFieldInfo;

                if (abstractFieldInfo is SpecificPropertyInfo propertyInfo)
                {
                    // We only want the most overriden instance of a property for the type we are working with
                    if (!propertyInfo.IsMostOverridden(Type))
                    {
                        continue;
                    }

                    if (propertyInfo.PropertyInfo.GetMethod == null)
                    {
                        Logger.ErrorS(SerializationManager.LogCategory, $"Property {propertyInfo} is annotated with DataFieldAttribute but has no getter");
                        continue;
                    }
                    else if (propertyInfo.PropertyInfo.SetMethod == null)
                    {
                        if (!propertyInfo.TryGetBackingField(out var backingFieldInfo))
                        {
                            Logger.ErrorS(SerializationManager.LogCategory, $"Property {propertyInfo} in type {propertyInfo.DeclaringType} is annotated with DataFieldAttribute as non-readonly but has no auto-setter");
                            continue;
                        }

                        backingField = backingFieldInfo;
                    }
                }

                var inheritanceBehaviour = InheritanceBehaviour.Default;
                if (abstractFieldInfo.HasAttribute<AlwaysPushInheritanceAttribute>(true))
                {
                    inheritanceBehaviour = InheritanceBehaviour.Always;
                }
                else if (abstractFieldInfo.HasAttribute<NeverPushInheritanceAttribute>(true))
                {
                    inheritanceBehaviour = InheritanceBehaviour.Never;
                }

                var fieldDefinition = new FieldDefinition(
                    dataField,
                    abstractFieldInfo.GetValue(dummyObject),
                    abstractFieldInfo,
                    backingField,
                    inheritanceBehaviour);

                fieldDefinitions.Add(fieldDefinition);
            }

            return fieldDefinitions;
        }
    }
}
