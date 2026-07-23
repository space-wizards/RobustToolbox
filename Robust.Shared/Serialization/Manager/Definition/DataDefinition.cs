using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using static Robust.Shared.Serialization.Manager.SerializationManager;

namespace Robust.Shared.Serialization.Manager.Definition
{
    public abstract class DataDefinition
    {
#pragma warning disable CS0618
        internal ImmutableArray<DataFieldDefinition> BaseFieldDefinitions { get; init; }
        internal bool IsRecord { get; init; }
        internal abstract PopulateDelegateSignature<object> PopulateObj { get; init; }
        internal abstract ISerializationManager.InstantiationDelegate<object> InstantiateObj { get; init; }
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
        internal override ISerializationManager.InstantiationDelegate<object> InstantiateObj { get; init; }

        [UsedImplicitly]
        internal DataDefinition(SerializationManager manager, bool isRecord)
        {
            IsRecord = isRecord;

#pragma warning disable CS0618
            var fieldDefs = new List<DataFieldDefinition>();
            T.GetFieldDefinitions(default, fieldDefs);
#pragma warning restore CS0618
            for (var i = 0; i < fieldDefs.Count; i++)
            {
                var field = fieldDefs[i];
                if (field is { IsDataField: true, Tag: null })
                    field.Tag = DataDefinitionUtility.AutoGenerateTag(field.FieldInfoName);
            }

            fieldDefs.Sort((a, b) =>
            {
                var priority = b.Priority.CompareTo(a.Priority);
                if (priority != 0)
                    return priority;

                return b.FieldInfoName.CompareTo(a.FieldInfoName, StringComparison.OrdinalIgnoreCase);
            });

            var dataFields = fieldDefs
                .Where(f => f.IsDataField)
                .Select(f => f.Tag ?? f.CamelCasedName)
                .ToArray();

            Duplicates = dataFields
                .Where(f =>
                    dataFields.Count(df => df == f) > 1)
                .Distinct()
                .ToArray();

            BaseFieldDefinitions = fieldDefs.ToImmutableArray();

            DefaultValues = fieldDefs.Select(f => f.DefaultValue).ToImmutableArray();
            DefaultValuesDict = fieldDefs.ToImmutableDictionary(
                f => f.IsDataField
                    ? f.Tag ?? f.CamelCasedName
                    : f.CamelCasedName,
                f => f.DefaultValue
            );

            //has to be done after fieldinterfaceinfos are done
#pragma warning disable CS0618
            // TODO source gen this one too!
            PopulateObj = (ref target, node, serialization, ctx, context) =>
            {
                var obj = (T) target;
                T.Read(ref obj, node, serialization, ctx, context);
                target = obj;
            };

            Populate = T.Read;
            Serialize = T.Write;
            CopyTo = (source, ref target, ctx, context) => source.Copy(ref target, manager, ctx, context); // TODO source gen this one too!
            FieldValidators = T.Validate;
            InstantiateObj = T.StaticInstantiateObject;
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
                var field = BaseFieldDefinitions[index];
                if (field.IsDataField && field.Tag == tag)
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
                if (!fieldDefinition.IsIncludeDataField) continue;

                var validationNode = validations[fieldDefinition.CamelCasedName];
                if (validationNode is ErrorNode errorNode)
                {
                    validatedMapping.Add(new InconclusiveNode(new ValueDataNode($"<{nameof(IncludeDataFieldAttribute)}={fieldDefinition.FieldInfoName}>")), errorNode);
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
                    if (!BaseFieldDefinitions[idx].FieldInfoNullable)
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
    }
}
