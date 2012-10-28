using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;

namespace SFML.Graphics.Design
{
    /// <summary>Provides a unified way of converting Quaternion values to other types, as well as for accessing standard values and subproperties.</summary>
    public class QuaternionConverter : MathTypeConverter
    {
        /// <summary>Initializes a new instance of the QuaternionConverter class.</summary>
        public QuaternionConverter()
        {
            var type = typeof(Quaternion);
            propertyDescriptions =
                new PropertyDescriptorCollection(new PropertyDescriptor[]
                {
                    new FieldPropertyDescriptor(type.GetField("X")), new FieldPropertyDescriptor(type.GetField("Y")),
                    new FieldPropertyDescriptor(type.GetField("Z")), new FieldPropertyDescriptor(type.GetField("W"))
                }).Sort(
                    new string[] { "X", "Y", "Z", "W" });
        }

        /// <summary>Converts the given object to the type of this converter, using the specified context and culture information.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="culture">The current culture.</param>
        /// <param name="value">The object to convert.</param>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var numArray = ConvertToValues<float>(context, culture, value, 4, new string[] { "X", "Y", "Z", "W" });
            if (numArray != null)
                return new Quaternion(numArray[0], numArray[1], numArray[2], numArray[3]);
            return base.ConvertFrom(context, culture, value);
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
            if ((destinationType == typeof(string)) && (value is Quaternion))
            {
                var quaternion2 = (Quaternion)value;
                return ConvertFromValues(context, culture,
                    new float[] { quaternion2.X, quaternion2.Y, quaternion2.Z, quaternion2.W });
            }
            if ((destinationType == typeof(InstanceDescriptor)) && (value is Quaternion))
            {
                var quaternion = (Quaternion)value;
                var constructor =
                    typeof(Quaternion).GetConstructor(new Type[] { typeof(float), typeof(float), typeof(float), typeof(float) });
                if (constructor != null)
                    return new InstanceDescriptor(constructor,
                        new object[] { quaternion.X, quaternion.Y, quaternion.Z, quaternion.W });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>Creates an instance of the type that this QuaternionConverter is associated with, using the specified context, given a set of property values for the object.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="propertyValues">The new property values.</param>
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null)
                throw new ArgumentNullException("propertyValues", FrameworkMessages.NullNotAllowed);
            return new Quaternion((float)propertyValues["X"], (float)propertyValues["Y"], (float)propertyValues["Z"],
                (float)propertyValues["W"]);
        }
    }
}