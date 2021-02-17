using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public static class SerializationILExtensions
    {
        // object target, IMappingDataNode mappingDataNode, IServ3Manager serv3Manager, ISerializationContext? context, object?[] defaultValues
        public static void EmitPopulateField(this ILGenerator generator,
            SerializationDataDefinition.FieldDefinition fieldDefinition, int localIdx, int defaultValueIdx)
        {
            /* todo
        public readonly Type? FlagType;
        public readonly Type? ConstantType;
        public readonly bool ServerOnly;
             */

            var endLabel = generator.DefineLabel();

            generator.Emit(OpCodes.Ldarg_1); //load mappingnode
            generator.Emit(OpCodes.Ldstr, fieldDefinition.Attribute.Tag); //load tag
            var hasNodeMethod = typeof(IMappingDataNode).GetMethods().First(m =>
                m.Name == nameof(IMappingDataNode.HasNode) &&
                m.GetParameters().First().ParameterType == typeof(string));
            generator.Emit(OpCodes.Callvirt, hasNodeMethod); //checking if our node is mapped
            var notMappedLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Brfalse, notMappedLabel);

            generator.Emit(OpCodes.Ldarg_2); //load serv3mgr

            // getting the type //
            generator.Emit(OpCodes.Ldtoken, fieldDefinition.FieldType);
            generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);

            // getting the node //
            generator.Emit(OpCodes.Ldarg_1); //load mappingnode
            generator.Emit(OpCodes.Ldstr, fieldDefinition.Attribute.Tag); //loading the tag
            var getNodeMethod = typeof(IMappingDataNode).GetMethods().First(m =>
                m.Name == nameof(IMappingDataNode.GetNode) &&
                m.GetParameters().First().ParameterType == typeof(string));
            generator.Emit(OpCodes.Callvirt, getNodeMethod); //getting the node

            generator.Emit(OpCodes.Ldarg_3); //loading context

            var readValueMethod = typeof(IServ3Manager).GetMethods().First(m =>
                m.Name == nameof(IServ3Manager.ReadValue) && m.GetParameters().Length == 3);
            generator.Emit(OpCodes.Callvirt, readValueMethod); //reads node into our desired value

            //storing the return value so we can populate it into our field later
            generator.Emit(OpCodes.Stloc, localIdx);

            //loading back our value
            generator.Emit(OpCodes.Ldloc, localIdx);

            //loading default value
            generator.Emit(OpCodes.Ldarg, 4);
            generator.Emit(OpCodes.Ldc_I4, defaultValueIdx);
            generator.Emit(OpCodes.Ldelem, fieldDefinition.FieldType);

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

        // object obj, IServ3Manager serv3Manager, IDataNodeFactory nodeFactory, ISerializationContext? context, bool alwaysWrite, object?[] defaultValues
        public static void EmitSerializeField(this ILGenerator generator,
            SerializationDataDefinition.FieldDefinition fieldDefinition, int defaultValueIdx)
        {
            if(fieldDefinition.Attribute.ReadOnly) return; //hehe ez pz

            /*
public readonly Type? FlagType;
public readonly Type? ConstantType;
public readonly bool ServerOnly;
 */

            var endLabel = generator.DefineLabel();

            // loading value into loc0
            generator.Emit(OpCodes.Ldarg_0);
            generator.EmitLdfld(fieldDefinition.FieldInfo);
            generator.Emit(OpCodes.Stloc_0);

            //only do defaultcheck if we aren't required
            if (!fieldDefinition.Attribute.Required)
            {
                var skipDefaultCheckLabel = generator.DefineLabel();

                generator.Emit(OpCodes.Ldarg, 4); //load alwaysWrite bool
                generator.Emit(OpCodes.Brtrue_S, skipDefaultCheckLabel); //skip defaultcheck if alwayswrite is true

                //skip all of this if the value is default
                generator.Emit(OpCodes.Ldloc_0); //load val
                //load default value
                generator.Emit(OpCodes.Ldarg, 5);
                generator.Emit(OpCodes.Ldc_I4, defaultValueIdx);
                generator.Emit(OpCodes.Ldelem, fieldDefinition.FieldType);
                //check if value is default
                generator.EmitEquals(fieldDefinition.FieldType, endLabel);

                generator.MarkLabel(skipDefaultCheckLabel);
            }

            generator.Emit(OpCodes.Ldloc_0); //loading mappingnode for calling merge


            generator.Emit(OpCodes.Ldarg_1); //load serv3mgr

            generator.Emit(OpCodes.Ldtoken, fieldDefinition.FieldType);
            generator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);

            generator.Emit(OpCodes.Ldloc_0); //load value

            generator.Emit(OpCodes.Ldarg_2); //load nodeFactory

            generator.Emit(OpCodes.Ldarg, 4); //load alwaysWrite bool

            generator.Emit(OpCodes.Ldarg_3); //load context

            var writeDataFieldMethod = typeof(IServ3Manager).GetMethods().First(m =>
                m.Name == nameof(IServ3Manager.WriteValue) && m.GetParameters().Length == 5);
            generator.Emit(OpCodes.Callvirt, writeDataFieldMethod); //get new node

            generator.Emit(OpCodes.Castclass, typeof(IMappingDataNode)); //cast our idatanode to a mappingnode

            var mergeMethod = typeof(IMappingDataNode).GetMethod(nameof(IMappingDataNode.Merge));
            Debug.Assert(mergeMethod != null, nameof(mergeMethod) + " != null");
            generator.Emit(OpCodes.Callvirt, mergeMethod); //merge em

            generator.Emit(OpCodes.Stloc_0); //store updated node in loc0

            generator.MarkLabel(endLabel);
        }

        public static void EmitPushInheritanceField(this ILGenerator generator,
            SerializationDataDefinition.FieldDefinition fieldDefinition, int defaultValueIdx)
        {
            var isDefaultValue = generator.DefineLabel();

            //load sourcevalue
            generator.Emit(OpCodes.Ldarg_0);
            generator.EmitLdfld(fieldDefinition.FieldInfo);

            //load defaultValue
            generator.Emit(OpCodes.Ldarg_3);
            generator.Emit(OpCodes.Ldc_I4, defaultValueIdx);
            generator.Emit(OpCodes.Ldelem, fieldDefinition.FieldType);
            generator.EmitEquals(fieldDefinition.FieldType, isDefaultValue);

            //copy if not default
            generator.EmitCopy(0, fieldDefinition.FieldInfo, 1, fieldDefinition.FieldInfo, 2);

            generator.MarkLabel(isDefaultValue);
        }

        public static void EmitCopy(this ILGenerator generator, int fromArg, AbstractFieldInfo fromField, int toArg,
            AbstractFieldInfo toField, int mgrArg, bool assumeValueNotNull = false)
        {
            if (assumeValueNotNull)
            {
                var type = fromField.FieldType;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    type = type.GenericTypeArguments.First();
                }
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
            generator.Emit(OpCodes.Ldarg, toArg);
            generator.EmitLdfld(toField);

            var copyMethod = typeof(IServ3Manager).GetMethod(nameof(IServ3Manager.Copy));
            Debug.Assert(copyMethod != null, nameof(copyMethod) + " != null");
            generator.Emit(OpCodes.Callvirt, copyMethod);

            generator.EmitStfld(toField);
        }

        public static void EmitEquals(this ILGenerator generator, Type type, Label label)
        {
            if (type.IsPrimitive || type == typeof(string))
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

        public static void EmitStfld(this ILGenerator generator,
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

        public static void EmitLdfld(this ILGenerator generator,
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
