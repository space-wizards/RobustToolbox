using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;

namespace SFML.Graphics.Design
{
    /// <summary>Provides a unified way of converting Color values to other types, as well as for accessing standard values and subproperties.</summary>
    public class ColorConverter : MathTypeConverter
    {
        /// <summary>Initializes a new instance of the ColorConverter class.</summary>
        public ColorConverter()
        {
            var type = typeof(Color);
            propertyDescriptions =
                new PropertyDescriptorCollection(new PropertyDescriptor[]
                {
                    new PropertyPropertyDescriptor(type.GetProperty("R")), new PropertyPropertyDescriptor(type.GetProperty("G")),
                    new PropertyPropertyDescriptor(type.GetProperty("B")), new PropertyPropertyDescriptor(type.GetProperty("A"))
                })
                    .Sort(new string[] { "R", "G", "B", "A" });
        }

        /// <summary>Converts the given value to the type of this converter.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="culture">The current culture.</param>
        /// <param name="value">The object to convert.</param>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var buffer = ConvertToValues<byte>(context, culture, value, 4, new string[] { "R", "G", "B", "A" });
            if (buffer != null)
                return new Color(buffer[0], buffer[1], buffer[2], buffer[3]);
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
            if ((destinationType == typeof(string)) && (value is Color))
            {
                var color2 = (Color)value;
                return ConvertFromValues(context, culture, new byte[] { color2.R, color2.G, color2.B, color2.A });
            }
            if ((destinationType == typeof(InstanceDescriptor)) && (value is Color))
            {
                var color = (Color)value;
                var constructor =
                    typeof(Color).GetConstructor(new Type[] { typeof(byte), typeof(byte), typeof(byte), typeof(byte) });
                if (constructor != null)
                    return new InstanceDescriptor(constructor, new object[] { color.R, color.G, color.B, color.A });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>Re-creates an object given a set of property values for the object.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="propertyValues">The new property values.</param>
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null)
                throw new ArgumentNullException("propertyValues");
            return new Color((byte)propertyValues["R"], (byte)propertyValues["G"], (byte)propertyValues["B"],
                (byte)propertyValues["A"]);
        }
    }
}