using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClientInterfaces.State;
using ClientServices.UserInterface.Components;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Label = ClientServices.UserInterface.Components.Label;

namespace ClientServices.State.States
{
    public class OptionsMenu : State, IState
    {
        #region Fields

        private readonly Label _applybtt;
        private readonly Sprite _background;

        private readonly Checkbox _chkfullscreen;
        private readonly Checkbox _chkvsync;

        private readonly Label _lblfullscreen;
        private readonly Label _lblvsync;
        private readonly Label _mainmenubtt;
        private readonly Listbox _reslistbox;
        private readonly Sprite _ticketbg;

        private readonly Dictionary<string, VideoMode> vmList = new Dictionary<string, VideoMode>();

        #endregion

        #region Properties

        #endregion

        public OptionsMenu(IDictionary<Type, object> managers)
            : base(managers)
        {
            _background = ResourceManager.GetSprite("mainbg");
            _background.Smoothing = Smoothing.Smooth;

            _lblfullscreen = new Label("Fullscreen", "CALIBRI", ResourceManager);

            _chkfullscreen = new Checkbox(ResourceManager);
            _chkfullscreen.Value = ConfigurationManager.GetFullscreen();
            _chkfullscreen.ValueChanged += _chkfullscreen_ValueChanged;

            _lblvsync = new Label("Vsync", "CALIBRI", ResourceManager);

            _chkvsync = new Checkbox(ResourceManager);
            _chkvsync.Value = ConfigurationManager.GetVsync();
            _chkvsync.ValueChanged += _chkvsync_ValueChanged;

            _reslistbox = new Listbox(250, 150, ResourceManager);
            _reslistbox.ItemSelected += _reslistbox_ItemSelected;

            IOrderedEnumerable<VideoMode> modes = from v in Gorgon.CurrentDriver.VideoModes
                                                  where
                                                      (v.Height > 748 && v.Width > 1024) &&
                                                      v.Format == BackBufferFormats.BufferRGB888 && v.RefreshRate >= 59
                                                  //GOSH I HOPE NOONES USING 16 BIT COLORS. OR RUNNING AT LESS THAN 59 hz
                                                  orderby v.Height*v.Width ascending
                                                  select v;

            if (!modes.Any())
                //No compatible videomodes at all. It is likely the game is being run on a calculator. TODO handle this.
                Application.Exit();

            foreach (VideoMode vm in modes)
            {
                if (!vmList.ContainsKey(GetVmString(vm)))
                {
                    vmList.Add(GetVmString(vm), vm);
                    _reslistbox.AddItem(GetVmString(vm));
                }
            }

            if (
                vmList.Any(
                    x =>
                    x.Value.Width == Gorgon.CurrentVideoMode.Width && x.Value.Height == Gorgon.CurrentVideoMode.Height &&
                    x.Value.RefreshRate ==
                    (Gorgon.Screen.Windowed ? Gorgon.DesktopVideoMode.RefreshRate : Gorgon.CurrentVideoMode.RefreshRate)))
            {
                KeyValuePair<string, VideoMode> curr =
                    vmList.FirstOrDefault(
                        x =>
                        x.Value.Width == Gorgon.CurrentVideoMode.Width &&
                        x.Value.Height == Gorgon.CurrentVideoMode.Height &&
                        x.Value.RefreshRate ==
                        (Gorgon.Screen.Windowed
                             ? Gorgon.DesktopVideoMode.RefreshRate
                             : Gorgon.CurrentVideoMode.RefreshRate));
                _reslistbox.SelectItem(curr.Key, false);
            }
            else
            {
                //No match due to different refresh rate in windowed mode. Just pick first resolution based on size only.
                KeyValuePair<string, VideoMode> curr =
                    vmList.FirstOrDefault(
                        x =>
                        x.Value.Width == Gorgon.CurrentVideoMode.Width &&
                        x.Value.Height == Gorgon.CurrentVideoMode.Height);
                _reslistbox.SelectItem(curr.Key, false);
            }

            _ticketbg = ResourceManager.GetSprite("ticketoverlay");

            _mainmenubtt = new Label("Main Menu", "CALIBRI", ResourceManager);
            _mainmenubtt.DrawBorder = true;
            _mainmenubtt.Clicked += _mainmenubtt_Clicked;

            _applybtt = new Label("Apply", "CALIBRI", ResourceManager);
            _applybtt.DrawBorder = true;
            _applybtt.Clicked += _applybtt_Clicked;
        }

        #region IState Members

        public void GorgonRender(FrameEventArgs e)
        {
            _background.Draw(new Rectangle(0, 0, Gorgon.CurrentClippingViewport.Width,
                                           Gorgon.CurrentClippingViewport.Height));
            _ticketbg.Draw(new Rectangle(0, (int) (Gorgon.CurrentClippingViewport.Height/2f - _ticketbg.Height/2f),
                                         (int) _ticketbg.Width, (int) _ticketbg.Height));
            UserInterfaceManager.Render();
        }

        public void FormResize()
        {
        }

        #endregion

        #region Input

        public void KeyDown(KeyboardInputEventArgs e)
        {
            UserInterfaceManager.KeyDown(e);
        }

        public void KeyUp(KeyboardInputEventArgs e)
        {
        }

        public void MouseUp(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }

        public void MouseDown(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }

        public void MouseMove(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseMove(e);
        }

