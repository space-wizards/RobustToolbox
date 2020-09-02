using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class OptionsMenu : SS14Window
    {
        private readonly Button ApplyButton;
        private readonly CheckBox VSyncCheckBox;
        private readonly CheckBox FullscreenCheckBox;
        private readonly Slider LightingQualitySlider;
        private readonly CheckBox SoftShadowsCheckBox;
        private readonly IConfigurationManager configManager;

        protected override Vector2? CustomSize => (180, 160);

        public OptionsMenu(IConfigurationManager configMan)
        {
            configManager = configMan;

            Title = "Options";

            var vBox = new VBoxContainer();
            Contents.AddChild(vBox);
            //vBox.SetAnchorAndMarginPreset(LayoutPreset.Wide);

            VSyncCheckBox = new CheckBox {Text = "VSync"};
            vBox.AddChild(VSyncCheckBox);
            VSyncCheckBox.OnToggled += OnCheckBoxToggled;

            FullscreenCheckBox = new CheckBox {Text = "Fullscreen"};
            vBox.AddChild(FullscreenCheckBox);
            FullscreenCheckBox.OnToggled += OnCheckBoxToggled;

            vBox.AddChild(new Label {Text = "Light Resolution"});

            LightingQualitySlider = new Slider {MinValue = 0, MaxValue = 2, Rounded = true};
            vBox.AddChild(LightingQualitySlider);
            LightingQualitySlider.OnValueChanged += OnLightingQualityChanged;

            SoftShadowsCheckBox = new CheckBox {Text = "Soft Shadows"};
            vBox.AddChild(SoftShadowsCheckBox);
            SoftShadowsCheckBox.OnToggled += OnCheckBoxToggled;

            ApplyButton = new Button
            {
                Text = "Apply", TextAlign = Label.AlignMode.Center,
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };
            vBox.AddChild(ApplyButton);
            ApplyButton.OnPressed += OnApplyButtonPressed;

            VSyncCheckBox.Pressed = configManager.GetCVar<bool>("display.vsync");
            FullscreenCheckBox.Pressed = ConfigIsFullscreen;
            LightingQualitySlider.Value = ConfigLightingQuality;
            SoftShadowsCheckBox.Pressed = configManager.GetCVar<bool>("display.softshadows");
        }

        private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
        {
            configManager.SetCVar("display.vsync", VSyncCheckBox.Pressed);
            ConfigLightingQuality = (int) LightingQualitySlider.Value;
            configManager.SetCVar("display.softshadows", SoftShadowsCheckBox.Pressed);
            configManager.SetCVar("display.windowmode",
                (int) (FullscreenCheckBox.Pressed ? WindowMode.Fullscreen : WindowMode.Windowed));
            configManager.SaveToFile();
            UpdateApplyButton();
        }

        private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
        {
            UpdateApplyButton();
        }

        private void OnLightingQualityChanged(Range rng)
        {
            UpdateApplyButton();
        }

        private void UpdateApplyButton()
        {
            var isVSyncSame = VSyncCheckBox.Pressed == configManager.GetCVar<bool>("display.vsync");
            var isFullscreenSame = FullscreenCheckBox.Pressed == ConfigIsFullscreen;
            var isLightingQualitySame = ((int) LightingQualitySlider.Value) == ConfigLightingQuality;
            var isSoftShadowsSame = SoftShadowsCheckBox.Pressed == configManager.GetCVar<bool>("display.softshadows");
            ApplyButton.Disabled = isVSyncSame && isFullscreenSame && isLightingQualitySame && isSoftShadowsSame;
        }

        private bool ConfigIsFullscreen =>
            configManager.GetCVar<int>("display.windowmode") == (int) WindowMode.Fullscreen;

        private int ConfigLightingQuality
        {
            get
            {
                var val = configManager.GetCVar<int>("display.lightmapdivider");
                if (val < 2)
                {
                    return 2;
                }
                else if (val < 3)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (value == 0)
                {
                    configManager.SetCVar("display.lightmapdivider", 8);
                }
                else if (value == 1)
                {
                    configManager.SetCVar("display.lightmapdivider", 2);
                }
                else
                {
                    configManager.SetCVar("display.lightmapdivider", 1);
                }
            }
        }
    }
}
