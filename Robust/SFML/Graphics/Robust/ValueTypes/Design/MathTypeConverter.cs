using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;

namespace SFML.Graphics.Design
{
    /// <summary>Provides a unified way of converting math type values to other types, as well as for accessing standard values and subproperties.</summary>
    public class MathTypeConverter : ExpandableObjectConverter
    {
        /// <summary>Represents a collection of PropertyDescriptor objects.</summary>
        protected PropertyDescriptorCollection propertyDescriptions;

        /// <summary>Returns whether string conversion is supported.</summary>
        protected bool supportStringConvert = true;

        /// <summary>Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="sourceType">The type you want to convert from.</param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return ((supportStringConvert && (sourceType == typeof(string))) || base.CanConvertFrom(context, sourceType));
        }

        /// <summary>Returns whether this converter can convert an object of one type to the type of this converter.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="destinationType">The destination type.</param>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return ((destinationType == typeof(InstanceDescriptor)) || base.CanConvertTo(context, destinationType));
        }

        internal static string ConvertFromValues<T>(ITypeDescriptorContext context, CultureInfo culture, T[] values)
        {
            if (culture == null)
                culture = CultureInfo.CurrentCulture;
            var separator = culture.TextInfo.ListSeparator + " ";
            var converter = TypeDescriptor.GetConverter(typeof(T));
            var strArray = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                strArray[i] = converter.ConvertToString(context, culture, values[i]);
            }
            return string.Join(separator, strArray);
        }

        internal static T[] ConvertToValues<T>(ITypeDescriptorContext context, CultureInfo culture, object value, int arrayCount,
                                               params string[] expectedParams)
        {
            var str = value as string;
            if (str == null)
                return null;

            str = str.Trim();
            if (str.StartsWith("{"))
                str = str.Substring(1);
            if (str.EndsWith("}"))
                str = str.Substring(0, str.Length - 1);

            if (culture == null)
                culture = CultureInfo.CurrentCulture;

            var strArray = str.Split(new string[] { culture.TextInfo.ListSeparator }, StringSplitOptions.None);
            var localArray = new T[strArray.Length];
            var converter = TypeDescriptor.GetConverter(typeof(T));
            for (var i = 0; i < localArray.Length; i++)
            {
                try
                {
                    localArray[i] = (T)converter.ConvertFromString(context, culture, strArray[i]);
                }
                catch (Exception exception)
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture, FrameworkMessages.InvalidStringFormat,
                            new object[] { string.Join(culture.TextInfo.ListSeparator, expectedParams) }), exception);
                }
            }
            if (localArray.Length != arrayCount)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FrameworkMessages.InvalidStringFormat,
                    new object[] { string.Join(culture.TextInfo.ListSeparator, expectedParams) }));
            }
            return localArray;
        }

        /// <summary>Returns whether changing a value on this object requires a call to CreateInstance to create a new value, using the specified context.</summary>
        /// <param name="context">The format context.</param>
        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>Returns a collection of properties for the type of array specified by the value parameter.</summary>
        /// <param name="context">The format context.</param>
        /// <param name="value">The type of array for which to get properties.</param>
        /// <param name="attributes">An array to use as a filter.</param>
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value,
                                                                   Attribute[] attributes)
        {
            return propertyDescriptions;
        }

        /// <summary>Returns whether this object supports properties, using the specified context.</summary>
        /// <param name="context">The format context.</param>
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}