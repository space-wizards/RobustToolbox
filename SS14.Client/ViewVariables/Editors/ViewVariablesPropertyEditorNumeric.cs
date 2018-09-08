using System;
using System.Globalization;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.ViewVariables.Editors
{
    internal sealed class ViewVariablesPropertyEditorNumeric : ViewVariablesPropertyEditor
    {
        private readonly NumberType _type;

        public ViewVariablesPropertyEditorNumeric(NumberType type)
        {
            _type = type;
        }

        protected override Control MakeUI(object value)
        {
            var lineEdit = new LineEdit
            {
                Text = NumberToText(value),
                Editable = !ReadOnly,
                CustomMinimumSize = new Vector2(240, 0)
            };
            lineEdit.OnTextEntered += e => ValueChanged(TextToNumber(e.Text, _type));
            return lineEdit;
        }

        private static object TextToNumber(string text, NumberType type)
        {
            switch (type)
            {
                case NumberType.Byte:
                    return byte.Parse(text, CultureInfo.InvariantCulture);
                case NumberType.SByte:
                    return sbyte.Parse(text, CultureInfo.InvariantCulture);
                case NumberType.UShort:
                    return ushort.Parse(text, CultureInfo.InvariantCulture);
                case NumberType.Short:
                    return short.Parse(text, CultureInfo.InvariantCulture);
                case NumberType.UInt:
                    return uint.Parse(text, CultureInfo.InvariantCulture);
                case NumberType.Int:
                    return int.Parse(text, CultureInfo.InvariantCulture);
                case NumberType.ULong:
                    return ulong.Parse(text, CultureInfo.InvariantCulture);
                case NumberType.Long:
                    return long.Parse(text, CultureInfo.InvariantCulture);
                case NumberType.Float:
                    return float.Parse(text, CultureInfo.InvariantCulture);
                case NumberType.Double:
                    return double.Parse(text, CultureInfo.InvariantCulture);
                case NumberType.Decimal:
                    return decimal.Parse(text, CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string NumberToText(object val)
        {
            // All numeric primitives implement IConvertible.
            var convert = (IConvertible) val;
            return convert.ToString(CultureInfo.InvariantCulture);
        }

        public enum NumberType
        {
            Byte,
            SByte,
            UShort,
            Short,
            UInt,
            Int,
            ULong,
            Long,
            Float,
            Double,
            Decimal,
        }
    }
}
