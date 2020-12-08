#nullable enable
using System;
using System.Globalization;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.ViewVariables.Editors
{
    internal sealed class VVPropEditorNumeric : VVPropEditor
    {
        private readonly NumberType _type;

        public VVPropEditorNumeric(NumberType type)
        {
            _type = type;
        }

        protected override Control MakeUI(object? value)
        {
            var lineEdit = new LineEdit
            {
                Text = NumberToText(value!),
                Editable = !ReadOnly,
                CustomMinimumSize = new Vector2(240, 0)
            };
            lineEdit.OnTextEntered += e =>
            {
                if (TryTextToNumber(e.Text, _type, out var val))
                    ValueChanged(val);
            };
            return lineEdit;
        }

        private static bool TryTextToNumber(string text, NumberType type, out object? val)
        {
            try
            {
                val = type switch
                {
                    NumberType.Byte => byte.Parse(text, CultureInfo.InvariantCulture),
                    NumberType.SByte => sbyte.Parse(text, CultureInfo.InvariantCulture),
                    NumberType.UShort => ushort.Parse(text, CultureInfo.InvariantCulture),
                    NumberType.Short => short.Parse(text, CultureInfo.InvariantCulture),
                    NumberType.UInt => uint.Parse(text, CultureInfo.InvariantCulture),
                    NumberType.Int => int.Parse(text, CultureInfo.InvariantCulture),
                    NumberType.ULong => ulong.Parse(text, CultureInfo.InvariantCulture),
                    NumberType.Long => long.Parse(text, CultureInfo.InvariantCulture),
                    NumberType.Float => float.Parse(text, CultureInfo.InvariantCulture),
                    NumberType.Double => double.Parse(text, CultureInfo.InvariantCulture),
                    NumberType.Decimal => decimal.Parse(text, CultureInfo.InvariantCulture),
                    _ => throw new ArgumentOutOfRangeException(),
                };
                return true;
            }
            catch (FormatException)
            {
                val = null;
                return false;
            }
        }

        private static string NumberToText(object val)
        {
            // All numeric primitives implement IConvertible.
            var convert = (IConvertible) val;
            return convert.ToString(CultureInfo.InvariantCulture);
        }

        public enum NumberType : byte
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
