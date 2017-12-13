using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Render;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.State.States
{
    /// <summary>
    ///     Options screen that displays all in-game options that can be changed.
    /// </summary>
    // Instantiated dynamically through the StateManager.
    public class OptionsMenu : State
    {
        private readonly Dictionary<string, VideoMode> _videoModeList = new Dictionary<string, VideoMode>();
        private Panel _bgPanel;

        private Button _btnApply;
        private Button _btnBack;

        private Checkbox _chkFullScreen;
        private Checkbox _chkVSync;
        private Label _lblFullScreen;
        private Label _lblTitle;
        private Label _lblVSync;
        private Listbox _lstResolution;
        private Screen _uiScreen;

        /// <summary>
        ///     Constructs an instance of this object.
        /// </summary>
        /// <param name="managers">A dictionary of common managers from the IOC system, so you don't have to resolve them yourself.</param>
        public OptionsMenu(IDictionary<Type, object> managers)
            : base(managers) { }

        /// <inheritdoc />
        public override void Startup()
        {
            UserInterfaceManager.AddComponent(_uiScreen);
            FormResize();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            UserInterfaceManager.RemoveComponent(_uiScreen);
        }

        /// <inheritdoc />
        public override void FormResize()
        {
            _uiScreen.Width = (int)CluwneLib.Window.Viewport.Size.X;
            _uiScreen.Height = (int)CluwneLib.Window.Viewport.Size.Y;

            UserInterfaceManager.ResizeComponents();
        }

        /// <inheritdoc />
        public override void KeyDown(KeyEventArgs e)
        {
            UserInterfaceManager.KeyDown(e);
        }

        /// <inheritdoc />
        public override void MouseUp(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }

        /// <inheritdoc />
        public override void MouseDown(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }

        /// <inheritdoc />
        public override void MousePressed(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }

        /// <inheritdoc />
        public override void MouseMove(MouseMoveEventArgs e)
        {
            UserInterfaceManager.MouseMove(e);
        }

        /// <inheritdoc />
        public override void MouseWheelMove(MouseWheelScrollEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        }

        /// <inheritdoc />
        public override void MouseEntered(EventArgs e)
        {
            UserInterfaceManager.MouseEntered(e);
        }

        /// <inheritdoc />
        public override void MouseLeft(EventArgs e)
        {
            UserInterfaceManager.MouseLeft(e);
        }

        /// <inheritdoc />
        public override void TextEntered(TextEventArgs e)
        {
            UserInterfaceManager.TextEntered(e);
        }

        /// <inheritdoc />
        public override void InitializeGUI()
        {
            _uiScreen = new Screen();
            _uiScreen.BackgroundImage = ResourceCache.GetSprite("ss14_logo_background");
            // added to interface manager in startup

            _bgPanel = new Panel();
            _bgPanel.BackgroundImage = ResourceCache.GetResource<SpriteResource>(@"Textures/UserInterface/TicketOverlay.png");
            _bgPanel.BackgroundColor = new Color(128, 128, 128, 128);
            _bgPanel.Alignment = Align.HCenter | Align.VCenter;
            _bgPanel.Layout += (sender, args) =>
            {
                _bgPanel.Width = (int)(_uiScreen.Width * 0.85f);
                _bgPanel.Height = (int)(_uiScreen.Height * 0.85f);
            };
            _uiScreen.AddControl(_bgPanel);

            _lblTitle = new Label("Options", "CALIBRI", 48);
            _lblTitle.LocalPosition = new Vector2i(10, 10);
            _bgPanel.AddControl(_lblTitle);

            _lstResolution = new Listbox(250, 150);
            _lstResolution.Alignment = Align.Bottom;
            _lstResolution.LocalPosition = new Vector2i(50, 50);
            _lstResolution.ItemSelected += _lstResolution_ItemSelected;
            PopulateAvailableVideoModes(_lstResolution);
            _lblTitle.AddControl(_lstResolution);

            _chkFullScreen = new Checkbox();
            _chkFullScreen.Value = ConfigurationManager.GetCVar<bool>("display.fullscreen");
            _chkFullScreen.ValueChanged += _chkFullScreen_ValueChanged;
            _chkFullScreen.Alignment = Align.Bottom;
            _chkFullScreen.LocalPosition = new Vector2i(0, 50);
            _lstResolution.AddControl(_chkFullScreen);

            _lblFullScreen = new Label("Fullscreen", "CALIBRI");
            _lblFullScreen.Alignment = Align.Right;
            _lblFullScreen.LocalPosition = new Vector2i(3, 0);
            _chkFullScreen.AddControl(_lblFullScreen);

            _chkVSync = new Checkbox();
            _chkVSync.Value = ConfigurationManager.GetCVar<bool>("display.vsync");
            _chkVSync.ValueChanged += _chkVSync_ValueChanged;
            _chkVSync.Alignment = Align.Bottom;
            _chkVSync.LocalPosition = new Vector2i(0, 3);
            _chkFullScreen.AddControl(_chkVSync);

            _lblVSync = new Label("Vsync", "CALIBRI");
            _lblVSync.Alignment = Align.Right;
            _lblVSync.LocalPosition = new Vector2i(3, 0);
            _chkVSync.AddControl(_lblVSync);

            _btnApply = new Button("Apply Settings");
            _btnApply.Clicked += _btnApply_Clicked;
            _btnApply.Alignment = Align.Bottom | Align.Right;
            _btnApply.Resize += (sender, args) => { _btnApply.LocalPosition = new Vector2i(-10 + -_btnApply.ClientArea.Width, -10 + -_btnApply.ClientArea.Height); };
            _bgPanel.AddControl(_btnApply);

            _btnBack = new Button("Back");
            _btnBack.Clicked += _btnBack_Clicked;
            _btnBack.Resize += (sender, args) => { _btnBack.LocalPosition = new Vector2i(-10 + -_btnBack.ClientArea.Width, 0); };
            _btnApply.AddControl(_btnBack);
        }

        private void PopulateAvailableVideoModes(Listbox resListBox)
        {
            resListBox.ClearItems();
            _videoModeList.Clear();

            var modes = VideoMode.FullscreenModes
                .Where(v => v.Height > 748 && v.Width > 1024)
                .OrderBy(v => v.Height * v.Width)
                .ToList();

            if (!modes.Any())
                throw new InvalidOperationException("No available video modes");

            foreach (var vm in modes)
            {
                if (!_videoModeList.ContainsKey(GetVmString(vm)))
                {
                    _videoModeList.Add(GetVmString(vm), vm);
                    resListBox.AddItem(GetVmString(vm));
                }
            }

            if (_videoModeList.Any(x =>
                x.Value.Width == CluwneLib.Window.Viewport.Size.X &&
                x.Value.Height == CluwneLib.Window.Viewport.Size.Y))
            {
                var currentMode = _videoModeList.FirstOrDefault(x =>
                    x.Value.Width == CluwneLib.Window.Viewport.Size.X &&
                    x.Value.Height == CluwneLib.Window.Viewport.Size.Y);

                resListBox.SelectItem(currentMode.Key);
            }
            else
            {
                //No match due to different refresh rate in windowed mode. Just pick first resolution based on size only.
                var currentMode =
                    _videoModeList.FirstOrDefault(
                        x =>
                            x.Value.Width == CluwneLib.Window.Viewport.Size.X &&
                            x.Value.Height == CluwneLib.Window.Viewport.Size.Y);
                resListBox.SelectItem(currentMode.Key);
            }
        }

        private void _chkVSync_ValueChanged(bool newValue, Checkbox sender)
        {
            ConfigurationManager.SetCVar("display.vsync", newValue);
        }

        private void _chkFullScreen_ValueChanged(bool newValue, Checkbox sender)
        {
            ConfigurationManager.SetCVar("display.fullscreen", newValue);
        }

        private void _lstResolution_ItemSelected(Label item, Listbox sender)
        {
            if (_videoModeList.ContainsKey(item.Text))
            {
                var sel = _videoModeList[item.Text];
                ConfigurationManager.SetCVar("display.width", (int)sel.Width);
                ConfigurationManager.SetCVar("display.height", (int)sel.Height);

                CluwneLib.UpdateVideoSettings();
                FormResize();
            }
        }

        private string GetVmString(VideoMode vm)
        {
            return $"{vm.Width} x {vm.Height} @ {vm.BitsPerPixel}bpp";
        }

        private void _btnApply_Clicked(Button sender)
        {
            CluwneLib.UpdateVideoSettings();
        }

        private void _btnBack_Clicked(Button sender)
        {
            StateManager.RequestStateChange<MainScreen>();
        }
    }
}
