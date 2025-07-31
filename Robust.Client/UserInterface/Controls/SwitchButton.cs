using Robust.Client.Graphics;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

using static Robust.Client.UserInterface.Controls.Label;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A type of toggleable button that a switch icon and a secondary text label both showing the current state
    /// </summary>
    [Virtual]
    public class SwitchButton : ContainerButton
    {
        public const string StyleClassSwitchButton = "switchButton";
        public new const string StylePseudoClassPressed = "pressed";
        public new const string StylePseudoClassDisabled = "disabled";

        public const string StylePropertyIconTexture = "icon-texture";
        public const string StylePropertyFontColor = "font-color";

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
            if (Disabled)
            {
                AddStylePseudoClass(StylePseudoClassDisabled);
            }
            else
            {
                RemoveStylePseudoClass(StylePseudoClassDisabled);
            }

            if (Pressed)
            {
                AddStylePseudoClass(StylePseudoClassPressed);
            }
            else
            {
                RemoveStylePseudoClass(StylePseudoClassPressed);
            }

            // no base.DrawModeChanged() call - ContainerButton's pseudoclass handling
            // doesn't support a button being both pressed and disabled
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
            TryGetStyleProperty(StylePropertyIconTexture, out Texture? texture);
            if (texture is not null && TextureRect is not null)
            {
                TextureRect.Texture = texture;
            }

            TryGetStyleProperty(StylePropertyFontColor, out Color? fontColor);
            if (fontColor is not null)
            {
                if (Label is not null)
                {
                    Label.FontColorOverride = fontColor;
                }
                if (OffStateLabel is not null)
                {
                    OffStateLabel.FontColorOverride = fontColor;
                }
                if (OnStateLabel is not null)
                {
                    OnStateLabel.FontColorOverride = fontColor;
                }
            }

            if (OffStateLabel is not null)
            {
                OffStateLabel.Visible = !Pressed;
            }

            if (OnStateLabel is not null)
            {
                OnStateLabel.Visible = Pressed;
            }
        }

        protected override void StylePropertiesChanged()
        {
            UpdateAppearance();
            base.StylePropertiesChanged();
        }
    }
}
