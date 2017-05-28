using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Event;
using SS14.Client.Interfaces.State;
using SS14.Client.UserInterface.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using KeyEventArgs = SFML.Window.KeyEventArgs;
using Label = SS14.Client.UserInterface.Components.Label;

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

        private IntRect _boundingArea = new IntRect();

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
            _boundingArea.Height = 600;
            _boundingArea.Width = 1000;
            _boundingArea.Left = 0;
            _boundingArea.Top = (int)(CluwneLib.Screen.Size.Y / 2f) - (int)(_boundingArea.Height / 2f);
        }

        private void InitalizeGUI()
        {
            _background = ResourceManager.GetSprite("mainbg");
            _ticketBg = ResourceManager.GetSprite("ticketoverlay");

            _lblTitle = new Label("Options", "CALIBRI", 48, ResourceManager);
            UserInterfaceManager.AddComponent(_lblTitle);

            _lblFullscreen = new Label("Fullscreen", "CALIBRI", ResourceManager);
            UserInterfaceManager.AddComponent(_lblFullscreen);

            _chkFullscreen = new Checkbox(ResourceManager);
            _chkFullscreen.ValueChanged += _chkfullscreen_ValueChanged;
            _chkfullscreen_ValueChanged(ConfigurationManager.GetFullscreen(), _chkFullscreen);
            UserInterfaceManager.AddComponent(_chkFullscreen);

            _lblVsync = new Label("Vsync", "CALIBRI", ResourceManager);
            UserInterfaceManager.AddComponent(_lblVsync);

            _chkVsync = new Checkbox(ResourceManager);
            _chkVsync.ValueChanged += _chkvsync_ValueChanged;
            _chkvsync_ValueChanged(ConfigurationManager.GetVsync(), _chkVsync);
            UserInterfaceManager.AddComponent(_chkVsync);

            _lstResolution = new Listbox(250, 150, ResourceManager);
            _lstResolution.ItemSelected += _reslistbox_ItemSelected;
            PopulateAvailableVideoModes();
            UserInterfaceManager.AddComponent(_lstResolution);

            _btnBack = new Button("Back", ResourceManager);
            _btnBack.Clicked += _backBtn_Clicked;
            UserInterfaceManager.AddComponent(_btnBack);

            _btnApply = new Button("Apply Settings", ResourceManager);
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
                new Exception("No available video modes");

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
                    x.Value.Width == CluwneLib.Screen.Size.X && x.Value.Height == CluwneLib.Screen.Size.Y))

            {
                KeyValuePair<string, VideoMode> curr =
                    vmList.FirstOrDefault(
                        x =>
                        x.Value.Width == CluwneLib.Screen.Size.X &&
                        x.Value.Height == CluwneLib.Screen.Size.Y);

                _lstResolution.SelectItem(curr.Key, false);
            }
            else
            {
                //No match due to different refresh rate in windowed mode. Just pick first resolution based on size only.
                KeyValuePair<string, VideoMode> curr =
                    vmList.FirstOrDefault(
                        x =>
                        x.Value.Width == CluwneLib.Screen.Size.X &&
                        x.Value.Height == CluwneLib.Screen.Size.Y);
                _lstResolution.SelectItem(curr.Key, false);
            }
        }

        #region Startup, Shutdown, Update

        public void Startup()
        {
            NetworkManager.Disconnect(); //TODO: Is this really needed here?
            InitalizeGUI();
        }


        public void Shutdown()
        {
            UserInterfaceManager.DisposeAllComponents();
        }

        public void Update(FrameEventArgs e)
        {
            if (CluwneLib.Screen.Size.X != _prevScreenWidth || CluwneLib.Screen.Size.Y != _prevScreenHeight)
            {
                _prevScreenHeight = (int)CluwneLib.Screen.Size.Y;
                _prevScreenWidth = (int)CluwneLib.Screen.Size.X;
                UpdateBounds();
                UpdateGUIPosition();
            }

            _chkFullscreen.Value = ConfigurationManager.GetFullscreen();
            UserInterfaceManager.Update(e);
        }

        #endregion

        #region IState Members

        public void Render(FrameEventArgs e)
        {
            _background.SetTransformToRect(new IntRect(0, 0, (int)CluwneLib.Screen.Size.X, (int) CluwneLib.Screen.Size.Y));
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
            ConfigurationManager.SetVsync(newValue);
        }



        private void _chkfullscreen_ValueChanged(bool newValue, Checkbox sender)
        {
            ConfigurationManager.SetFullscreen(newValue);
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
                ConfigurationManager.SetResolution((uint) sel.Width, (uint) sel.Height);
               
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