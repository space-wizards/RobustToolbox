using System;
using System.Collections.Generic;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Robust.Client.ViewVariables.Editors
{
    sealed class VVPropEditorEnum : VVPropEditor
    {
        private readonly Dictionary<int, int> _idToValue = new();
        private readonly Dictionary<int, int> _valueToId = new();

        private readonly Dictionary<int, Button> _buttons = new();

        private int _invalidOptionId;

        private int _value;
        private bool _flagEnum;

        protected override Control MakeUI(object? value)
        {
            DebugTools.Assert(value!.GetType().IsEnum);
            var enumType = value.GetType();
            var enumList = Enum.GetValues(enumType);
            var enumNames = Enum.GetNames(enumType);
            var underlyingType = Enum.GetUnderlyingType(enumType);

            var convertedValue = Convert.ToInt32(value);

            var hBoxContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
            };

            var optionButton = new OptionButton();
            hBoxContainer.AddChild(optionButton);

            var hasValue = false;
            var selectedId = 0;
            var i = 0;
            foreach (var val in enumList)
            {
                var label = enumNames[i];
                var entry = Convert.ToInt32(val);
                _idToValue.Add(i, entry);
                _valueToId.TryAdd(entry, i);
                optionButton.AddItem(label, i);
                if (entry == convertedValue)
                {
                    hasValue = true;
                    selectedId = i;
                }
                i += 1;
            }

            var isFlags = enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;

            // Handle unnamed enum values.
            if (!hasValue || isFlags)
            {
                _invalidOptionId = i;
                _idToValue.Add(_invalidOptionId, convertedValue);
                optionButton.AddItem(string.Empty, _invalidOptionId);
                if (!hasValue)
                    selectedId = _invalidOptionId;
            }

            optionButton.SelectId(selectedId);
            optionButton.Disabled = ReadOnly;

            // Flags
            if (isFlags)
            {
                _flagEnum = true;
                var flags = 0;
                foreach (var val in enumList)
                {
                    var entry = Convert.ToInt32(val);
                    if ((entry & flags) != 0 || entry == 0)
                        continue;

                    flags |= entry;
                    var button = new Button
                    {
                        Text = enumNames[_valueToId[entry]],
                    };
                    _buttons.Add(entry, button);
                    hBoxContainer.AddChild(button);
                    button.ToggleMode = true;
                    if (!ReadOnly)
                    {
                        button.OnToggled += args =>
                        {
                            if (args.Pressed)
                                SelectButtons(_value | entry);
                            else
                                SelectButtons(_value & ~entry);
                        };
                    }
                }
            }

            if (!ReadOnly)
            {
                optionButton.OnItemSelected += e =>
                {
                    if (e.Id == _invalidOptionId)
                    {
                        optionButton.SelectId(_invalidOptionId);
                        return;
                    }

                    SelectButtons(_idToValue[e.Id]);
                };
            }

            SelectButtons(convertedValue, false);

            return hBoxContainer;

            void SelectButtons(int flags, bool changeValue = true)
            {
                _value = flags;
                if (_flagEnum)
                {
                    foreach (var (buttonFlags, button) in _buttons)
                    {
                        button.Pressed = (buttonFlags & flags) != 0;
                    }
                }

                if (!_valueToId.TryGetValue(flags, out var id)
                    || !optionButton.TrySelectId(id))
                    optionButton.SelectId(_invalidOptionId);

                if (changeValue)
                    ValueChanged(Convert.ChangeType(flags, underlyingType));
            }
        }
    }
}
