using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager.Definition
{
    public partial class DataDefinition
    {
        private DeserializeDelegate EmitDeserializationDelegate()
        {
            DeserializedFieldEntry[] DeserializationDelegate(MappingDataNode mappingDataNode,
                ISerializationManager serializationManager, ISerializationContext? serializationContext, bool skipHook)
            {
                var mappedInfo = new DeserializedFieldEntry[BaseFieldDefinitions.Length];

                for (var i = 0; i < BaseFieldDefinitions.Length; i++)
                {
                    var fieldDefinition = BaseFieldDefinitions[i];

                    if (fieldDefinition.Attribute.ServerOnly && !IoCManager.Resolve<INetManager>().IsServer)
                    {
                        mappedInfo[i] = new DeserializedFieldEntry(false, fieldDefinition.InheritanceBehavior);
                        continue;
                    }

                    var mapped = mappingDataNode.Has(fieldDefinition.Attribute.Tag);

                    if (!mapped)
                    {
                        mappedInfo[i] = new DeserializedFieldEntry(mapped, fieldDefinition.InheritanceBehavior);
                        continue;
                    }

                    var type = fieldDefinition.FieldType;
                    var node = mappingDataNode.Get(fieldDefinition.Attribute.Tag);
                    DeserializationResult result;
                    if (fieldDefinition.Attribute.CustomTypeSerializer != null)
                    {
                        var foundInterface = false;
                        foreach (var @interface in fieldDefinition.Attribute.CustomTypeSerializer.GetInterfaces())
                        {
                            if(@interface.GetGenericTypeDefinition() != typeof(ITypeReader<,>)) continue;
                            if (@interface.GenericTypeArguments[0] == type && @interface.GenericTypeArguments[1] == node.GetType())
                            {
                                foundInterface = true;
                            }
                        }

                        if (!foundInterface)
                        {
                            throw new InvalidOperationException(
                                $"Could not find implementation of ITypeReader for type {type} and node {node.GetType()} on customtypeserializer {fieldDefinition.Attribute.CustomTypeSerializer}");
                        }

                        result = serializationManager.ReadWithTypeSerializer(type,
                            fieldDefinition.Attribute.CustomTypeSerializer, node, serializationContext, skipHook);
                    }
                    else
                    {
                        result = serializationManager.Read(type, node, serializationContext, skipHook);
                    }

                    var entry = new DeserializedFieldEntry(mapped, fieldDefinition.InheritanceBehavior, result);
                    mappedInfo[i] = entry;
                }

                return mappedInfo;
            }

            return DeserializationDelegate;
        }

        private PopulateDelegateSignature EmitPopulateDelegate()
        {
            // TODO Serialization: validate mappings array count
            var constructor =
                typeof(DeserializedDefinition<>).MakeGenericType(Type).GetConstructor(new[] {Type, typeof(DeserializedFieldEntry[])}) ??
                throw new NullReferenceException();

            var valueParam = Expression.Parameter(typeof(object), "value");
            var valueParamCast = Expression.Convert(valueParam, Type);

            var mappingParam = Expression.Parameter(typeof(DeserializedFieldEntry[]), "mapping");

            var newExp = Expression.New(constructor, valueParamCast, mappingParam);
            var createDefinitionDelegate = Expression.Lambda<CreateDefinitionDelegate>(newExp, valueParam, mappingParam).Compile();

            DeserializationResult PopulateDelegate(
                object target,
                DeserializedFieldEntry[] deserializedFields,
                object?[] defaultValues)
            {
                for (var i = 0; i < BaseFieldDefinitions.Length; i++)
                {
                    var res = deserializedFields[i];
                    if (!res.Mapped) continue;

                    var defValue = defaultValues[i];

                    if (Equals(res.Result?.RawValue, defValue))
                    {
                        continue;
                    }

                    FieldAssigners[i](ref target, res.Result?.RawValue);
                }

                return createDefinitionDelegate(target, deserializedFields);
            }

            return PopulateDelegate;
        }

        private SerializeDelegateSignature EmitSerializeDelegate()
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

                    if (fieldDefinition.Attribute.ServerOnly &&
                        !IoCManager.Resolve<INetManager>().IsServer)
                    {
                        continue;
                    }

                    var value = FieldAccessors[i](ref obj);

                    if (value == null)
                    {
                        continue;
                    }

                    if (!fieldDefinition.Attribute.Required &&
                        !alwaysWrite &&
                        Equals(value, defaultValues[i]))
                    {
                        continue;
                    }

                    var type = fieldDefinition.FieldType;

                    DataNode node;
                    if (fieldDefinition.Attribute.CustomTypeSerializer != null)
                    {
                        var foundInterface = false;
                        foreach (var @interface in fieldDefinition.Attribute.CustomTypeSerializer.GetInterfaces())
                        {
                            if(@interface.GetGenericTypeDefinition() != typeof(ITypeWriter<>)) continue;
                            if (@interface.GenericTypeArguments[0] == type)
                            {
                                foundInterface = true;
                            }
                        }

                        if (!foundInterface)
                        {
                            throw new InvalidOperationException(
                                $"Could not find implementation of ITypeWriter for type {type} on customtypeserializer {fieldDefinition.Attribute.CustomTypeSerializer}");
                        }

                        node = manager.WriteWithTypeSerializer(type, fieldDefinition.Attribute.CustomTypeSerializer,
                            value, alwaysWrite, context);
                    }
                    else
                    {
                        node = manager.WriteValue(type, value, alwaysWrite, context);
                    }

                    mapping[fieldDefinition.Attribute.Tag] = node;
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
                        TypeHelpers.SelectCommonType(sourceValue.GetType(), targetValue.GetType()) == null)
                    {
                        copy = manager.CreateCopy(sourceValue, context);
                    }
                    else
                    {
                        copy = field.Attribute.CustomTypeSerializer != null
                            ? manager.CopyWithTypeSerializer(field.Attribute.CustomTypeSerializer, sourceValue,
                                targetValue,
                                context)
                            : manager.Copy(sourceValue, targetValue, context);
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

                EmitSetField(generator, backingField);

                generator.Emit(OpCodes.Ret);
            }

            return method.CreateDelegate<AssignField<T, object?>>();
        }
    }
}
