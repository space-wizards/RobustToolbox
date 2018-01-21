using SS14.Client.Graphics;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.UserInterface.Controls;
using SS14.Client.Utility;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.CustomControls
{
    class OptionsMenu : SS14Window
    {
        Button ApplyButton;
        Button VSyncCheckBox;
        Button FullscreenCheckBox;
        private IConfigurationManager configManager;

        protected override Godot.Control SpawnSceneControl()
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load("res://Scenes/OptionsMenu/OptionsMenu.tscn");
            return (Godot.Control)res.Instance();
        }

        protected override void Initialize()
        {
            base.Initialize();
            configManager = IoCManager.Resolve<IConfigurationManager>();

            var vbox = Contents.GetChild("VBoxContainer");
            ApplyButton = vbox.GetChild<Button>("ApplyButton");
            ApplyButton.OnPressed += OnApplyButtonPressed;

            VSyncCheckBox = vbox.GetChild<Button>("VSyncCheckBox");
            VSyncCheckBox.OnToggled += OnVSyncCheckBoxToggled;

            FullscreenCheckBox = vbox.GetChild<Button>("FullscreenCheckBox");
            FullscreenCheckBox.OnToggled += OnFullscreenCheckBoxToggled;

            VSyncCheckBox.Pressed = configManager.GetCVar<bool>("display.vsync");
            FullscreenCheckBox.Pressed = ConfigIsFullscreen;
        }

        private void OnApplyButtonPressed(BaseButton.ButtonEventArgs args)
        {
            configManager.SetCVar("display.vsync", VSyncCheckBox.Pressed);
            configManager.SetCVar("display.windowmode", (int)(FullscreenCheckBox.Pressed ? WindowMode.Fullscreen : WindowMode.Windowed));
            configManager.SaveToFile();
            UpdateApplyButton();
            IoCManager.Resolve<IDisplayManager>().ReadConfig();
        }

        private void OnVSyncCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
        {
            UpdateApplyButton();
        }

        private void OnFullscreenCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
        {
            UpdateApplyButton();
        }

        private void UpdateApplyButton()
        {
            bool isvsyncsame = VSyncCheckBox.Pressed == configManager.GetCVar<bool>("display.vsync");
            bool isfullscreensame = FullscreenCheckBox.Pressed == ConfigIsFullscreen;
            ApplyButton.Disabled = isvsyncsame && isfullscreensame;
        }

        private bool ConfigIsFullscreen => configManager.GetCVar<int>("display.windowmode") == (int)WindowMode.Fullscreen;
    }
}
