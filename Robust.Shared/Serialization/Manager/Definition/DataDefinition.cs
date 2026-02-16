using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.Serialization.NamingConventions;
using static Robust.Shared.Serialization.Manager.SerializationManager;

namespace Robust.Shared.Serialization.Manager.Definition
{
    public abstract class DataDefinition
    {
        internal ImmutableArray<FieldDefinition> BaseFieldDefinitions { get; init; }
        internal bool IsRecord { get; init; }
#pragma warning disable CS0618
        internal abstract PopulateDelegateSignature<object> PopulateObj { get; init; }
#pragma warning restore CS0618

        public abstract bool TryGetDuplicates(out string[] duplicates);
    }

    internal sealed class DataDefinition<T> : DataDefinition where T : ISerializationGenerated<T>
    {
#pragma warning disable CS0618
        internal readonly PopulateDelegateSignature<T> Populate;
        internal readonly SerializeDelegateSignature<T> Serialize;
        internal readonly CopyDelegateSignature<T> CopyTo;

        internal override PopulateDelegateSignature<object> PopulateObj { get; init; }
#pragma warning restore CS0618

        [UsedImplicitly]
        internal DataDefinition(SerializationManager manager, bool isRecord)
        {
            IsRecord = isRecord;

            var fieldDefs = GetFieldDefinitions(isRecord);
            foreach (var field in fieldDefs)
            {
                if (field.Attribute is DataFieldAttribute { Tag: null } attribute)
                    attribute.Tag = DataDefinitionUtility.AutoGenerateTag(field.FieldInfo.Name);
            }

            fieldDefs.Sort((a, b) =>
            {
                var priority = b.Attribute.Priority.CompareTo(a.Attribute.Priority);
                if (priority != 0)
                    return priority;

                return b.FieldInfo.Name.CompareTo(a.FieldInfo.Name, StringComparison.OrdinalIgnoreCase);
            });

            var dataFields = fieldDefs
                .Select(f => f.Attribute)
                .OfType<DataFieldAttribute>()
                .ToArray();

            Duplicates = dataFields
                .Where(f =>
                    dataFields.Count(df => df.Tag == f.Tag) > 1)
                .Select(f => f.Tag!)
                .Distinct()
                .ToArray();

            BaseFieldDefinitions = fieldDefs.ToImmutableArray();

            DefaultValues = fieldDefs.Select(f => f.DefaultValue).ToImmutableArray();
            DefaultValuesDict = fieldDefs.ToImmutableDictionary(
                f => f.Attribute is DataFieldAttribute attr
                    ? attr.Tag ?? f.CamelCasedName
                    : f.CamelCasedName,
                f => f.DefaultValue
            );

            //has to be done after fieldinterfaceinfos are done
#pragma warning disable CS0618
            PopulateObj = (ref target, node, serialization, ctx, context) =>
            {
                var obj = (T) target;
                T.Read(ref obj, node, serialization, ctx, context);
                target = obj;
            };

            Populate = T.Read;
            Serialize = T.Write;
            CopyTo = null!;
            FieldValidators = T.Validate;
#pragma warning restore CS0618
        }

        private string[] Duplicates { get; }
        internal ImmutableArray<object?> DefaultValues { get; }
        internal ImmutableDictionary<string, object?> DefaultValuesDict { get; }

#pragma warning disable CS0618
        private ValidateAllFieldsDelegate FieldValidators { get; }
#pragma warning restore CS0618

        private bool TryGetIndex(string tag, out int index)
        {
            for (index = 0; index < BaseFieldDefinitions.Length; index++)
            {
                if (BaseFieldDefinitions[index].Attribute is DataFieldAttribute dataFieldAttribute &&
                    dataFieldAttribute.Tag == tag)
                    return true;
            }

            return false;
        }

