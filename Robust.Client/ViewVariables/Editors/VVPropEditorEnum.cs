using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Robust.Client.ViewVariables.Editors
{
    sealed class VVPropEditorEnum : VVPropEditor
    {
        private Dictionary<int, int> _idToValue = new();

        protected override Control MakeUI(object? value)
        {
            DebugTools.Assert(value!.GetType().IsEnum);
            var enumType = value.GetType();
            var enumList = Enum.GetValues(enumType);
            var enumNames = Enum.GetNames(enumType);

            var convertedValue = Convert.ToInt32(value);

            var optionButton = new OptionButton();
            var hasValue = false;
            var valueIndex = 0;
            var i = 0;
            foreach (var val in enumList)
            {
                var label = enumNames[i];
                var entry = Convert.ToInt32(val);
                _idToValue.Add(i, entry);
                optionButton.AddItem(label, i);
                if (entry == convertedValue)
                {
                    hasValue = true;
                    valueIndex = i;
                }
                i += 1;
            }

            // Handle 0 value of flags or weird enum values.
            if (!hasValue)
            {
                valueIndex = _idToValue.Count;
                _idToValue.Add(valueIndex, convertedValue);
                optionButton.AddItem(value.ToString() ?? string.Empty, valueIndex);
            }

            optionButton.SelectId(valueIndex);
            optionButton.Disabled = ReadOnly;

            if (!ReadOnly)
            {
                var underlyingType = Enum.GetUnderlyingType(value.GetType());
                optionButton.OnItemSelected += e =>
                {
                    optionButton.SelectId(e.Id);
                    ValueChanged(Convert.ChangeType(_idToValue[e.Id], underlyingType));
                };
            }

            return optionButton;
        }
    }
}
