using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Robust.Shared.Interfaces.Serialization;

namespace Robust.Shared.Serialization.Manager
{
    //TODO PAUL FUCK I FORGOT NESTING
    public static class SerializationILExtensions
    {
        /// <summary>
        /// WARNING: This method assumes the object is at position 0 & the yamlobjectserializer is at position 1
        /// </summary>
        public static void EmitPopulateField(this ILGenerator generator,
            SerializationDataDefinition.BaseFieldDefinition fieldDefinition)
        {
            generator.Emit(OpCodes.Ldarg_0); //load object - needed for the stfld call later

            if (fieldDefinition.FieldType.IsPrimitive)
            {
                generator.Emit(OpCodes.Ldarg_1); //load serializer
                generator.Emit(OpCodes.Ldstr, fieldDefinition.Attribute.Tag); // load the yaml tag
                generator.Emit_LdInst(fieldDefinition.DefaultValue, true); //load default value

                var readDataFieldMethod =
                    typeof(YamlObjectSerializer).GetMethod(nameof(YamlObjectSerializer.ReadDataField))?
                        .MakeGenericMethod(fieldDefinition.FieldType);
                Debug.Assert(readDataFieldMethod != null, nameof(readDataFieldMethod) + " != null");
                generator.Emit(OpCodes.Call, readDataFieldMethod); //reading datafield using the yamlobjectserializer
            }
            else
            {
                generator.Emit(OpCodes.Ldarg_2); //load serv3manager
                generator.Emit(OpCodes.Ldobj, fieldDefinition.FieldType); //load type
                generator.Emit(OpCodes.Ldarg_1); //load serializer

                var populateMethod = typeof(SerializationManager).GetMethod(nameof(SerializationManager.Populate));
                Debug.Assert(populateMethod != null, nameof(populateMethod) + " != null");
                generator.Emit(OpCodes.Call, populateMethod); //call populate(type, serializer)
            }

            generator.Emit(OpCodes.Stloc_0); //storing the return value so we can populate it into our field later
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit_LdInst(fieldDefinition.DefaultValue, true); //load default value

            var isDefaultLabel = new Label();
            generator.EmitEquals(fieldDefinition.FieldType, isDefaultLabel); //checking if the value is default. if so, we skip setting the field

            //setting the field
            generator.Emit(OpCodes.Ldloc_0);
            generator.EmitStdlf(fieldDefinition);

            generator.MarkLabel(isDefaultLabel);
        }

        public static void EmitPushInheritanceField(this ILGenerator generator,
            SerializationDataDefinition.BaseFieldDefinition fieldDefinition)
        {
            var isDefaultValue = new Label();

            generator.Emit(OpCodes.Ldarg_0);
            generator.EmitLdfld(fieldDefinition);
            generator.Emit_LdInst(fieldDefinition.DefaultValue, true);
            generator.EmitEquals(fieldDefinition.FieldType, isDefaultValue);

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_0);
            generator.EmitLdfld(fieldDefinition);
            generator.EmitStdlf(fieldDefinition);

            generator.MarkLabel(isDefaultValue);
        }

        public static void EmitEquals(this ILGenerator generator, Type type, Label label)
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

        public static void EmitStdlf(this ILGenerator generator,
            SerializationDataDefinition.BaseFieldDefinition fieldDefinition)
        {
            switch (fieldDefinition)
            {
                case SerializationDataDefinition.FieldDefinition field:
                    generator.Emit(OpCodes.Stfld, field.FieldInfo);
                    break;
                case SerializationDataDefinition.PropertyDefinition property:
                    generator.Emit(OpCodes.Call, property.PropertyInfo.SetMethod!); //todo paul enforce setter!!!
                    break;
            }
        }

        public static void EmitLdfld(this ILGenerator generator,
            SerializationDataDefinition.BaseFieldDefinition fieldDefinition)
        {
            switch (fieldDefinition)
            {
                case SerializationDataDefinition.FieldDefinition field:
                    generator.Emit(OpCodes.Ldfld, field.FieldInfo);
                    break;
                case SerializationDataDefinition.PropertyDefinition property:
                    generator.Emit(OpCodes.Call, property.PropertyInfo.GetMethod!); //todo paul enforce getter!!!
                    break;
            }
        }

