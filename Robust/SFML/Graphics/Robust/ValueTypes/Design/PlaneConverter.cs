using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;

namespace SFML.Graphics.Design
{
    /// <summary>Provides a unified way of converting Plane values to other types, as well as for accessing standard values and subproperties.</summary>
    public class PlaneConverter : MathTypeConverter
    {
        /// <summary>Initializes a new instance of the PlaneConverter class.</summary>
        public PlaneConverter()
        {
            var type = typeof(Plane);
            propertyDescriptions =
                new PropertyDescriptorCollection(new PropertyDescriptor[]
                { new FieldPropertyDescriptor(type.GetField("Normal")), new FieldPropertyDescriptor(type.GetField("D")) }).Sort(
                    new string[] { "Normal", "D" });
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
            if ((destinationType == typeof(InstanceDescriptor)) && (value is Plane))
            {
                var plane = (Plane)value;
                var constructor = typeof(Plane).GetConstructor(new Type[] { typeof(Vector3), typeof(float) });
                if (constructor != null)
                    return new InstanceDescriptor(constructor, new object[] { plane.Normal, plane.D });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>Creates an instance of the type that this PlaneConverter is associated with, using the specified context, given a set of property values for the object.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="propertyValues">The new property values.</param>
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null)
                throw new ArgumentNullException("propertyValues", FrameworkMessages.NullNotAllowed);
            return new Plane((Vector3)propertyValues["Normal"], (float)propertyValues["D"]);
        }
    }
}