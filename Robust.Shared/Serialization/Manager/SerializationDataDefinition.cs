using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public class SerializationDataDefinition
    {
        private delegate DeserializedFieldEntry[] DeserializeDelegate(MappingDataNode mappingDataNode,
            ISerializationManager serializationManager, ISerializationContext? context);

        private delegate DeserializationResult PopulateDelegateSignature(object target, DeserializedFieldEntry[] deserializationResults, object?[] defaultValues);

        private delegate MappingDataNode SerializeDelegateSignature(object obj, ISerializationManager serializationManager,
            ISerializationContext? context, bool alwaysWrite, object?[] defaultValues);

        private delegate object CopyDelegateSignature(object source, object target,
            ISerializationManager serializationManager);

        public readonly Type Type;

        private readonly string[] _duplicates;
        private readonly FieldDefinition[] _baseFieldDefinitions;
        private readonly object?[] _defaultValues;

        private readonly DeserializeDelegate _deserializeDelegate;

        private readonly PopulateDelegateSignature _populateDelegate;

        private readonly SerializeDelegateSignature _serializeDelegate;

        private readonly CopyDelegateSignature _copyDelegate;

        public DeserializationResult InvokePopulateDelegate(object target, DeserializedFieldEntry[] fields) =>
            _populateDelegate(target, fields, _defaultValues);

        public DeserializationResult InvokePopulateDelegate(object target, MappingDataNode mappingDataNode, ISerializationManager serializationManager,
            ISerializationContext? context)
        {
            var fields = _deserializeDelegate(mappingDataNode, serializationManager, context);
            return _populateDelegate(target, fields, _defaultValues);
        }

        public MappingDataNode InvokeSerializeDelegate(object obj, ISerializationManager serializationManager, ISerializationContext? context, bool alwaysWrite) =>
            _serializeDelegate(obj, serializationManager, context, alwaysWrite, _defaultValues);

        public object InvokeCopyDelegate(object source, object target, ISerializationManager serializationManager) =>
            _copyDelegate(source, target, serializationManager);

        public bool CanCallWith(object obj) => Type.IsInstanceOfType(obj);

        public SerializationDataDefinition(Type type)
        {
            Type = type;
            var dummyObj = Activator.CreateInstance(type)!;

            var fieldDefs = new List<FieldDefinition>();

            foreach (var abstractFieldInfo in type.GetAllPropertiesAndFields())
            {
                var attr = abstractFieldInfo.GetCustomAttribute<DataFieldAttribute>();

                if (attr == null) continue;

                if (abstractFieldInfo is SpecificPropertyInfo propertyInfo)
                {
                    // We only want the most overriden instance of a property for the type we are working with
                    if (!propertyInfo.IsMostOverridden(type))
                    {
                        continue;
                    }

                    if (propertyInfo.PropertyInfo.GetMethod == null)
                    {
                        Logger.ErrorS("serialization", $"Property {propertyInfo} is annotated with DataFieldAttribute but has no getter");
                        continue;
                    }
                    else if (!attr.ReadOnly && propertyInfo.PropertyInfo.SetMethod == null)
                    {
                        Logger.ErrorS("serialization", $"Property {propertyInfo} is annotated with DataFieldAttribute as non-readonly but has no setter");
                        continue;
                    }
                }

                var alwaysPushInheritance =
                    abstractFieldInfo.GetCustomAttribute<AlwaysPushInheritanceAttribute>() != null;

                fieldDefs.Add(new FieldDefinition(attr, abstractFieldInfo.GetValue(dummyObj), abstractFieldInfo, alwaysPushInheritance));
            }

            _duplicates = fieldDefs
                .Where(f =>
                    fieldDefs.Count(df => df.Attribute.Tag == f.Attribute.Tag) > 1)
                .Select(f => f.Attribute.Tag)
                .Distinct()
                .ToArray();

            var fields = fieldDefs;
            //todo paul write a test for this
            fields.Sort((a, b) => a.Attribute.Priority.CompareTo(a.Attribute.Priority));
            _baseFieldDefinitions = fields.ToArray();
            _defaultValues = fieldDefs.Select(f => f.DefaultValue).ToArray();

            _deserializeDelegate = EmitDeserializationDelegate();
            _populateDelegate = EmitPopulateDelegate();
            _serializeDelegate = EmitSerializeDelegate();
            _copyDelegate = EmitCopyDelegate();
        }

        public int DataFieldCount => _baseFieldDefinitions.Length;

        public bool TryGetDuplicates([NotNullWhen(true)] out string[] duplicates)
        {
            duplicates = _duplicates;
            return duplicates.Length > 0;
        }

        private DeserializeDelegate EmitDeserializationDelegate()
        {
            DeserializedFieldEntry[] DeserializationDelegate(MappingDataNode mappingDataNode,
                ISerializationManager serializationManager, ISerializationContext? serializationContext)
            {
                var mappedInfo = new DeserializedFieldEntry[_baseFieldDefinitions.Length];

                for (var i = 0; i < _baseFieldDefinitions.Length; i++)
                {
                    var fieldDefinition = _baseFieldDefinitions[i];
                    var mapped = mappingDataNode.HasNode(fieldDefinition.Attribute.Tag);

                    if (!mapped)
                    {
                        mappedInfo[i] = new DeserializedFieldEntry(mapped);
                        continue;
                    }

                    DeserializationResult? result;

                    switch (fieldDefinition.Attribute)
                    {
                        case DataFieldWithConstantAttribute constantAttribute:
                        {
                            if (fieldDefinition.FieldType.EnsureNotNullableType() != typeof(int))
                                throw new InvalidOperationException();

                            var type = constantAttribute.ConstantTag;
                            var node = mappingDataNode.GetNode(fieldDefinition.Attribute.Tag);
                            var constant = serializationManager.ReadConstant(type, node);

                            result = new DeserializedValue<int>(constant);
                            break;
                        }
                        case DataFieldWithFlagAttribute flagAttribute:
                        {
                            if (fieldDefinition.FieldType.EnsureNotNullableType() != typeof(int)) throw new InvalidOperationException();

                            var type = flagAttribute.FlagTag;
                            var node = mappingDataNode.GetNode(fieldDefinition.Attribute.Tag);
                            var flag = serializationManager.ReadFlag(type, node);

                            result = new DeserializedValue<int>(flag);
                            break;
                        }
                        default:
                        {
                            var type = fieldDefinition.FieldType;
                            var node = mappingDataNode.GetNode(fieldDefinition.Attribute.Tag);
                            result = serializationManager.Read(type, node, serializationContext);
                            break;
                        }
                    }

                    var entry = new DeserializedFieldEntry(mapped, result, fieldDefinition.AlwaysPushInheritanceFlag);
                    mappedInfo[i] = entry;
                }

                return mappedInfo;
            }

            return DeserializationDelegate;
        }

        // TODO PAUL SERV3: Turn this back into IL once it is fixed
        private PopulateDelegateSignature EmitPopulateDelegate()
        {
            DeserializationResult PopulateDelegate(object target, DeserializedFieldEntry[] deserializedFields, object?[] defaultValues)
            {
                for (var i = 0; i < _baseFieldDefinitions.Length; i++)
                {
                    var res = deserializedFields[i];
                    if (!res.Mapped) continue;

                    var fieldDefinition = _baseFieldDefinitions[i];

                    var defValue = defaultValues[i];

                    if (Equals(res.Result?.RawValue, defValue))
                    {
                        continue;
                    }

                    fieldDefinition.FieldInfo.SetValue(target, res.Result?.RawValue);
                }

                return DeserializationResult.Definition(target, deserializedFields);
            }

            return PopulateDelegate;
        }

        private SerializeDelegateSignature EmitSerializeDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_serializeDelegate<>{Type}",
                typeof(MappingDataNode),
                new[] {typeof(object), typeof(ISerializationManager), typeof(ISerializationContext), typeof(bool), typeof(object?[])},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "serializationManager");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serializationContext");
            dynamicMethod.DefineParameter(4, ParameterAttributes.In, "alwaysWrite");
            dynamicMethod.DefineParameter(5, ParameterAttributes.In, "defaultValues");
            var generator = dynamicMethod.GetRobustGen();

            var loc = generator.DeclareLocal(typeof(MappingDataNode));
            Debug.Assert(loc.LocalIndex == 0);
            generator.Emit(OpCodes.Newobj, typeof(MappingDataNode).GetConstructor(new Type[0])!);
            generator.Emit(OpCodes.Stloc_0);

            for (var i = _baseFieldDefinitions.Length-1; i >= 0; i--)
            {
                var fieldDefinition = _baseFieldDefinitions[i];
                generator.EmitSerializeField(fieldDefinition, i);
            }

            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<SerializeDelegateSignature>();
        }

        private CopyDelegateSignature EmitCopyDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_populateDelegate<>{Type}",
                typeof(object),
                new[] {typeof(object), typeof(object), typeof(ISerializationManager)},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "source");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "target");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            var generator = dynamicMethod.GetRobustGen();

            foreach (var fieldDefinition in _baseFieldDefinitions)
            {
                generator.EmitCopy(0, fieldDefinition.FieldInfo, 1, fieldDefinition.FieldInfo, 2);
            }

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<CopyDelegateSignature>();
        }

        public class FieldDefinition
        {
            public readonly DataFieldAttribute Attribute;
            public readonly object? DefaultValue;
            public readonly AbstractFieldInfo FieldInfo;
            public readonly bool AlwaysPushInheritanceFlag;

            public FieldDefinition(DataFieldAttribute attr, object? defaultValue, AbstractFieldInfo fieldInfo, bool alwaysPushInheritanceFlag)
            {
                Attribute = attr;
                DefaultValue = defaultValue;
                FieldInfo = fieldInfo;
                AlwaysPushInheritanceFlag = alwaysPushInheritanceFlag;
            }

            public Type FieldType => FieldInfo.FieldType;
        }
    }
}