        private bool TryGetIncludeMappingPair(List<ValidatedMappingNode> includeValidations, string key, out KeyValuePair<ValidationNode, ValidationNode> pair)
        {
            foreach (var includeValidation in includeValidations)
            {
                if (includeValidation.Mapping.TryFirstOrNull(x =>
                            x.Key is ValidatedValueNode { DataNode: ValueDataNode valNode } &&
                            valNode.Value == key,
                        out var validatedPair))
                {
                    pair = validatedPair.Value;
                    return true;
                }
            }

            pair = default;
            return false;
        }

        public ValidationNode Validate(
            ISerializationManager serialization,
            MappingDataNode mapping,
            ISerializationContext? context)
        {
            var validatedMapping = new Dictionary<ValidationNode, ValidationNode>();
            var includeValidations = new List<ValidatedMappingNode>();

            var validations = new Dictionary<string, ValidationNode>();
            FieldValidators(validations, mapping, serialization, context);

            foreach (var fieldDefinition in BaseFieldDefinitions)
            {
                if (fieldDefinition.Attribute is not IncludeDataFieldAttribute) continue;

                var validationNode = validations[fieldDefinition.CamelCasedName];
                if (validationNode is ErrorNode errorNode)
                {
                    validatedMapping.Add(new InconclusiveNode(new ValueDataNode($"<{nameof(IncludeDataFieldAttribute)}={fieldDefinition.FieldInfo.Name}>")), errorNode);
                    continue;
                }

                if (validationNode is not ValidatedMappingNode validationMapping)
                {
                    throw new InvalidValidationNodeReturnedException<ValidatedMappingNode>(validationNode);
                }

                includeValidations.Add(validationMapping);
            }

            foreach (var (key, val) in mapping.Children)
            {
                if (!TryGetIndex(key, out var idx))
                {
                    if (TryGetIncludeMappingPair(includeValidations, key, out var validatedNotFoundPair))
                    {
                        validatedMapping.Add(validatedNotFoundPair.Key, validatedNotFoundPair.Value);
                        continue;
                    }

                    var error = new FieldNotFoundErrorNode(mapping.GetKeyNode(key), typeof(T));

                    validatedMapping.Add(error, new InconclusiveNode(val));
                    continue;
                }

                var keyValidated = serialization.ValidateNode<string>(mapping.GetKeyNode(key), context);

                ValidationNode valNode;
                if (IsNull(val))
                {
                    if (!NullableHelper.IsMarkedAsNullable(BaseFieldDefinitions[idx].FieldInfo))
                    {
                        var error = new ErrorNode(
                            val,
                            $"Field \"{key}\" had null value despite not being annotated as nullable.");

                        validatedMapping.Add(keyValidated, error);
                        continue;
                    }

                    valNode = new ValidatedValueNode(val);
                }
                else
                {
                    valNode = validations[key];
                }

                //include node errors override successful nodes on the main datadef
                if (valNode is not ErrorNode && TryGetIncludeMappingPair(includeValidations, key, out var validatedPair))
                {
                    if (validatedPair.Value is ErrorNode)
                    {
                        validatedMapping.Add(validatedPair.Key, validatedPair.Value);
                    }
                    continue;
                }

                validatedMapping.Add(keyValidated, valNode);
            }

            return new ValidatedMappingNode(validatedMapping);
        }

        public override bool TryGetDuplicates(out string[] duplicates)
        {
            duplicates = Duplicates;
            return duplicates.Length > 0;
        }

