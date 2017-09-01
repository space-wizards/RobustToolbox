using SFML.Graphics;
using SFML.System;
using SFML.Window;
using OpenTK;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.State;
using SS14.Client.UserInterface.Components;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using KeyEventArgs = SFML.Window.KeyEventArgs;
using Label = SS14.Client.UserInterface.Components.Label;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.State.States
{
    public class OptionsMenu : State, IState
    {
        #region Fields

        private Sprite _background;
        private Sprite _ticketBg;

        private Checkbox _chkFullscreen;
        private Checkbox _chkVsync;

        private Label _lblTitle;
        private Label _lblFullscreen;
        private Label _lblVsync;
        private Button _btnBack;
        private Button _btnApply;

        private Listbox _lstResolution;

        private Dictionary<string, VideoMode> vmList = new Dictionary<string, VideoMode>();
        private int _prevScreenHeight;
        private int _prevScreenWidth;

        private Box2i _boundingArea = new Box2i();

        #endregion

        #region Properties

        #endregion

        public OptionsMenu(IDictionary<Type, object> managers)
            : base(managers)
        {
            UpdateBounds();
        }

        private void UpdateBounds()
        {
            var top = (int)(CluwneLib.Window.Viewport.Size.Y / 2f) - (int)(_boundingArea.Height / 2f);
            _boundingArea = Box2i.FromDimensions(0, top, 1000, 600);
        }

        private void InitalizeGUI()
        {
            _background = ResourceCache.GetSprite("mainbg");
            _ticketBg = ResourceCache.GetSprite("ticketoverlay");

            _lblTitle = new Label("Options", "CALIBRI", 48, ResourceCache);
            UserInterfaceManager.AddComponent(_lblTitle);

            _lblFullscreen = new Label("Fullscreen", "CALIBRI", ResourceCache);
            UserInterfaceManager.AddComponent(_lblFullscreen);

            _chkFullscreen = new Checkbox(ResourceCache);
            _chkFullscreen.ValueChanged += _chkfullscreen_ValueChanged;
            _chkfullscreen_ValueChanged(ConfigurationManager.GetCVar<bool>("display.fullscreen"), _chkFullscreen);
            UserInterfaceManager.AddComponent(_chkFullscreen);

            _lblVsync = new Label("Vsync", "CALIBRI", ResourceCache);
            UserInterfaceManager.AddComponent(_lblVsync);

            _chkVsync = new Checkbox(ResourceCache);
            _chkVsync.ValueChanged += _chkvsync_ValueChanged;
            _chkvsync_ValueChanged(ConfigurationManager.GetCVar<bool>("display.vsync"), _chkVsync);
            UserInterfaceManager.AddComponent(_chkVsync);

            _lstResolution = new Listbox(250, 150, ResourceCache);
            _lstResolution.ItemSelected += _reslistbox_ItemSelected;
            PopulateAvailableVideoModes();
            UserInterfaceManager.AddComponent(_lstResolution);

            _btnBack = new Button("Back", ResourceCache);
            _btnBack.Clicked += _backBtn_Clicked;
            UserInterfaceManager.AddComponent(_btnBack);

            _btnApply = new Button("Apply Settings", ResourceCache);
            _btnApply.Clicked += _applybtt_Clicked;
            UserInterfaceManager.AddComponent(_btnApply);

            UpdateGUIPosition();
        }

        private void UpdateGUIPosition()
        {
            const int SECTION_PADDING = 50;
            const int OPTION_PADDING = 10;
            const int LABEL_PADDING = 3;


            _lblTitle.Position = new Vector2i(_boundingArea.Left + 10, _boundingArea.Top + 10);
            _lblTitle.Update(0);

            _lstResolution.Position = new Vector2i(_boundingArea.Left + SECTION_PADDING,
                _lblTitle.Position.Y + _lblTitle.ClientArea.Height + SECTION_PADDING);
            _lstResolution.Update(0);

            _chkFullscreen.Position = new Vector2i(_lstResolution.Position.X,
                _lstResolution.Position.Y + _lstResolution.ClientArea.Height + SECTION_PADDING);
            _chkFullscreen.Update(0);
            _lblFullscreen.Position = new Vector2i(_chkFullscreen.Position.X + _chkFullscreen.ClientArea.Width + LABEL_PADDING,
                _chkFullscreen.Position.Y);
            _lblFullscreen.Update(0);

            _chkVsync.Position = new Vector2i(_lblFullscreen.Position.X,
                _lblFullscreen.Position.Y + _lblFullscreen.ClientArea.Height + OPTION_PADDING);
            _chkVsync.Update(0);
            _lblVsync.Position = new Vector2i(_chkVsync.Position.X + _chkVsync.ClientArea.Width + LABEL_PADDING,
                _chkVsync.Position.Y);
            _lblVsync.Update(0);

            _btnApply.Position = new Vector2i((_boundingArea.Left + _boundingArea.Width) - (_btnApply.ClientArea.Width + SECTION_PADDING),
                                                (_boundingArea.Top + _boundingArea.Height) - (_btnApply.ClientArea.Height + SECTION_PADDING));
            _btnApply.Update(0);
            _btnBack.Position = new Vector2i(_btnApply.Position.X - (_btnBack.ClientArea.Width + OPTION_PADDING), _btnApply.Position.Y);
            _btnBack.Update(0);
        }

        private void PopulateAvailableVideoModes()
        {
            _lstResolution.ClearItems();
            vmList.Clear();
            IOrderedEnumerable<VideoMode> modes = from v in SFML.Window.VideoMode.FullscreenModes
                                                  where (v.Height > 748 && v.Width > 1024) //GOSH I HOPE NOONES USING 16 BIT COLORS. OR RUNNING AT LESS THAN 59 hz
                                                  orderby v.Height * v.Width ascending
                                                  select v;

            if (!modes.Any())
                throw new Exception("No available video modes");

            foreach (VideoMode vm in modes)
            {
                if (!vmList.ContainsKey(GetVmString(vm)))
                {
                    vmList.Add(GetVmString(vm), vm);
                    _lstResolution.AddItem(GetVmString(vm));
                }
            }

            if (
                 vmList.Any(
                    x =>
                    x.Value.Width == CluwneLib.Window.Viewport.Size.X && x.Value.Height == CluwneLib.Window.Viewport.Size.Y))

            {
                KeyValuePair<string, VideoMode> curr =
                    vmList.FirstOrDefault(
                        x =>
                        x.Value.Width == CluwneLib.Window.Viewport.Size.X &&
                        x.Value.Height == CluwneLib.Window.Viewport.Size.Y);

                _lstResolution.SelectItem(curr.Key, false);
            }
            else
            {
                //No match due to different refresh rate in windowed mode. Just pick first resolution based on size only.
                KeyValuePair<string, VideoMode> curr =
                    vmList.FirstOrDefault(
                        x =>
                        x.Value.Width == CluwneLib.Window.Viewport.Size.X &&
                        x.Value.Height == CluwneLib.Window.Viewport.Size.Y);
                _lstResolution.SelectItem(curr.Key, false);
            }
        }

        #region Startup, Shutdown, Update

        public void Startup()
        {
            NetworkManager.ClientDisconnect("Client killed old session."); //TODO: Is this really needed here?
            InitalizeGUI();
        }


        public void Shutdown()
        {
            UserInterfaceManager.DisposeAllComponents();
        }

        public void Update(FrameEventArgs e)
        {
            if (CluwneLib.Window.Viewport.Size.X != _prevScreenWidth || CluwneLib.Window.Viewport.Size.Y != _prevScreenHeight)
            {
                _prevScreenHeight = (int)CluwneLib.Window.Viewport.Size.Y;
                _prevScreenWidth = (int)CluwneLib.Window.Viewport.Size.X;
                UpdateBounds();
                UpdateGUIPosition();
            }

            _chkFullscreen.Value = ConfigurationManager.GetCVar<bool>("display.fullscreen");
            UserInterfaceManager.Update(e);
        }

        #endregion

        #region IState Members

        public void Render(FrameEventArgs e)
        {
            _background.SetTransformToRect(Box2i.FromDimensions(0, 0, (int)CluwneLib.Window.Viewport.Size.X, (int) CluwneLib.Window.Viewport.Size.Y));
            _background.Draw();

            _ticketBg.SetTransformToRect(_boundingArea);
            _ticketBg.Draw();
            UserInterfaceManager.Render(e);
        }

        public void FormResize()
        {
        }

        #endregion

        #region Input

        public void KeyDown(KeyEventArgs e)
        {
            UserInterfaceManager.KeyDown(e);
        }

        public void KeyUp(KeyEventArgs e)
        {
        }

        public void MouseUp(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }

        public void MouseDown(MouseButtonEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }

        public void MouseMoved( MouseMoveEventArgs e )
        {

        }
        public void MousePressed( MouseButtonEventArgs e )
        {
            UserInterfaceManager.MouseDown(e);
        }
        public void MouseMove(MouseMoveEventArgs e)
        {
            UserInterfaceManager.MouseMove(e);
        }

        public void MouseWheelMove(MouseWheelEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        }

        public void MouseEntered(EventArgs e)
        {
            UserInterfaceManager.MouseEntered(e);
        }
        public void MouseLeft(EventArgs e)
        {
            UserInterfaceManager.MouseLeft(e);
        }

        public void TextEntered(TextEventArgs e)
        {
            UserInterfaceManager.TextEntered(e);
        }
        #endregion

        private void _chkvsync_ValueChanged(bool newValue, Checkbox sender)
        {
            ConfigurationManager.SetCVar("display.vsync", newValue);
        }



        private void _chkfullscreen_ValueChanged(bool newValue, Checkbox sender)
        {
            ConfigurationManager.SetCVar("display.fullscreen", newValue);
        }

        private void ApplyVideoMode()
        {
          CluwneLib.UpdateVideoSettings();
        }

        private void _reslistbox_ItemSelected(Label item, Listbox sender)
        {
            if (vmList.ContainsKey(item.Text.Text))
            {
                VideoMode sel = vmList[item.Text.Text];
                ConfigurationManager.SetCVar("display.width", (int)sel.Width);
                ConfigurationManager.SetCVar("display.height", (int)sel.Height);
            }
        }

        private string GetVmString(VideoMode vm)
        {
            return string.Format(" - {0} x {1} @ {2}hz", vm.Width.ToString(), vm.Height.ToString(), vm.BitsPerPixel);
        }

        private void _applybtt_Clicked(Button sender)
        {
            ApplyVideoMode();
        }

        private void _backBtn_Clicked(Button sender)
        {
            StateManager.RequestStateChange<MainScreen>();
        }
    }
}
