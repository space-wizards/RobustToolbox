using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.Label;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A type of toggleable button that also has a checkbox.
    /// </summary>
    public class CheckBox : ContainerButton
    {
        public const string StyleIdentifierCheckBoxChecked = "checkBoxChecked";
        public const string StyleIdentifierCheckBoxUnchecked = "checkBoxUnchecked";

        public Label Label { get; }
        public TextureRect TextureRect { get; }

        public CheckBox() : base()
        {
            ToggleMode = true;

            var hBox = new HBoxContainer
            {
                MouseFilter = MouseFilterMode.Ignore
            };
            AddChild(hBox);

            TextureRect = new TextureRect
            {
                MouseFilter = MouseFilterMode.Ignore,
                StyleIdentifier = StyleIdentifierCheckBoxUnchecked
            };
            hBox.AddChild(TextureRect);

            Label = new Label
            {
                MouseFilter = MouseFilterMode.Ignore
            };
            hBox.AddChild(Label);
        }

        protected override void DrawModeChanged()
        {
            base.DrawModeChanged();

            if (TextureRect == null)
            {
                return;
            }

            if (Pressed)
                TextureRect.StyleIdentifier = StyleIdentifierCheckBoxChecked;
            else
                TextureRect.StyleIdentifier = StyleIdentifierCheckBoxUnchecked;
        }

        /// <summary>
        ///     How to align the text inside the button.
        /// </summary>
        [ViewVariables]
        public AlignMode TextAlign { get => Label.Align; set => Label.Align = value; }

        /// <summary>
        ///     If true, the button will allow shrinking and clip text
        ///     to prevent the text from going outside the bounds of the button.
        ///     If false, the minimum size will always fit the contained text.
        /// </summary>
        [ViewVariables]
        public bool ClipText { get => Label.ClipText; set => Label.ClipText = value; }

        /// <summary>
        ///     The text displayed by the button.
        /// </summary>
        [ViewVariables]
        public string Text { get => Label.Text; set => Label.Text = value; }
    }
}
