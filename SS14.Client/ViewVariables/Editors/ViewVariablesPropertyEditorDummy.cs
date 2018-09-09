using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Log;

namespace SS14.Client.ViewVariables.Editors
{
    /// <summary>
    ///     Just writes out the ToString of the object.
    ///     The ultimate fallback.
    /// </summary>
    internal sealed class ViewVariablesPropertyEditorDummy : ViewVariablesPropertyEditor
    {
        protected override Control MakeUI(object value)
        {
            if (!ReadOnly)
            {
                Logger.WarningS("vv", "ViewVariablesPropertyEditorDummy being selected for editable field.");
            }
            return new Label
            {
                Text = value == null ? "null" : value.ToString(),
                Align = Label.AlignMode.Right,
                SizeFlagsHorizontal = Control.SizeFlags.FillExpand,
            };
        }
    }
}
