using System;
using System.Globalization;
using System.Reflection;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.ViewVariables.Editors
{
    class ViewVariablesPropertyEditorEnum : ViewVariablesPropertyEditor
    {
        protected override Control MakeUI(object? value)
        {
            DebugTools.Assert(value!.GetType().IsEnum);
            var enumVal = (Enum)value;
            var enumType = value.GetType();
            var enumStorageType = enumType.GetEnumUnderlyingType();

            var hBox = new HBoxContainer
            {
                CustomMinimumSize = new Vector2(200, 0)
            };

            var lineEdit = new LineEdit
            {
                Text = enumVal.ToString(),
                Editable = !ReadOnly,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand
            };

            if (!ReadOnly)
            {
                lineEdit.OnTextEntered += e =>
                {
                    var parseSig = new []{typeof(string), typeof(NumberStyles), typeof(CultureInfo), enumStorageType.MakeByRefType()};
                    var parseMethod = enumStorageType.GetMethod("TryParse", parseSig);
                    DebugTools.AssertNotNull(parseMethod);

                    var parameters = new object?[] {e.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, null};
                    var parseWorked = (bool)parseMethod!.Invoke(null, parameters)!;

                    if (parseWorked) // textbox was the underlying type
                    {
                        DebugTools.AssertNotNull(parameters[3]);
                        ValueChanged(parameters[3]);
                    }
                    else if(Enum.TryParse(enumType, e.Text, true, out var enumValue))
                    {
                        var underlyingVal = Convert.ChangeType(enumValue, enumStorageType);
                        ValueChanged(underlyingVal);
                    }
                };
            }

            hBox.AddChild(lineEdit);
            return hBox;
        }
    }
}
