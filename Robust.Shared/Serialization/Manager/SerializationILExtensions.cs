using System;
using System.Diagnostics;
using System.Reflection.Emit;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public static class SerializationILExtensions
    {
        /// <summary>
        /// WARNING: This method assumes the object is at position 0 & the yamlobjectserializer is at position 1
        /// </summary>
        public static void EmitPopulateField(this ILGenerator generator,
            SerializationDataDefinition.BaseFieldDefinition fieldDefinition)
        {
            generator.Emit(OpCodes.Ldarg_0); //load object - needed for the stfld call later

            generator.Emit(OpCodes.Ldarg_1); //load serializer
            generator.Emit(OpCodes.Ldarg, fieldDefinition.Attribute.Tag); // load the yaml tag
            generator.Emit(OpCodes.Ldnull); //load default value (null)

            //making sure we have a nullable type
            var type = fieldDefinition.FieldType.EnsureNullableType();

            var readDataFieldMethod = typeof(YamlObjectSerializer).GetMethod(nameof(YamlObjectSerializer.ReadDataField))!.MakeGenericMethod(type);

            generator.Emit(OpCodes.Call, readDataFieldMethod); //reading datafield using the yamlobjectserializer

            generator.Emit(OpCodes.Stloc_0); //storing the return value so we can populate it into our field later
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldnull);

            var isNullLabel = new Label();
            generator.Emit(OpCodes.Beq_S, isNullLabel); //checking if the value is null. if so, we skip setting the field

            //setting the field
            generator.Emit(OpCodes.Ldloc_0);
            switch (fieldDefinition)
            {
                case SerializationDataDefinition.FieldDefinition field:
                    generator.Emit(OpCodes.Stfld, field.FieldInfo);
                    break;
                case SerializationDataDefinition.PropertyDefinition property:
                    generator.Emit(OpCodes.Call, property.PropertyInfo.SetMethod!); //todo paul enforce setter!!!
                    break;
            }

            generator.MarkLabel(isNullLabel);
        }

        public static void EmitSerializeField(this ILGenerator generator,
            SerializationDataDefinition.BaseFieldDefinition fieldDefinition)
        {
            if(fieldDefinition.Attribute.ReadOnly) return; //hehe ez pz

            var isNullLabel = new Label();

            //skip all of this if the value is null
            generator.Emit(OpCodes.Ldarg_0);
            switch (fieldDefinition)
            {
                case SerializationDataDefinition.FieldDefinition field:
                    generator.Emit(OpCodes.Ldfld, field.FieldInfo);
                    break;
                case SerializationDataDefinition.PropertyDefinition property:
                    generator.Emit(OpCodes.Call, property.PropertyInfo.GetMethod!); //todo paul enforce getter!!!
                    break;
            }
            generator.Emit(OpCodes.Stloc_0); //also storing the value for later so we dont have to do ldfld again
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Beq_S, isNullLabel);

            var type = fieldDefinition.FieldType.EnsureNullableType();

            var writedatafieldmethod = typeof(YamlObjectSerializer)
                .GetMethod(nameof(YamlObjectSerializer.WriteDataField))!.MakeGenericMethod(type);

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg, fieldDefinition.Attribute.Tag);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Call, writedatafieldmethod);

            generator.MarkLabel(isNullLabel);
        }

        public static void EmitExposeDataCall(this ILGenerator generator)
        {
            var exposeDataMethod = typeof(IExposeData).GetMethod(nameof(IExposeData.ExposeData));
            Debug.Assert(exposeDataMethod != null, nameof(exposeDataMethod) + " != null");
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, exposeDataMethod);
        }
    }
}