        public void MouseWheelMove(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        }

        #endregion

        private void _chkvsync_ValueChanged(bool newValue, Checkbox sender)
        {
            ConfigurationManager.SetVsync(newValue);
        }

        private void _applybtt_Clicked(Label sender, MouseInputEventArgs e)
        {
            ApplyVideoMode();
        }

        private void _chkfullscreen_ValueChanged(bool newValue, Checkbox sender)
        {
            ConfigurationManager.SetFullscreen(newValue);
        }

        private void ApplyVideoMode()
        {
            Form owner = Gorgon.Screen.OwnerForm;
            Gorgon.Stop();

            Gorgon.SetMode(owner, (int) ConfigurationManager.GetDisplayWidth(),
                           (int) ConfigurationManager.GetDisplayHeight(), BackBufferFormats.BufferRGB888,
                           !ConfigurationManager.GetFullscreen(), false, false,
                           (int) ConfigurationManager.GetDisplayRefresh(),
                           (ConfigurationManager.GetVsync() ? VSyncIntervals.IntervalOne : VSyncIntervals.IntervalNone));

            if (!ConfigurationManager.GetFullscreen())
            {
                //Gee thanks gorgon for changing this stuff only when switching TO fullscreen.
                owner.FormBorderStyle = FormBorderStyle.Sizable;
                owner.WindowState = FormWindowState.Normal;
                owner.ControlBox = true;
                owner.MaximizeBox = true;
                owner.MinimizeBox = true;
            }

            Gorgon.Go();
        }

        private void _reslistbox_ItemSelected(Label item, Listbox sender)
        {
            if (vmList.ContainsKey(item.Text.Text))
            {
                VideoMode sel = vmList[item.Text.Text];
                ConfigurationManager.SetResolution((uint) sel.Width, (uint) sel.Height);
                ConfigurationManager.SetDisplayRefresh((uint) sel.RefreshRate);
            }
        }

        private string GetVmString(VideoMode vm)
        {
            return vm.Width.ToString() + "x" + vm.Height.ToString() + " @ " + vm.RefreshRate + " hz";
        }

        private void _exitbtt_Clicked(Label sender)
        {
            Environment.Exit(0);
        }

        private void _mainmenubtt_Clicked(Label sender, MouseInputEventArgs e)
        {
            StateManager.RequestStateChange<MainScreen>();
        }

        private void _connectbtt_Clicked(Label sender, MouseInputEventArgs e)
        {
        }

        private void _connecttxt_OnSubmit(string text)
        {
        }

        #region Startup, Shutdown, Update

        public void Startup()
        {
            NetworkManager.Disconnect();
            UserInterfaceManager.AddComponent(_mainmenubtt);
            UserInterfaceManager.AddComponent(_reslistbox);
            UserInterfaceManager.AddComponent(_chkfullscreen);
            UserInterfaceManager.AddComponent(_chkvsync);
            UserInterfaceManager.AddComponent(_lblfullscreen);
            UserInterfaceManager.AddComponent(_lblvsync);
            UserInterfaceManager.AddComponent(_applybtt);
        }


        public void Shutdown()
        {
            UserInterfaceManager.RemoveComponent(_mainmenubtt);
            UserInterfaceManager.RemoveComponent(_reslistbox);
            UserInterfaceManager.RemoveComponent(_chkfullscreen);
            UserInterfaceManager.RemoveComponent(_chkvsync);
            UserInterfaceManager.RemoveComponent(_lblfullscreen);
            UserInterfaceManager.RemoveComponent(_lblvsync);
            UserInterfaceManager.RemoveComponent(_applybtt);
        }

        public void Update(FrameEventArgs e)
        {
            //_connectbtt.Position = new Point(_connecttxt.Position.X, _connecttxt.Position.Y + _connecttxt.ClientArea.Height + 2);
            _reslistbox.Position = new Point(45, (int) (Gorgon.CurrentClippingViewport.Height/2.5f));
            _chkfullscreen.Position = new Point(_reslistbox.Position.X,
                                                _reslistbox.Position.Y + _reslistbox.ClientArea.Height + 10);
            _chkvsync.Position = new Point(_chkfullscreen.Position.X,
                                           _chkfullscreen.Position.Y + _chkfullscreen.ClientArea.Height + 10);
            _lblfullscreen.Position = new Point(_chkfullscreen.Position.X + _chkfullscreen.ClientArea.Width + 3,
                                                _chkfullscreen.Position.Y + (int) (_chkfullscreen.ClientArea.Height/2f) -
                                                (int) (_lblfullscreen.ClientArea.Height/2f));
            _lblvsync.Position = new Point(_chkvsync.Position.X + _chkvsync.ClientArea.Width + 3,
                                           _chkvsync.Position.Y + (int) (_chkvsync.ClientArea.Height/2f) -
                                           (int) (_chkvsync.ClientArea.Height/2f));
            _mainmenubtt.Position = new Point(_reslistbox.Position.X + 650, _reslistbox.Position.Y);
            _applybtt.Position = new Point(_mainmenubtt.Position.X,
                                           _mainmenubtt.Position.Y + _mainmenubtt.ClientArea.Height + 5);

            _chkfullscreen.Value = ConfigurationManager.GetFullscreen();

            UserInterfaceManager.Update(e.FrameDeltaTime);
        }

        #endregion
    }
}