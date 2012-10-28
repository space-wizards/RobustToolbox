using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;

namespace SFML.Graphics.Design
{
    /// <summary>Provides a unified way of converting Vector2 values to other types, as well as for accessing standard values and subproperties.</summary>
    public class Vector2Converter : MathTypeConverter
    {
        /// <summary>Initializes a new instance of the Vector2Converter class.</summary>
        public Vector2Converter()
        {
            var type = typeof(Vector2);
            propertyDescriptions =
                new PropertyDescriptorCollection(new PropertyDescriptor[]
                { new FieldPropertyDescriptor(type.GetField("X")), new FieldPropertyDescriptor(type.GetField("Y")) }).Sort(
                    new string[] { "X", "Y" });
        }

        /// <summary>Converts the given object to the type of this converter, using the specified context and culture information.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="culture">The current culture.</param>
        /// <param name="value">The object to convert.</param>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var numArray = ConvertToValues<float>(context, culture, value, 2, new string[] { "X", "Y" });
            if (numArray != null)
                return new Vector2(numArray[0], numArray[1]);
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
            if ((destinationType == typeof(string)) && (value is Vector2))
            {
                var vector2 = (Vector2)value;
                return ConvertFromValues(context, culture, new float[] { vector2.X, vector2.Y });
            }
            if ((destinationType == typeof(InstanceDescriptor)) && (value is Vector2))
            {
                var vector = (Vector2)value;
                var constructor = typeof(Vector2).GetConstructor(new Type[] { typeof(float), typeof(float) });
                if (constructor != null)
                    return new InstanceDescriptor(constructor, new object[] { vector.X, vector.Y });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>Creates an instance of the type that this Vector2Converter is associated with, using the specified context, given a set of property values for the object.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="propertyValues">The new property values.</param>
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null)
                throw new ArgumentNullException("propertyValues", FrameworkMessages.NullNotAllowed);
            return new Vector2((float)propertyValues["X"], (float)propertyValues["Y"]);
        }
    }
}