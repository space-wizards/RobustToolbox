using System;
using System.ComponentModel;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;

namespace SS14.Client.ViewVariables
{
    /// <summary>
    ///     An editor for the value of a property.
    /// </summary>
    public abstract class ViewVariablesPropertyEditor
    {
        /// <summary>
        ///     Invoked when the value was changed.
        /// </summary>
        internal event Action<object> OnValueChanged;

        protected bool ReadOnly { get; private set; }

        public Control Initialize(object value, bool readOnly)
        {
            ReadOnly = readOnly;
            return MakeUI(value);
        }

        protected abstract Control MakeUI(object value);

        protected void ValueChanged(object newValue)
        {
            OnValueChanged?.Invoke(newValue);
        }
    }
}
