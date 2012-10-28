using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;

namespace SFML.Graphics.Design
{
    /// <summary>Provides a unified way of converting Ray values to other types, as well as for accessing standard values and subproperties.</summary>
    public class RayConverter : MathTypeConverter
    {
        /// <summary>Initializes a new instance of the RayConverter class.</summary>
        public RayConverter()
        {
            var type = typeof(Ray);
            propertyDescriptions =
                new PropertyDescriptorCollection(new PropertyDescriptor[]
                {
                    new FieldPropertyDescriptor(type.GetField("Position")), new FieldPropertyDescriptor(type.GetField("Direction"))
                }).Sort(new string[] { "Position", "Direction" });

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
            if ((destinationType == typeof(InstanceDescriptor)) && (value is Ray))
            {
                var ray = (Ray)value;
                var constructor = typeof(Ray).GetConstructor(new Type[] { typeof(Vector3), typeof(Vector3) });
                if (constructor != null)
                    return new InstanceDescriptor(constructor, new object[] { ray.Position, ray.Direction });
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>Creates an instance of the type that this RayConverter is associated with, using the specified context, given a set of property values for the object.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="propertyValues">The new property values.</param>
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null)
                throw new ArgumentNullException("propertyValues", FrameworkMessages.NullNotAllowed);
            return new Ray((Vector3)propertyValues["Position"], (Vector3)propertyValues["Direction"]);
        }
    }
}