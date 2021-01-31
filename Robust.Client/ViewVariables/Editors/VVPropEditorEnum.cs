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
                optionButton.AddItem(label, Convert.ToInt32(val));
            }

            optionButton.SelectId(Convert.ToInt32(value));
            optionButton.Disabled = ReadOnly;

            if (!ReadOnly)
            {
                var underlyingType = Enum.GetUnderlyingType(value.GetType());
                optionButton.OnItemSelected += e =>
                {
                    optionButton.SelectId(e.Id);
                    ValueChanged(Convert.ChangeType(e.Id, underlyingType));
                };
            }

            return optionButton;
        }
    }
}
