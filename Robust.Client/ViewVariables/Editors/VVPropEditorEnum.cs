using System;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Robust.Client.ViewVariables.Editors
{
    sealed class VVPropEditorEnum : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            DebugTools.Assert(value!.GetType().IsEnum);
            var enumType = value.GetType();
            var enumList = Enum.GetValues(enumType);

            var optionButton = new OptionButton();
            bool hasValue = false;
            foreach (var val in enumList)
            {
                var label = val?.ToString();
                if (label == null)
                    continue;
                optionButton.AddItem(label, Convert.ToInt32(val));
                hasValue |= Convert.ToInt32(val) == Convert.ToInt32(value);
            }

            // TODO properly support enum flags
            if (!hasValue)
                optionButton.AddItem(value.ToString() ?? string.Empty, Convert.ToInt32(value));

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
