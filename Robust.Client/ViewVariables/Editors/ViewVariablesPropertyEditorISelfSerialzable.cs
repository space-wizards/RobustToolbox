using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Serialization;
using System;

namespace Robust.Client.ViewVariables.Editors
{
    internal class ViewVariablesPropertyEditorISelfSerialzable<T> : ViewVariablesPropertyEditorString where T : ISelfSerialize
    {
        protected override void EventHandler(LineEdit.LineEditEventArgs e)
        {
            var instance = (ISelfSerialize)Activator.CreateInstance(typeof(T));
            instance.Deserialize(e.Text);
            ValueChanged(instance);
        }

        protected override string ToText(object value)
        {
            return base.ToText(((ISelfSerialize)value).ToString());
        }
    }
}
