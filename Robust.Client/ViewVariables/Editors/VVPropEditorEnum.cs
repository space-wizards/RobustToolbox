using System;
using System.Globalization;
using System.Reflection;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.ViewVariables.Editors
{
    class VVPropEditorEnum : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            DebugTools.Assert(value!.GetType().IsEnum);
            var enumType = value.GetType();
            var enumList = Enum.GetValues(enumType);

            var hBox = new HBoxContainer
            {
                CustomMinimumSize = new Vector2(200, 0)
            };

            var optionButton = new OptionButton();
            foreach (var val in enumList)
            {
                if(val == null)
                    continue;
                optionButton.AddItem(val.ToString()!, (int)val!);
            }

            optionButton.SelectId((int)value);
            optionButton.Disabled = ReadOnly;

            if (!ReadOnly)
            {
                optionButton.OnItemSelected += e =>
                {
                    ValueChanged(e.Id);
                };
            }

            hBox.AddChild(optionButton);
            return hBox;
        }
    }
}
