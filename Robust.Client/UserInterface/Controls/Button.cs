using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.Label;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Most common button type that draws text in a fancy box.
    /// </summary>
    [Virtual]
    public class Button : ContainerButton
    {
        public Label Label { get; }

        public Button()
        {
            AddStyleClass(StyleClassButton);
            Label = new Label
            {
                StyleClasses = { StyleClassButton }
            };
            AddChild(Label);
        }

        protected override void StylePropertiesChanged()
        {
            base.StylePropertiesChanged();

			// Temporary workaround to fix button styles in content thanks to a lack of correct style updating for child selectors.
			// The changing of the style class on the parent control (button in this case) doesn't cause a style update in the label correctly currently.
			// This works around that.
            Label.Restyle();
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
        public string? Text { get => Label.Text; set => Label.Text = value; }
    }
}
