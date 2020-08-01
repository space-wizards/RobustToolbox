using System;
using System.ComponentModel;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables
{
    /// <summary>
    ///     An editor for the value of a property.
    /// </summary>
    public abstract class ViewVariablesPropertyEditor
    {
        /// <summary>
        ///     Invoked when the value was changed.
        /// </summary>
        internal event Action<object?>? OnValueChanged;

        protected bool ReadOnly { get; private set; }

        protected NumericDisplay DisplayOverride { get; private set; }

        public Control Initialize(object? value, bool readOnly, NumericDisplay memberDisplay)
        {
            ReadOnly = readOnly;
            DisplayOverride = memberDisplay;
            return MakeUI(value);
        }

        protected abstract Control MakeUI(object? value);

        protected void ValueChanged(object? newValue)
        {
            OnValueChanged?.Invoke(newValue);
        }
    }
}