        public static void EmitSerializeField(this ILGenerator generator,
            SerializationDataDefinition.BaseFieldDefinition fieldDefinition)
        {
            if(fieldDefinition.Attribute.ReadOnly) return; //hehe ez pz

            var isDefaultLabel = new Label();
            var alwaysWriteLabel = new Label();

            generator.Emit(OpCodes.Ldarg_3); //load alwaysWrite bool
            generator.Emit(OpCodes.Brtrue_S, alwaysWriteLabel); //skip defaultcheck if alwayswrite is true

            //skip all of this if the value is default
            generator.Emit(OpCodes.Ldarg_0);
            generator.EmitLdfld(fieldDefinition);
            generator.Emit(OpCodes.Stloc_0); //also storing the value for later so we dont have to do ldfld again
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit_LdInst(fieldDefinition.DefaultValue);
            generator.EmitEquals(fieldDefinition.FieldType, isDefaultLabel);

            generator.MarkLabel(alwaysWriteLabel);

            if (fieldDefinition.FieldType.IsPrimitive)
            {
                generator.Emit(OpCodes.Ldarg_1); // loading serializer
                generator.Emit(OpCodes.Ldstr, fieldDefinition.Attribute.Tag); //loading tag
                generator.Emit(OpCodes.Ldloc_0); //loading value
                generator.Emit_LdInst(fieldDefinition.DefaultValue, true); //loading default value (altough this kinda doesn't matter)

                var writedatafieldmethod = typeof(YamlObjectSerializer)
                    .GetMethod(nameof(YamlObjectSerializer.WriteDataField))?.MakeGenericMethod(fieldDefinition.FieldType);
                Debug.Assert(writedatafieldmethod != null, nameof(writedatafieldmethod) + " != null");
                generator.Emit(OpCodes.Call, writedatafieldmethod); //calling writedatafield
            }
            else
            {
                generator.Emit(OpCodes.Ldarg_2); //load serv3mgr
                generator.Emit(OpCodes.Ldobj, fieldDefinition.FieldType); //loading type
                generator.Emit(OpCodes.Ldloc_0); //loading object
                generator.Emit(OpCodes.Ldarg_1); //loading serializer
                generator.Emit(OpCodes.Ldarg_3); //loading alwaysWrite flag

                var serializeMethod = typeof(SerializationManager).GetMethod(nameof(SerializationManager.Serialize));
                Debug.Assert(serializeMethod != null, nameof(serializeMethod) + " != null");
                generator.Emit(OpCodes.Call, serializeMethod);
            }

            generator.MarkLabel(isDefaultLabel);
        }

        public static void EmitExposeDataCall(this ILGenerator generator)
        {
            var exposeDataMethod = typeof(IExposeData).GetMethod(nameof(IExposeData.ExposeData));
            Debug.Assert(exposeDataMethod != null, nameof(exposeDataMethod) + " != null");
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, exposeDataMethod);
        }

        /// <summary>
        /// Burn an reference to the specified runtime object instance into the DynamicMethod
        /// taken from https://stackoverflow.com/questions/4989681/place-an-object-on-top-of-stack-in-ilgenerator
        /// </summary>
        public static void Emit_LdInst<T>(this ILGenerator il, T inst, bool free = false)
            where T : class
        {
            var gch = GCHandle.Alloc(inst);

            var ptr = GCHandle.ToIntPtr(gch);

            if (IntPtr.Size == 4)
                il.Emit(OpCodes.Ldc_I4, ptr.ToInt32());
            else
                il.Emit(OpCodes.Ldc_I8, ptr.ToInt64());

            il.Emit(OpCodes.Ldobj, typeof(T));

            /// Do this only if you can otherwise ensure that 'inst' outlives the DynamicMethod
            if(free)
                gch.Free();
        }
    }
}
