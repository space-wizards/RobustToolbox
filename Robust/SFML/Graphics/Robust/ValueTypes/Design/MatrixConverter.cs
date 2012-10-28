using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;

namespace SFML.Graphics.Design
{
    /// <summary>Provides a unified way of converting Matrix values to other types, as well as for accessing standard values and subproperties.</summary>
    public class MatrixConverter : MathTypeConverter
    {
        /// <summary>Initializes a new instance of the MatrixConverter class.</summary>
        public MatrixConverter()
        {
            var componentType = typeof(Matrix);
            var properties = TypeDescriptor.GetProperties(componentType);
            var descriptors =
                new PropertyDescriptorCollection(new PropertyDescriptor[]
                {
                    properties.Find("Translation", true), new FieldPropertyDescriptor(componentType.GetField("M11")),
                    new FieldPropertyDescriptor(componentType.GetField("M12")),
                    new FieldPropertyDescriptor(componentType.GetField("M13")),
                    new FieldPropertyDescriptor(componentType.GetField("M14")),
                    new FieldPropertyDescriptor(componentType.GetField("M21")),
                    new FieldPropertyDescriptor(componentType.GetField("M22")),
                    new FieldPropertyDescriptor(componentType.GetField("M23")),
                    new FieldPropertyDescriptor(componentType.GetField("M24")),
                    new FieldPropertyDescriptor(componentType.GetField("M31")),
                    new FieldPropertyDescriptor(componentType.GetField("M32")),
                    new FieldPropertyDescriptor(componentType.GetField("M33")),
                    new FieldPropertyDescriptor(componentType.GetField("M34")),
                    new FieldPropertyDescriptor(componentType.GetField("M41")),
                    new FieldPropertyDescriptor(componentType.GetField("M42")),
                    new FieldPropertyDescriptor(componentType.GetField("M43")),
                    new FieldPropertyDescriptor(componentType.GetField("M44"))
                });
            propertyDescriptions = descriptors;
            supportStringConvert = false;
        }

        /// <summary>Converts the given value object to the specified type, using the specified context and culture information.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <param name="value">The object to convert.</param>
        /// <param name="destinationType">The destination type.</param>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException("destinationType");
            if ((destinationType == typeof(InstanceDescriptor)) && (value is Matrix))
            {
                var matrix = (Matrix)value;
                var constructor =
                    typeof(Matrix).GetConstructor(new Type[]
                    {
                        typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float),
                        typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float), typeof(float),
                        typeof(float), typeof(float)
                    });
                if (constructor != null)
                {
                    return new InstanceDescriptor(constructor,
                        new object[]
                        {
                            matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                            matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44
                        });
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>Creates an instance of the type that this MatrixConverter is associated with, using the specified context, given a set of property values for the object.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="propertyValues">The new property values.</param>
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null)
                throw new ArgumentNullException("propertyValues", FrameworkMessages.NullNotAllowed);
            return new Matrix((float)propertyValues["M11"], (float)propertyValues["M12"], (float)propertyValues["M13"],
                (float)propertyValues["M14"], (float)propertyValues["M21"], (float)propertyValues["M22"],
                (float)propertyValues["M23"], (float)propertyValues["M24"], (float)propertyValues["M31"],
                (float)propertyValues["M32"], (float)propertyValues["M33"], (float)propertyValues["M34"],
                (float)propertyValues["M41"], (float)propertyValues["M42"], (float)propertyValues["M43"],
                (float)propertyValues["M44"]);
        }
    }
}