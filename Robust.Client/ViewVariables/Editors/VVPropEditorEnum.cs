using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables.Editors
{
    class VVPropEditorEnum : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            DebugTools.Assert(value!.GetType().IsEnum);
            var enumType = value.GetType();
            var enumList = Enum.GetValues(enumType);

            var optionButton = new OptionButton();
            foreach (var val in enumList)
            {
                var label = val?.ToString();
                if (label == null)
                    continue;
                optionButton.AddItem(label, (int?)val);
            }

            optionButton.SelectId((int)value);
            optionButton.Disabled = ReadOnly;

            if (!ReadOnly)
            {
                optionButton.OnItemSelected += e =>
                {
                    optionButton.SelectId(e.Id);
                    ValueChanged(e.Id);
                };
            }

            return optionButton;
        }
    }
}
