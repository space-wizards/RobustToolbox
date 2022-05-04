using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using static Robust.Shared.Serialization.Manager.SerializationManager;

namespace Robust.Shared.Serialization.Manager.Definition
{
    public sealed partial class DataDefinition
    {
        private readonly struct FieldInterfaceInfo
        {
            public readonly (bool Value, bool Sequence, bool Mapping) Reader;
            public readonly bool Writer;
            public readonly bool Copier;

            public FieldInterfaceInfo((bool Value, bool Sequence, bool Mapping) reader, bool writer, bool copier)
            {
                Reader = reader;
                Writer = writer;
                Copier = copier;
            }
        }

        private readonly PopulateDelegateSignature _populate;
        private readonly SerializeDelegateSignature _serialize;
        private readonly CopyDelegateSignature _copy;

        public DataDefinition(Type type, IDependencyCollection collection)
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

            _populate = EmitPopulateDelegate(collection);
            _serialize = EmitSerializeDelegate();
            _copy = EmitCopyDelegate();

            var fieldAccessors = new AccessField<object, object?>[BaseFieldDefinitions.Length];
            var fieldAssigners = new AssignField<object, object?>[BaseFieldDefinitions.Length];
            var interfaceInfos = new FieldInterfaceInfo[BaseFieldDefinitions.Length];

            for (var i = 0; i < BaseFieldDefinitions.Length; i++)
            {
                var fieldDefinition = BaseFieldDefinitions[i];

                fieldAccessors[i] = EmitFieldAccessor(fieldDefinition);
                fieldAssigners[i] = EmitFieldAssigner(fieldDefinition);

                //reader (value, sequence, mapping), writer, copier
                var reader = (false, false, false);
                var writer = false;
                var copier = false;
                if (fieldDefinition.Attribute.CustomTypeSerializer != null)
                {
                    foreach (var @interface in fieldDefinition.Attribute.CustomTypeSerializer.GetInterfaces())
                    {
                        var genericTypedef = @interface.GetGenericTypeDefinition();
                        if (genericTypedef == typeof(ITypeWriter<>))
                        {
                            if (@interface.GenericTypeArguments[0] == type)
                            {
                                writer = true;
                            }
                        }
                        else if ( genericTypedef == typeof(ITypeCopier<>))
                        {
                            if (@interface.GenericTypeArguments[0] == type)
                            {
                                copier = true;
                            }
                        }
                        else if (@interface.GetGenericTypeDefinition() == typeof(ITypeReader<,>))
                        {
                            if (@interface.GenericTypeArguments[0] == type)
                            {
                                if (@interface.GenericTypeArguments[1] == typeof(ValueDataNode))
                                {
                                    reader.Item1 = true;
                                }else if (@interface.GenericTypeArguments[1] == typeof(SequenceDataNode))
                                {
                                    reader.Item2 = true;
                                }else if (@interface.GenericTypeArguments[1] == typeof(MappingDataNode))
                                {
                                    reader.Item3 = true;
                                }
                            }
                        }
                    }

                    if (!reader.Item1 && !reader.Item2 && !reader.Item3 && !writer && !copier)
                    {
                        throw new InvalidOperationException(
                            $"Could not find any fitting implementation of ITypeReader, ITypeWriter or ITypeCopier for field {fieldDefinition.Attribute.Tag}({fieldDefinition.FieldType}) on type {type} on CustomTypeSerializer {fieldDefinition.Attribute.CustomTypeSerializer}");
                    }

                    interfaceInfos[i] = new FieldInterfaceInfo(reader, writer, copier);
                }

            }

            FieldAccessors = fieldAccessors.ToImmutableArray();
            FieldAssigners = fieldAssigners.ToImmutableArray();
            FieldInterfaceInfos = interfaceInfos.ToImmutableArray();
        }

        public Type Type { get; }

        private string[] Duplicates { get; }
        private object?[] DefaultValues { get; }

        private ImmutableArray<AccessField<object, object?>> FieldAccessors { get; }
        private ImmutableArray<AssignField<object, object?>> FieldAssigners { get; }

        internal ImmutableArray<FieldDefinition> BaseFieldDefinitions { get; }
        private ImmutableArray<FieldInterfaceInfo> FieldInterfaceInfos { get; }

        public object Populate(
            object target,
            MappingDataNode mapping,
            ISerializationManager serialization,
            ISerializationContext? context,
            bool skipHook)
        {
            return _populate(target, mapping, serialization, context, skipHook, DefaultValues);
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
                        Logger.ErrorS(LogCategory, $"Property {propertyInfo} is annotated with DataFieldAttribute but has no getter");
                        continue;
                    }
                    else if (propertyInfo.PropertyInfo.SetMethod == null)
                    {
                        if (!propertyInfo.TryGetBackingField(out var backingFieldInfo))
                        {
                            Logger.ErrorS(LogCategory, $"Property {propertyInfo} in type {propertyInfo.DeclaringType} is annotated with DataFieldAttribute as non-readonly but has no auto-setter");
                            continue;
                        }

                        backingField = backingFieldInfo;
                    }
                }

                var inheritanceBehaviour = InheritanceBehavior.Default;
                if (abstractFieldInfo.HasAttribute<AlwaysPushInheritanceAttribute>(true))
                {
                    inheritanceBehaviour = InheritanceBehavior.Always;
                }
                else if (abstractFieldInfo.HasAttribute<NeverPushInheritanceAttribute>(true))
                {
                    inheritanceBehaviour = InheritanceBehavior.Never;
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
