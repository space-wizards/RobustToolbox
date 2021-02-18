using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.Label;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A type of toggleable button that also has a checkbox.
    /// </summary>
    public class CheckBox : ContainerButton
    {
        public const string StyleClassCheckBox = "checkBox";
        public const string StyleClassCheckBoxChecked = "checkBoxChecked";

        public Label Label { get; }
        public TextureRect TextureRect { get; }

        public CheckBox() : base()
        {
            ToggleMode = true;

            var hBox = new HBoxContainer
            {
                StyleClasses = { StyleClassCheckBox },
            };
            AddChild(hBox);

            TextureRect = new TextureRect
            {
                StyleClasses = { StyleClassCheckBox },
            };
            hBox.AddChild(TextureRect);

            Label = new Label();
            hBox.AddChild(Label);
        }

        protected override void DrawModeChanged()
        {
            base.DrawModeChanged();

            if (TextureRect != null)
            {
                if (Pressed)
                    TextureRect.AddStyleClass(StyleClassCheckBoxChecked);
                else
                    TextureRect.RemoveStyleClass(StyleClassCheckBoxChecked);
            }
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
