using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public static class SerializationILExtensions
    {
        // object target, MappingDataNode mappingDataNode, IServ3Manager serializationManager, ISerializationContext? context, object?[] defaultValues
        public static void EmitPopulateField(this RobustILGenerator generator,
            SerializationDataDefinition.FieldDefinition fieldDefinition, int localIdx, int defaultValueIdx)
        {
            /* todo paul
        public readonly bool ServerOnly;
             */

            var endLabel = generator.DefineLabel();

            generator.Emit(OpCodes.Ldarg_1); //load mappingnode
            generator.Emit(OpCodes.Ldstr, fieldDefinition.Attribute.Tag); //load tag
            var hasNodeMethod = typeof(MappingDataNode).GetMethods().First(m =>
                m.Name == nameof(MappingDataNode.HasNode) &&
                m.GetParameters().First().ParameterType == typeof(string));
            generator.Emit(OpCodes.Callvirt, hasNodeMethod); //checking if our node is mapped
            var notMappedLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Brfalse, notMappedLabel);

            generator.Emit(OpCodes.Ldarg_2); //load serializationmanager

            var getNodeMethod = typeof(MappingDataNode).GetMethods().First(m =>
                m.Name == nameof(MappingDataNode.GetNode) &&
                m.GetParameters().First().ParameterType == typeof(string));
            switch (fieldDefinition.Attribute)
            {
                case DataFieldWithConstantAttribute constantAttribute:
                    if (fieldDefinition.FieldType.EnsureNotNullableType() != typeof(int)) throw new InvalidOperationException();

                    // getting the type //
                    generator.Emit(OpCodes.Ldtoken, constantAttribute.ConstantTag);
                    generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);

                    // getting the node //
                    generator.Emit(OpCodes.Ldarg_1); //load mappingnode
                    generator.Emit(OpCodes.Ldstr, fieldDefinition.Attribute.Tag); //loading the tag
                    generator.Emit(OpCodes.Callvirt, getNodeMethod); //getting the node

                    var readConstMethod = typeof(ISerializationManager).GetMethod(nameof(ISerializationManager.ReadConstant));
                    Debug.Assert(readConstMethod != null, nameof(readConstMethod) + " != null");
                    generator.Emit(OpCodes.Callvirt, readConstMethod);
                    break;
                case DataFieldWithFlagAttribute flagAttribute:
                    if (fieldDefinition.FieldType.EnsureNotNullableType() != typeof(int)) throw new InvalidOperationException();

                    // getting the type //
                    generator.Emit(OpCodes.Ldtoken, flagAttribute.FlagTag);
                    generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);

                    // getting the node //
                    generator.Emit(OpCodes.Ldarg_1); //load mappingnode
                    generator.Emit(OpCodes.Ldstr, fieldDefinition.Attribute.Tag); //loading the tag
                    generator.Emit(OpCodes.Callvirt, getNodeMethod); //getting the node

                    var readFlagMethod = typeof(ISerializationManager).GetMethod(nameof(ISerializationManager.ReadFlag));
                    Debug.Assert(readFlagMethod != null, nameof(readFlagMethod) + " != null");
                    generator.Emit(OpCodes.Callvirt, readFlagMethod);
                    break;
                default:
                    // getting the type //
                    generator.Emit(OpCodes.Ldtoken, fieldDefinition.FieldType);
                    generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);

                    // getting the node //
                    generator.Emit(OpCodes.Ldarg_1); //load mappingnode
                    generator.Emit(OpCodes.Ldstr, fieldDefinition.Attribute.Tag); //loading the tag
                    generator.Emit(OpCodes.Callvirt, getNodeMethod); //getting the node

                    generator.Emit(OpCodes.Ldarg_3); //loading context

                    var readValueMethod = typeof(ISerializationManager).GetMethods().First(m =>
                        m.Name == nameof(ISerializationManager.Read) && m.GetParameters().Length == 3);
                    generator.Emit(OpCodes.Callvirt, readValueMethod); //reads node into our desired value

                    //unbox the value if necessary since ReadValue returns a object
                    //if(fieldDefinition.FieldType.IsValueType)
                    generator.Emit(OpCodes.Unbox_Any, fieldDefinition.FieldType);
                    break;
            }

            //storing the return value so we can populate it into our field later
            generator.Emit(OpCodes.Stloc, localIdx);

            //loading back our value
            generator.Emit(OpCodes.Ldloc, localIdx);
            if(!fieldDefinition.FieldType.IsPrimitive && fieldDefinition.FieldType.IsValueType)
                generator.Emit(OpCodes.Box, fieldDefinition.FieldType);

            //loading default value
            generator.Emit(OpCodes.Ldarg, 4);
            generator.Emit(OpCodes.Ldc_I4, defaultValueIdx);
            generator.Emit(OpCodes.Ldelem, fieldDefinition.FieldType);
            if(!fieldDefinition.FieldType.IsPrimitive && fieldDefinition.FieldType.IsValueType)
                generator.Emit(OpCodes.Box, fieldDefinition.FieldType);
            //generator.Emit(OpCodes.Unbox_Any, fieldDefinition.FieldType.EnsureNotNullableType());

            //checking if the value is default. if so, we skip setting the field
            generator.EmitEquals(fieldDefinition.FieldType, endLabel);

            //load object
            generator.Emit(OpCodes.Ldarg_0);
            //load value
            generator.Emit(OpCodes.Ldloc, localIdx);
            //setting the field
            generator.EmitStfld(fieldDefinition.FieldInfo);
            generator.Emit(OpCodes.Br, endLabel);

            generator.MarkLabel(notMappedLabel);

            if(fieldDefinition.Attribute.Required)
            {
                generator.ThrowException(typeof(RequiredDataFieldNotProvidedException));
            }

            generator.MarkLabel(endLabel);
        }

        // object obj, IServ3Manager serializationManager, ISerializationContext? context, bool alwaysWrite, object?[] defaultValues
        public static void EmitSerializeField(this RobustILGenerator generator,
            SerializationDataDefinition.FieldDefinition fieldDefinition, int defaultValueIdx)
        {
            if(fieldDefinition.Attribute.ReadOnly) return; //hehe ez pz

            /* todo paul
public readonly bool ServerOnly;
 */

            var locIdx = generator.DeclareLocal(fieldDefinition.FieldType).LocalIndex;

            var endLabel = generator.DefineLabel();

            // loading value into loc_idx
            generator.Emit(OpCodes.Ldarg_0);
            generator.EmitLdfld(fieldDefinition.FieldInfo);
            generator.Emit(OpCodes.Stloc, locIdx);

            //only do defaultcheck if we aren't required
            if (!fieldDefinition.Attribute.Required)
            {
                var skipDefaultCheckLabel = generator.DefineLabel();

                generator.Emit(OpCodes.Ldarg, 3); //load alwaysWrite bool
                generator.Emit(OpCodes.Brtrue_S, skipDefaultCheckLabel); //skip defaultcheck if alwayswrite is true

                //skip all of this if the value is default
                generator.Emit(OpCodes.Ldloc, locIdx); //load val
                if(!fieldDefinition.FieldType.IsPrimitive && fieldDefinition.FieldType.IsValueType)
                    generator.Emit(OpCodes.Box, fieldDefinition.FieldType);
                //load default value
                generator.Emit(OpCodes.Ldarg, 4);
                generator.Emit(OpCodes.Ldc_I4, defaultValueIdx);
                generator.Emit(OpCodes.Ldelem, fieldDefinition.FieldType);
                if(!fieldDefinition.FieldType.IsPrimitive && fieldDefinition.FieldType.IsValueType)
                    generator.Emit(OpCodes.Box, fieldDefinition.FieldType);
                //check if value is default
                generator.EmitEquals(fieldDefinition.FieldType, endLabel);

                generator.MarkLabel(skipDefaultCheckLabel);
            }

            generator.Emit(OpCodes.Ldloc_0); //loading mappingnode for calling addnode
            generator.Emit(OpCodes.Ldstr, fieldDefinition.Attribute.Tag); //loading tag for addnode

            generator.Emit(OpCodes.Ldarg_1); //load serializationManager

            switch (fieldDefinition.Attribute)
            {
                case DataFieldWithConstantAttribute constantAttribute:
                    if (fieldDefinition.FieldType.EnsureNotNullableType() != typeof(int)) throw new InvalidOperationException();

                    // load type //
                    generator.Emit(OpCodes.Ldtoken, constantAttribute.ConstantTag);
                    generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);

                    generator.Emit(OpCodes.Ldloc, locIdx); //load value

                    var writeConstantMethod = typeof(ISerializationManager).GetMethod(nameof(ISerializationManager.WriteConstant));
                    Debug.Assert(writeConstantMethod != null, nameof(writeConstantMethod) + " != null");
                    generator.Emit(OpCodes.Callvirt, writeConstantMethod);
                    break;
                case DataFieldWithFlagAttribute flagAttribute:
                    if (fieldDefinition.FieldType.EnsureNotNullableType() != typeof(int)) throw new InvalidOperationException();

                    // load type //
                    generator.Emit(OpCodes.Ldtoken, flagAttribute.FlagTag);
                    generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);

                    generator.Emit(OpCodes.Ldloc, locIdx); //load value

                    var writeFlagMethod = typeof(ISerializationManager).GetMethod(nameof(ISerializationManager.WriteFlag));
                    Debug.Assert(writeFlagMethod != null, nameof(writeConstantMethod) + " != null");
                    generator.Emit(OpCodes.Callvirt, writeFlagMethod);
                    break;
                default:
                    // load type //
                    generator.Emit(OpCodes.Ldtoken, fieldDefinition.FieldType);
                    generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);

                    generator.Emit(OpCodes.Ldloc, locIdx); //load value
                    generator.Emit(OpCodes.Box, fieldDefinition.FieldType);

                    generator.Emit(OpCodes.Ldarg, 3); //load alwaysWrite bool

                    generator.Emit(OpCodes.Ldarg, 2); //load context

                    var writeDataFieldMethod = typeof(ISerializationManager).GetMethods().First(m =>
                        m.Name == nameof(ISerializationManager.WriteValue) && m.GetParameters().Length == 4);
                    generator.Emit(OpCodes.Callvirt, writeDataFieldMethod); //get new node
                    break;
            }

            var addMethod = typeof(MappingDataNode).GetMethods().First(m =>
                m.Name == nameof(MappingDataNode.AddNode) &&
                m.GetParameters().First().ParameterType == typeof(string));
            generator.Emit(OpCodes.Callvirt, addMethod); //add it

            generator.MarkLabel(endLabel);
        }

        public static void EmitCopy(this RobustILGenerator generator, int fromArg, AbstractFieldInfo fromField, int toArg,
            AbstractFieldInfo toField, int mgrArg, int ctxArg, bool assumeValueNotNull = false)
        {
            //todo paul breaks if fromfield is nullable & tofield is not

            if (assumeValueNotNull)
            {
                var type = fromField.FieldType.EnsureNotNullableType();
                if (!toField.FieldType.IsAssignableFrom(type))
                    throw new InvalidOperationException("Mismatch of types in EmitCopy call");
            }
            else
            {
                if (!toField.FieldType.IsAssignableFrom(fromField.FieldType))
                    throw new InvalidOperationException("Mismatch of types in EmitCopy call");
            }

            generator.Emit(OpCodes.Ldarg, toArg);

            generator.Emit(OpCodes.Ldarg, mgrArg);

            generator.Emit(OpCodes.Ldarg, fromArg);
            generator.EmitLdfld(fromField);
            //if(!fromField.FieldType.IsPrimitive && fromField.FieldType.IsValueType)
            generator.Emit(OpCodes.Box, fromField.FieldType);
            generator.Emit(OpCodes.Ldarg, toArg);
            generator.EmitLdfld(toField);
            //if(!toField.FieldType.IsPrimitive && toField.FieldType.IsValueType)
            generator.Emit(OpCodes.Box, toField.FieldType);

            //todo paul use context
            generator.Emit(OpCodes.Ldarg, ctxArg);

            var copyMethod = typeof(ISerializationManager).GetMethod(nameof(ISerializationManager.Copy), new Type[] {typeof(object), typeof(object), typeof(ISerializationContext)});
            Debug.Assert(copyMethod != null, nameof(copyMethod) + " != null");
            generator.Emit(OpCodes.Callvirt, copyMethod);
            generator.Emit(OpCodes.Unbox_Any, toField.FieldType);

            generator.EmitStfld(toField);
        }

        public static void EmitEquals(this RobustILGenerator generator, Type type, Label label)
        {
            if (type.IsPrimitive)
            {
                generator.Emit(OpCodes.Beq_S, label);
            }
            else
            {
                var equalsmethod = typeof(Object).GetMethods()
                    .First(m => m.Name == nameof(Equals) && m.GetParameters().Length == 2);
                generator.Emit(OpCodes.Call, equalsmethod);
                generator.Emit(OpCodes.Brtrue_S, label);
            }
        }

        public static void EmitStfld(this RobustILGenerator generator,
            AbstractFieldInfo fieldDefinition)
        {
            switch (fieldDefinition)
            {
                case SpecificFieldInfo field:
                    generator.Emit(OpCodes.Stfld, field.FieldInfo);
                    break;
                case SpecificPropertyInfo property:
                    generator.Emit(OpCodes.Call, property.PropertyInfo.SetMethod!);
                    break;
            }
        }

        public static void EmitLdfld(this RobustILGenerator generator,
            AbstractFieldInfo fieldDefinition)
        {
            switch (fieldDefinition)
            {
                case SpecificFieldInfo field:
                    generator.Emit(OpCodes.Ldfld, field.FieldInfo);
                    break;
                case SpecificPropertyInfo property:
                    generator.Emit(OpCodes.Call, property.PropertyInfo.GetMethod!);
                    break;
            }
        }
    }
}
