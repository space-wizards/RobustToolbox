using Robust.Client.Graphics;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.Label;
using Robust.Shared.Localization;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A type of toggleable button that a switch icon and a secondary text label both showing the current state
    /// </summary>
    [Virtual]
    public class SwitchButton : ContainerButton
    {
        public const string StyleClassSwitchButton = "switchButton";

        public const string StylePropertyTextureUnchecked = "textureUnchecked";
        public const string StylePropertyTextureChecked = "textureChecked";

        public Label Label { get; }
        public Label OffStateLabel { get; }
        public Label OnStateLabel { get; }

        // I think PanelContainer is the simplest container; it is only used here
        // so the labels can reserve overlapping spaces, so that switching which one is
        // visible when the button is clicked doesn't affect the texture position or the
        // surrounding layout
        public PanelContainer StateLabelsContainer { get; }
        public TextureRect TextureRect { get; }

        public SwitchButton()
        {
            ToggleMode = true;

            var hBox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                StyleClasses = { StyleClassSwitchButton }
            };
            AddChild(hBox);

            TextureRect = new TextureRect
            {
                StyleClasses = { StyleClassSwitchButton },
                VerticalAlignment = VAlignment.Center,
            };

            Label = new Label();
            Label.Visible = false;

            OffStateLabel = new Label();
            OffStateLabel.Text = Loc.GetString("toggle-switch-default-off-state-label");
            OffStateLabel.ReservesSpace = true;

            OnStateLabel = new Label();
            OnStateLabel.Text = Loc.GetString("toggle-switch-default-on-state-label");
            OnStateLabel.ReservesSpace = true;
            OnStateLabel.Visible = false;

            StateLabelsContainer = new PanelContainer();
            StateLabelsContainer.HorizontalExpand = false;  // will change if text added for Label
            StateLabelsContainer.AddChild(OffStateLabel);
            StateLabelsContainer.AddChild(OnStateLabel);

            Label.HorizontalExpand = true;
            hBox.AddChild(Label);
            hBox.AddChild(TextureRect);
            hBox.AddChild(StateLabelsContainer);
        }

        protected override void DrawModeChanged()
        {
            base.DrawModeChanged();
            UpdateAppearance();
        }

        /// <summary>
        ///     If true, the button will allow shrinking and clip text of the main
        ///     label to prevent the text from going outside the bounds of the button.
        ///     If false, the minimum size will always fit the contained text.
        /// </summary>
        [ViewVariables]
        public bool ClipText { get => Label.ClipText; set => Label.ClipText = value; }

        /// <summary>
        ///     The text displayed by the button's main label.
        /// </summary>
        [ViewVariables]
        public string? Text
        {
            get => Label.Text;
            set
            {
                Label.Text = value;
                if (string.IsNullOrEmpty(value))
                {
                    Label.Visible = false;
                    StateLabelsContainer.HorizontalExpand = true;
                }
                else
                {
                    Label.Visible = true;
                    StateLabelsContainer.HorizontalExpand = false;
                }
            }
        }

        /// <summary>
        ///     The text displayed by the button's secondary label in the off state.
        /// </summary>
        [ViewVariables]
        public string? OffStateText
        {
            get => OffStateLabel.Text;
            set => OffStateLabel.Text = value;
        }

        /// <summary>
        ///     The text displayed by the button's secondary label in the on state.
        /// </summary>
        [ViewVariables]
        public string? OnStateText
        {
            get => OnStateLabel.Text;
            set => OnStateLabel.Text = value;
        }

        private void UpdateAppearance()
        {
            if ( Pressed )
            {
                TryGetStyleProperty(StylePropertyTextureChecked, out Texture? texture);
                if (texture is not null && TextureRect is not null)
                {
                    TextureRect.Texture = texture;
                }

                if (OffStateLabel is not null)
                    OffStateLabel.Visible = false;
                if (OnStateLabel is not null)
                    OnStateLabel.Visible = true;
            }
            else
            {
                TryGetStyleProperty(StylePropertyTextureUnchecked, out Texture? texture);
                if (texture is not null && TextureRect is not null)
                {
                    TextureRect.Texture = texture;
                }

                if (OffStateLabel is not null)
                    OffStateLabel.Visible = true;
                if (OnStateLabel is not null)
                    OnStateLabel.Visible = false;
            }
        }

        protected override void StylePropertiesChanged()
        {
            UpdateAppearance();
            base.StylePropertiesChanged();
        }
    }
}