        private bool GatherFieldData(
            AbstractFieldInfo fieldInfo,
            out DataFieldBaseAttribute? dataFieldBaseAttribute,
            [NotNullWhen(true)] out AbstractFieldInfo? backingField,
            [NotNullWhen(true)] ref InheritanceBehavior? inheritanceBehavior)
        {
            dataFieldBaseAttribute = null;
            backingField = fieldInfo;
            inheritanceBehavior ??= InheritanceBehavior.Default;

            if (fieldInfo.HasAttribute<AlwaysPushInheritanceAttribute>(true))
                inheritanceBehavior = InheritanceBehavior.Always;
            else if (fieldInfo.HasAttribute<NeverPushInheritanceAttribute>(true))
                inheritanceBehavior = InheritanceBehavior.Never;

            if (fieldInfo is SpecificPropertyInfo propertyInfo)
            {
                // We only want the most overriden instance of a property for the type we are working with
                if (!propertyInfo.IsMostOverridden(typeof(T)))
                    return false;

                if (propertyInfo.PropertyInfo.GetMethod == null)
                {
                    Logger.ErrorS(LogCategory,
                        $"Property {propertyInfo} is annotated with DataFieldAttribute but has no getter");
                    return false;
                }
            }

            // Most data fields have an explicit data field attribute
            if (fieldInfo.TryGetAttribute<DataFieldAttribute>(out var dataFieldAttribute, true))
                return GatherDataFieldData(fieldInfo, out dataFieldBaseAttribute, ref backingField, dataFieldAttribute);

            if (fieldInfo.TryGetAttribute<IncludeDataFieldAttribute>(out var includeDataFieldAttribute, true))
            {
                dataFieldBaseAttribute = includeDataFieldAttribute;
                return true;
            }

            // This field/property has no explicit data field related annotations. However, things like
            // DataRecordAttribute will cause all fields to be interpreted as data fields, so we still handle them

            if (fieldInfo is not SpecificPropertyInfo)
                return true;

            var potentialBackingField = fieldInfo.GetBackingField();
            if (potentialBackingField == null)
                return false;

            return GatherFieldData(potentialBackingField,
                out dataFieldBaseAttribute,
                out backingField,
                ref inheritanceBehavior);
        }

        private static bool GatherDataFieldData(
            AbstractFieldInfo fieldInfo,
            out DataFieldBaseAttribute dataFieldBaseAttribute,
            ref AbstractFieldInfo backingField,
            DataFieldAttribute dataFieldAttribute)
        {
            dataFieldBaseAttribute = dataFieldAttribute;

            if (fieldInfo is not SpecificPropertyInfo property
                || dataFieldAttribute.ReadOnly
                || property.PropertyInfo.SetMethod != null)
            {
                return true;
            }

            if (!property.TryGetBackingField(out var backingFieldInfo))
            {
                Logger.ErrorS(LogCategory, $"Property {property} in type {property.DeclaringType} is annotated with DataFieldAttribute as non-readonly but has no auto-setter");
                return false;
            }

            backingField = backingFieldInfo;
            return true;
        }

        private List<FieldDefinition> GetFieldDefinitions(bool isRecord)
        {
#pragma warning disable CS0618
            var dummyObject = T.StaticInstantiate();
#pragma warning restore CS0618
            var fieldDefinitions = new List<FieldDefinition>();

            foreach (var abstractFieldInfo in typeof(T).GetAllPropertiesAndFields())
            {
                if (abstractFieldInfo.IsBackingField())
                    continue;

                if (isRecord && abstractFieldInfo.IsAutogeneratedRecordMember())
                    continue;

                InheritanceBehavior? inheritanceBehavior = InheritanceBehavior.Default;
                if (!GatherFieldData(abstractFieldInfo,
                        out var dataFieldBaseAttribute,
                        out var backingField,
                        ref inheritanceBehavior))
                    continue;

                if (dataFieldBaseAttribute == null)
                {
                    if (!isRecord)
                        continue;

                    dataFieldBaseAttribute = new DataFieldAttribute(CamelCaseNamingConvention.Instance.Apply(abstractFieldInfo.Name));
                }

                var fieldDefinition = new FieldDefinition(
                    dataFieldBaseAttribute,
                    abstractFieldInfo.GetValue(dummyObject),
                    abstractFieldInfo,
                    backingField,
                    inheritanceBehavior.Value);

                fieldDefinitions.Add(fieldDefinition);
            }

            // There should be no duplicates
            // I.e., we haven't accidentally included a property's backing field twice?
            DebugTools.Assert(fieldDefinitions.Select(x=> x.FieldInfo).Distinct().Count() == fieldDefinitions.Count);
            return fieldDefinitions;
        }
    }
}
