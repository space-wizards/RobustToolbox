using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using SFML.Graphics;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Components;
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

        //private Box2i _boundingArea;
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

        private void InitializeGui()
        {
            _uiScreen = new Screen();
            _uiScreen.Position = new Vector2i(0, 0);
            _uiScreen.Background = ResourceCache.GetSprite("ss14_logo_background");
            UserInterfaceManager.AddComponent(_uiScreen);

            _bgPanel = new Panel();
            _bgPanel.Background = ResourceCache.GetSprite("ticketoverlay");
            _bgPanel.Background.Color = new Color(128, 128, 128, 128);
            _bgPanel.Alignment = Align.HCenter | Align.VCenter;
            _bgPanel.Layout += (sender, args) =>
            {
                _bgPanel.Width = (int) (_uiScreen.Width * 0.85f);
                _bgPanel.Height = (int) (_uiScreen.Height * 0.85f);
            };
            _uiScreen.AddComponent(_bgPanel);

            _lblTitle = new Label("Options", "CALIBRI", 48, ResourceCache);
            _lblTitle.LocalPosition = new Vector2i(10, 10);
            _bgPanel.AddComponent(_lblTitle);

            _lstResolution = new Listbox(250, 150, ResourceCache);
            _lstResolution.Alignment = Align.Bottom;
            _lstResolution.LocalPosition = new Vector2i(50, 50);
            _lstResolution.ItemSelected += _lstResolution_ItemSelected;
            PopulateAvailableVideoModes();
            _lblTitle.AddComponent(_lstResolution);

            _chkFullScreen = new Checkbox(ResourceCache);
            _chkFullScreen.ValueChanged += _chkFullScreen_ValueChanged;
            _chkFullScreen_ValueChanged(ConfigurationManager.GetCVar<bool>("display.fullscreen"), _chkFullScreen); // TODO: Dafuk is this?
            _chkFullScreen.Alignment = Align.Bottom;
            _chkFullScreen.LocalPosition = new Vector2i(0, 50);
            _lstResolution.AddComponent(_chkFullScreen);

            _lblFullScreen = new Label("Fullscreen", "CALIBRI", ResourceCache);
            _lblFullScreen.Alignment = Align.Right;
            _lblFullScreen.LocalPosition = new Vector2i(3, 0);
            _chkFullScreen.AddComponent(_lblFullScreen);

            _chkVSync = new Checkbox(ResourceCache);
            _chkVSync.ValueChanged += _chkVSync_ValueChanged;
            _chkVSync_ValueChanged(ConfigurationManager.GetCVar<bool>("display.vsync"), _chkVSync); // TODO: Dafuk is that?
            _chkVSync.Alignment = Align.Bottom;
            _chkVSync.LocalPosition = new Vector2i(0, 3);
            _chkFullScreen.AddComponent(_chkVSync);

            _lblVSync = new Label("Vsync", "CALIBRI", ResourceCache);
            _lblVSync.Alignment = Align.Right;
            _lblVSync.LocalPosition = new Vector2i(3, 0);
            _chkVSync.AddComponent(_lblVSync);
            
            _btnApply = new Button("Apply Settings", ResourceCache);
            _btnApply.Clicked += _btnApply_Clicked;
            _btnApply.Alignment = Align.Bottom | Align.Right;
            _btnApply.Resize += (sender, args) =>
            {
                _btnApply.LocalPosition = new Vector2i(-10 + -_btnApply.ClientArea.Width, -10 + -_btnApply.ClientArea.Height);
            };
            _bgPanel.AddComponent(_btnApply);

            _btnBack = new Button("Back", ResourceCache);
            _btnBack.Clicked += _btnBack_Clicked;
            _btnBack.Resize += (sender, args) =>
            {
                _btnBack.LocalPosition = new Vector2i(-10 + -_btnBack.ClientArea.Width, 0);
            };
            _btnApply.AddComponent(_btnBack);
        }
        
        private void PopulateAvailableVideoModes()
        {
            _lstResolution.ClearItems();
            _videoModeList.Clear();

            var modes = from v in VideoMode.FullscreenModes
                where v.Height > 748 && v.Width > 1024 //GOSH I HOPE NO ONES USING 16 BIT COLORS. OR RUNNING AT LESS THAN 59 hz
                orderby v.Height * v.Width
                select v;

            if (!modes.Any())
                throw new InvalidOperationException("No available video modes");

            foreach (var vm in modes)
            {
                if (!_videoModeList.ContainsKey(GetVmString(vm)))
                {
                    _videoModeList.Add(GetVmString(vm), vm);
                    _lstResolution.AddItem(GetVmString(vm));
                }
            }

            if (
                _videoModeList.Any(
                    x =>
                        x.Value.Width == CluwneLib.Window.Viewport.Size.X && x.Value.Height == CluwneLib.Window.Viewport.Size.Y))

            {
                var currentMode =
                    _videoModeList.FirstOrDefault(
                        x =>
                            x.Value.Width == CluwneLib.Window.Viewport.Size.X &&
                            x.Value.Height == CluwneLib.Window.Viewport.Size.Y);

                _lstResolution.SelectItem(currentMode.Key);
            }
            else
            {
                //No match due to different refresh rate in windowed mode. Just pick first resolution based on size only.
                var currentMode =
                    _videoModeList.FirstOrDefault(
                        x =>
                            x.Value.Width == CluwneLib.Window.Viewport.Size.X &&
                            x.Value.Height == CluwneLib.Window.Viewport.Size.Y);
                _lstResolution.SelectItem(currentMode.Key);
            }
        }

        /// <inheritdoc />
        public override void Startup()
        {
            InitializeGui();
            
            FormResize();
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            UserInterfaceManager.DisposeAllComponents();
        }

        /// <inheritdoc />
        public override void Update(FrameEventArgs e)
        {
            UserInterfaceManager.Update(e);
        }

        /// <inheritdoc />
        public override void Render(FrameEventArgs e)
        {
            UserInterfaceManager.Render(e);
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
        public override void KeyUp(KeyEventArgs e) { }

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
        public override void MouseWheelMove(MouseWheelEventArgs e)
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
            if (_videoModeList.ContainsKey(item.Text.Text))
            {
                var sel = _videoModeList[item.Text.Text];
                ConfigurationManager.SetCVar("display.width", (int) sel.Width);
                ConfigurationManager.SetCVar("display.height", (int) sel.Height);

                CluwneLib.UpdateVideoSettings();
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
