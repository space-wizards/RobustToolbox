using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public class SerializationDataDefinition
    {
        private delegate object PopulateDelegateSignature(object target, MappingDataNode mappingDataNode, ISerializationManager serializationManager,
            ISerializationContext? context, object?[] defaultValues);

        private delegate MappingDataNode SerializeDelegateSignature(object obj, ISerializationManager serializationManager,
            ISerializationContext? context, bool alwaysWrite, object?[] defaultValues);
        public delegate object CopyDelegateSignature(object source, object target,
            ISerializationManager serializationManager);

        public readonly Type Type;

        private readonly string[] _duplicates;
        private readonly FieldDefinition[] _baseFieldDefinitions;
        private readonly object?[] _defaultValues;

        private readonly PopulateDelegateSignature _populateDelegate;

        private readonly SerializeDelegateSignature _serializeDelegate;

        public readonly CopyDelegateSignature _copyDelegate;

        public object InvokePopulateDelegate(object target, MappingDataNode mappingDataNode, ISerializationManager serializationManager,
            ISerializationContext? context) =>
            _populateDelegate(target, mappingDataNode, serializationManager, context, _defaultValues);

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
                    // TODO paul this is most definitely 100.10% wrong help
                    // We only want the most overriden instance of a property for the type we are working with
                    if (propertyInfo.IsOverridenIn(type))
                    {
                        continue;
                    }

                    if (propertyInfo.PropertyInfo.GetMethod == null)
                    {
                        Logger.ErrorS("SerV3", $"Property {propertyInfo} is annotated with DataFieldAttribute but has no getter");
                        continue;
                    }
                    else if (!attr.ReadOnly && propertyInfo.PropertyInfo.SetMethod == null)
                    {
                        Logger.ErrorS("SerV3", $"Property {propertyInfo} is annotated with DataFieldAttribute as non-readonly but has no setter");
                        continue;
                    }
                }

                fieldDefs.Add(new FieldDefinition(attr, abstractFieldInfo.GetValue(dummyObj), abstractFieldInfo));
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

            _populateDelegate = EmitPopulateDelegate();
            _serializeDelegate = EmitSerializeDelegate();
            _copyDelegate = EmitCopyDelegate();
        }

        public bool TryGetDuplicates([NotNullWhen(true)] out string[] duplicates)
        {
            duplicates = _duplicates;
            return duplicates.Length > 0;
        }

        // TODO PAUL SERV3: Turn this back into IL once it is fixed
        private PopulateDelegateSignature EmitPopulateDelegate()
        {
            object PopulateDelegate(object target, MappingDataNode mappingDataNode, ISerializationManager serv3Manager,
                ISerializationContext? context, object?[] defaultValues)
            {
                for (var i = 0; i < _baseFieldDefinitions.Length; i++)
                {
                    var fieldDefinition = _baseFieldDefinitions[i];
                    var mapped = mappingDataNode.HasNode(fieldDefinition.Attribute.Tag);

                    if (!mapped)
                    {
                        continue;
                    }

                    object fieldVal;

                    switch (fieldDefinition.Attribute)
                    {
                        case DataFieldWithConstantAttribute constant:
                        {
                            if (fieldDefinition.FieldType.EnsureNotNullableType() != typeof(int))
                                throw new InvalidOperationException();

                            var type = constant.ConstantTag;

                            var node = mappingDataNode.GetNode(fieldDefinition.Attribute.Tag);

                            fieldVal = serv3Manager.ReadConstant(type, node);
                            break;
                        }
                        case DataFieldWithFlagAttribute flag:
                        {
                            if (fieldDefinition.FieldType.EnsureNotNullableType() != typeof(int)) throw new InvalidOperationException();

                            var type = flag.FlagTag;

                            var node = mappingDataNode.GetNode(fieldDefinition.Attribute.Tag);

                            fieldVal = serv3Manager.ReadFlag(type, node);
                            break;
                        }
                        default:
                        {
                            var type = fieldDefinition.FieldType;

                            var node = mappingDataNode.GetNode(fieldDefinition.Attribute.Tag);

                            fieldVal = serv3Manager.ReadValue(type, node);
                            break;
                        }
                    }

                    var defValue = defaultValues[i];

                    if (fieldVal.Equals(defValue))
                    {
                        continue;
                    }

                    fieldDefinition.FieldInfo.SetValue(target, fieldVal);
                }

                return target;
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

            public FieldDefinition(DataFieldAttribute attr, object? defaultValue, AbstractFieldInfo fieldInfo)
            {
                Attribute = attr;
                DefaultValue = defaultValue;
                FieldInfo = fieldInfo;
            }

            public Type FieldType => FieldInfo.FieldType;
        }
    }
}
