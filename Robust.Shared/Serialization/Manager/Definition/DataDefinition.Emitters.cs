using System;
using System.Reflection;
using System.Reflection.Emit;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager.Definition
{
    public partial class DataDefinition
    {
        private PopulateDelegateSignature EmitPopulateDelegate(IDependencyCollection collection)
        {
            var isServer = collection.Resolve<INetManager>().IsServer;

            object PopulateDelegate(
                object target,
                MappingDataNode mappingDataNode,
                ISerializationManager serializationManager,
                ISerializationContext? serializationContext,
                bool skipHook,
                object?[] defaultValues)
            {
                for (var i = 0; i < BaseFieldDefinitions.Length; i++)
                {
                    var fieldDefinition = BaseFieldDefinitions[i];

                    if (fieldDefinition.Attribute.ServerOnly && !isServer)
                    {
                        continue;
                    }

                    if (fieldDefinition.Attribute is DataFieldAttribute dfa && !mappingDataNode.Has(dfa.Tag))
                    {
                        if (dfa.Required)
                            throw new InvalidOperationException($"Required field {dfa.Tag} of type {target.GetType()} wasn't mapped.");
                        continue;
                    }

                    var type = fieldDefinition.FieldType;
                    var node = fieldDefinition.Attribute is DataFieldAttribute dfa2 ? mappingDataNode.Get(dfa2.Tag) : mappingDataNode;
                    object? result;
                    if (fieldDefinition.Attribute.CustomTypeSerializer != null && node switch
                        {
                            ValueDataNode => FieldInterfaceInfos[i].Reader.Value,
                            SequenceDataNode => FieldInterfaceInfos[i].Reader.Sequence,
                            MappingDataNode => FieldInterfaceInfos[i].Reader.Mapping,
                            _ => throw new InvalidOperationException()
                        })
                    {
                        result = serializationManager.ReadWithTypeSerializer(type,
                            fieldDefinition.Attribute.CustomTypeSerializer, node, serializationContext, skipHook);
                    }
                    else
                    {
                        result = serializationManager.Read(type, node, serializationContext, skipHook);
                    }

                    var defValue = defaultValues[i];

                    if (Equals(result, defValue))
                    {
                        continue;
                    }

                    FieldAssigners[i](ref target, result);
                }

                return target;
            }

            return PopulateDelegate;
        }

        private SerializeDelegateSignature EmitSerializeDelegate(IDependencyCollection collection)
        {
            MappingDataNode SerializeDelegate(
                object obj,
                ISerializationManager manager,
                ISerializationContext? context,
                bool alwaysWrite,
                object?[] defaultValues)
            {
                var mapping = new MappingDataNode();

                for (var i = BaseFieldDefinitions.Length - 1; i >= 0; i--)
                {
                    var fieldDefinition = BaseFieldDefinitions[i];

                    if (fieldDefinition.Attribute.ReadOnly)
                    {
                        continue;
                    }

                    if (fieldDefinition.Attribute is DataFieldAttribute dfa1 && mapping.Has(dfa1.Tag))
                    {
                        continue; //this node was already written by a type higher up the includetree
                    }

                    if (fieldDefinition.Attribute.ServerOnly &&
                        !collection.Resolve<INetManager>().IsServer)
                    {
                        continue;
                    }

                    var value = FieldAccessors[i](ref obj);

                    if (value == null)
                    {
                        continue;
                    }

                    if (fieldDefinition.Attribute is not DataFieldAttribute { Required: true } &&
                        !alwaysWrite &&
                        Equals(value, defaultValues[i]))
                    {
                        continue;
                    }


                    var type = fieldDefinition.FieldType;

                    DataNode node;
                    if (fieldDefinition.Attribute.CustomTypeSerializer != null && FieldInterfaceInfos[i].Writer)
                    {
                        node = manager.WriteWithTypeSerializer(type, fieldDefinition.Attribute.CustomTypeSerializer,
                            value, alwaysWrite, context);
                    }
                    else
                    {
                        node = manager.WriteValue(type, value, alwaysWrite, context);
                    }

                    if (fieldDefinition.Attribute is not DataFieldAttribute dfa)
                    {
                        if (node is not MappingDataNode nodeMapping)
                        {
                            throw new InvalidOperationException(
                                $"Writing field {fieldDefinition} for type {Type} did not return a {nameof(MappingDataNode)} but was annotated to be included.");
                        }

                        mapping.Insert(nodeMapping, skipDuplicates: true);
                    }
                    else
                    {
                        mapping[dfa.Tag] = node;
                    }
                }

                return mapping;
            }

            return SerializeDelegate;
        }

        // TODO Serialization: add skipHook
        private CopyDelegateSignature EmitCopyDelegate()
        {
            object CopyDelegate(
                object source,
                object target,
                ISerializationManager manager,
                ISerializationContext? context)
            {
                for (var i = 0; i < BaseFieldDefinitions.Length; i++)
                {
                    var field = BaseFieldDefinitions[i];
                    var accessor = FieldAccessors[i];
                    var sourceValue = accessor(ref source);
                    var targetValue = accessor(ref target);

                    object? copy;
                    if (sourceValue != null &&
                        targetValue != null &&
                        !TypeHelpers.TrySelectCommonType(sourceValue.GetType(), targetValue.GetType(), out _))
                    {
                        copy = manager.Copy(sourceValue, context);
                    }
                    else
                    {
                        if (field.Attribute.CustomTypeSerializer != null && FieldInterfaceInfos[i].Copier)
                        {
                            copy = manager.CopyWithTypeSerializer(field.Attribute.CustomTypeSerializer, sourceValue,
                                targetValue,
                                context);
                        }
                        else
                        {
                            copy = targetValue;
                            manager.Copy(sourceValue, ref copy, context);
                        }
                    }

                    FieldAssigners[i](ref target, copy);
                }

                return target;
            }

            return CopyDelegate;
        }

        private static void EmitSetField(RobustILGenerator rGenerator, AbstractFieldInfo info)
        {
            switch (info)
            {
                case SpecificFieldInfo field:
                    rGenerator.Emit(OpCodes.Stfld, field.FieldInfo);
                    break;
                case SpecificPropertyInfo property:
                    var setter = property.PropertyInfo.GetSetMethod(true) ?? throw new NullReferenceException();

                    var opCode = info.DeclaringType?.IsValueType ?? false
                        ? OpCodes.Call
                        : OpCodes.Callvirt;

                    rGenerator.Emit(opCode, setter);
                    break;
            }
        }

        private AccessField<object, object?> EmitFieldAccessor(FieldDefinition fieldDefinition)
        {
            var method = new DynamicMethod(
                "AccessField",
                typeof(object),
                new[] {typeof(object).MakeByRefType()},
                true);

            method.DefineParameter(1, ParameterAttributes.Out, "target");

            var generator = method.GetRobustGen();

            if (Type.IsValueType)
            {
                generator.DeclareLocal(Type);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldind_Ref);
                generator.Emit(OpCodes.Unbox_Any, Type);
                generator.Emit(OpCodes.Stloc_0);
                generator.Emit(OpCodes.Ldloca_S, 0);
            }
            else
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldind_Ref);
                generator.Emit(OpCodes.Castclass, Type);
            }

            switch (fieldDefinition.BackingField)
            {
                case SpecificFieldInfo field:
                    generator.Emit(OpCodes.Ldfld, field.FieldInfo);
                    break;
                case SpecificPropertyInfo property:
                    var getter = property.PropertyInfo.GetGetMethod(true) ?? throw new NullReferenceException();
                    var opCode = Type.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
                    generator.Emit(opCode, getter);
                    break;
            }

            var returnType = fieldDefinition.BackingField.FieldType;
            if (returnType.IsValueType)
            {
                generator.Emit(OpCodes.Box, returnType);
            }

            generator.Emit(OpCodes.Ret);

            return method.CreateDelegate<AccessField<object, object?>>();
        }

        internal static AssignField<T, object?> EmitFieldAssigner<T>(Type type, Type fieldType, AbstractFieldInfo backingField)
        {
            var method = new DynamicMethod(
                "AssignField",
                typeof(void),
                new[] {typeof(T).MakeByRefType(), typeof(object)},
                true);

            method.DefineParameter(1, ParameterAttributes.Out, "target");
            method.DefineParameter(2, ParameterAttributes.None, "value");

            var generator = method.GetRobustGen();

            if (type.IsValueType)
            {
                generator.DeclareLocal(type);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldind_Ref);
                generator.Emit(OpCodes.Unbox_Any, type);
                generator.Emit(OpCodes.Stloc_0);
                generator.Emit(OpCodes.Ldloca, 0);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Unbox_Any, fieldType);

                EmitSetField(generator, backingField);

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Box, type);
                generator.Emit(OpCodes.Stind_Ref);

                generator.Emit(OpCodes.Ret);
            }
            else
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldind_Ref);
                generator.Emit(OpCodes.Castclass, type);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Unbox_Any, fieldType);

                EmitSetField(generator, backingField.GetBackingField() ?? backingField);

                generator.Emit(OpCodes.Ret);
            }

            return method.CreateDelegate<AssignField<T, object?>>();
        }
    }
}
