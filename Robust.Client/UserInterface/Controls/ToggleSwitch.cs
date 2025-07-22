using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.Label;
using Robust.Shared.Localization;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A type of toggleable button that a switch icon and a secondary text label both showing the current state
    /// </summary>
    [Virtual]
    public class ToggleSwitch : ContainerButton
    {
        public const string StyleClassToggleSwitch = "toggleSwitch";
        public const string StyleClassToggleSwitchOn = "toggleSwitchOn";


        public Label Label { get; }
        public Label OffStateLabel { get; }
        public Label OnStateLabel { get; }
        public TextureRect TextureRect { get; }

        public ToggleSwitch()
        {
            ToggleMode = true;

            var hBox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                StyleClasses = { StyleClassToggleSwitch },
            };
            AddChild(hBox);

            TextureRect = new TextureRect
            {
                StyleClasses = { StyleClassToggleSwitch },
                VerticalAlignment = VAlignment.Center,
            };

            Label = new Label();

            OffStateLabel = new Label();
            OffStateLabel.Text = Loc.GetString("toggle-switch-default-off-state-label");
            OffStateLabel.ReservesSpace = true;

            OnStateLabel = new Label();
            OnStateLabel.Text = Loc.GetString("toggle-switch-default-on-state-label");
            OnStateLabel.ReservesSpace = true;
            OnStateLabel.Visible = false;

            // I think PanelContainer is the simplest container; it is only used here
            // so the labels can reserve overlapping spaces, so that switching which one is
            // visible when the button is clicked doesn't affect the texture position or the
            // surrounding layout
            var stateLabelContainer = new PanelContainer();
            stateLabelContainer.AddChild(OffStateLabel);
            stateLabelContainer.AddChild(OnStateLabel);

            Label.HorizontalExpand = true;
            hBox.AddChild(Label);
            hBox.AddChild(TextureRect);
            hBox.AddChild(stateLabelContainer);
        }

        protected override void DrawModeChanged()
        {
            base.DrawModeChanged();

            if (Pressed)
            {
                TextureRect?.AddStyleClass(StyleClassToggleSwitchOn);
                if (OffStateLabel is not null)
                    OffStateLabel.Visible = false;
                if (OnStateLabel is not null)
                    OnStateLabel.Visible = true;
            }
            else
            {
                TextureRect?.RemoveStyleClass(StyleClassToggleSwitchOn);
                if (OffStateLabel is not null)
                    OffStateLabel.Visible = true;
                if (OnStateLabel is not null)
                    OnStateLabel.Visible = false;
            }
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
        public string? LabelText
        {
            get => Label.Text;
            set
            {
                Label.Text = value;
                Label.Visible = !string.IsNullOrEmpty(Label.Text);
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
    }
}
