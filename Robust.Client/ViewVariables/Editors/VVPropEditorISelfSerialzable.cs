using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using System;
using Robust.Shared.Serialization;

namespace Robust.Client.ViewVariables.Editors
{
    internal sealed class VVPropEditorISelfSerialzable<T> : VVPropEditor where T : ISelfSerialize
    {
        protected override Control MakeUI(object? value)
        {
            var lineEdit = new LineEdit
            {
                Text = ((ISelfSerialize)value!).Serialize(),
                Editable = !ReadOnly,
                HorizontalExpand = true,
            };

            if (!ReadOnly)
            {
                lineEdit.OnTextEntered += e =>
                {
                    var instance = (ISelfSerialize)Activator.CreateInstance(typeof(T))!;
                    instance.Deserialize(e.Text);
                    ValueChanged(instance);
                };
            }

            return lineEdit;
        }
    }
}
