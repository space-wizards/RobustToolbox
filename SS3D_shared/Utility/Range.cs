using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SS13_Shared.Utility
{
    /// <summary>
    /// Value type to indicate a range of values.
    /// </summary>
    /// <typeparam name="T">Type of data for the range.</typeparam>
    [TypeConverter(typeof(RangeTypeConverter))]
    public class Range<T>
    {
        /// <summary>
        /// Starting point in the range.
        /// </summary>
        public T Start { get; set; }

        /// <summary>
        /// Ending point in the range.
        /// </summary>
        public T End { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Range&lt;T&gt;"/> struct.
        /// </summary>
        /// <param name="start">The starting value.</param>
        /// <param name="end">The ending value.</param>
        public Range(T start, T end)
        {
            Start = start;
            End = end;
        }
    }

    
    public class RangeTypeConverter : ExpandableObjectConverter
    {
        private Type _genericInstanceType;
        private Type _innerType;
        private TypeConverter _innerTypeConverter;
        public RangeTypeConverter(Type type)
        {
            if (type.IsGenericType
    && type.GetGenericTypeDefinition() == typeof(Range<>)
    && type.GetGenericArguments().Length == 1)
            {
                _genericInstanceType = type;
                _innerType = type.GetGenericArguments()[0];
                _innerTypeConverter = TypeDescriptor.GetConverter(_innerType);
            }
            else
            {
                throw new ArgumentException("Incompatible type", "type");
            }
        }

        // only support To and From strings
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }
        
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                var strval = (string) value;
                var strings = strval.Replace(" ", "").Split('-');
                dynamic val1 = _innerTypeConverter.ConvertFromString(context, culture, strings[0]);
                dynamic val2 = _innerTypeConverter.ConvertFromString(context, culture, strings[1]);
                var rangeType = _genericInstanceType;
                return Activator.CreateInstance(rangeType, val1, val2);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
   {
      if (value != null && destinationType == typeof(System.String))
      {
          MethodInfo castMethod = GetType().GetMethod("Cast").MakeGenericMethod(_genericInstanceType);
          dynamic val = castMethod.Invoke(null, new[] { value });
         return _innerTypeConverter.ConvertTo(val.Start, destinationType) + "-" + _innerTypeConverter.ConvertTo(val.End, destinationType);
      }   
      return base.ConvertTo(context, culture, value, destinationType);
   }

        public static T Cast<T>(object o)
        {
            return (T) o;
        }
    }
}
