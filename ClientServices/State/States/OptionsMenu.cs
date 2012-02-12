using System;
using System.Collections.Generic;
using ClientInterfaces.State;
using ClientServices.Helpers;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using System.Drawing;
using ClientServices.UserInterface.Components;
using Lidgren.Network;
using System.Linq;

namespace ClientServices.State.States
{
    public class OptionsMenu : State, IState
    {
        #region Fields

        private readonly Sprite _background;
        private readonly Sprite _ticketbg;

        private readonly Label _mainmenubtt;
        private readonly Label _applybtt;

        private readonly Listbox _reslistbox;
        private readonly Checkbox _chkfullscreen;
        private readonly Label _lblfullscreen;

        private Dictionary<string, KeyValuePair<uint, uint>> vmList = new Dictionary<string, KeyValuePair<uint, uint>>();

        #endregion

        #region Properties
        #endregion

        public OptionsMenu(IDictionary<Type, object> managers)
            : base(managers)
        {
            _background = ResourceManager.GetSprite("mainbg");
            _background.Smoothing = Smoothing.Smooth;

            _chkfullscreen = new Checkbox(ResourceManager);
            _lblfullscreen = new Label("Fullscreen", "CALIBRI", ResourceManager);
            _chkfullscreen.ValueChanged += new Checkbox.CheckboxChangedHandler(_chkfullscreen_ValueChanged);

            _reslistbox = new Listbox(new Size(150, 250), 150, ResourceManager);
            _reslistbox.ItemSelected += new Listbox.ListboxPressHandler(_reslistbox_ItemSelected);

            var modes = from v in Gorgon.CurrentDriver.VideoModes
                        where v.Height * v.Width >= 786432 //Shitty way to limit it to 1024 * 748 upwards.
                        orderby v.Height * v.Width ascending
                        select v;

            foreach (VideoMode vm in modes)
            {
                if(!vmList.ContainsKey(GetVmString(vm)))
                {
                    vmList.Add(GetVmString(vm), new KeyValuePair<uint, uint>((uint)vm.Width, (uint)vm.Height));
                    _reslistbox.AddItem(GetVmString(vm));
                }
            }

            if (vmList.Any(x => x.Value.Key == Gorgon.Screen.Width && x.Value.Value == Gorgon.Screen.Height))
            {
                var curr = vmList.FirstOrDefault(x => x.Value.Key == Gorgon.Screen.Width && x.Value.Value == Gorgon.Screen.Height);
                _reslistbox.SelectItem(curr.Key, false);
            }

            _ticketbg = ResourceManager.GetSprite("ticketoverlay");

            _mainmenubtt = new Label("Main Menu", "CALIBRI", ResourceManager);
            _mainmenubtt.DrawBorder = true;
            _mainmenubtt.Clicked += new Label.LabelPressHandler(_mainmenubtt_Clicked);

            _applybtt = new Label("Apply", "CALIBRI", ResourceManager);
            _applybtt.DrawBorder = true;
            _applybtt.Clicked += new Label.LabelPressHandler(_applybtt_Clicked);
        }

        void _applybtt_Clicked(Label sender)
        {
            ApplyVideoMode();
        }

        void _chkfullscreen_ValueChanged(bool newValue)
        {
            ConfigurationManager.SetFullscreen(newValue);
        }

        private void ApplyVideoMode()
        {
            Gorgon.SetMode(Gorgon.Screen.OwnerForm, (int)ConfigurationManager.GetDisplayWidth(), (int)ConfigurationManager.GetDisplayHeight(), Gorgon.DesktopVideoMode.Format, ConfigurationManager.GetFullscreen(), false, false, Gorgon.DesktopVideoMode.RefreshRate);
        }

        void _reslistbox_ItemSelected(Label item)
        {
            if (vmList.ContainsKey(item.Text.Text))
            {
                var sel = vmList[item.Text.Text];
                ConfigurationManager.SetResolution((uint)sel.Key, (uint)sel.Value);
            }
        }

        private string GetVmString(VideoMode vm)
        {
            return vm.Width.ToString() + "x" + vm.Height.ToString();
        }

        void _exitbtt_Clicked(Label sender)
        {
            Environment.Exit(0);
        }

        void _mainmenubtt_Clicked(Label sender)
        {
            StateManager.RequestStateChange<ConnectMenu>();
        }

        void _connectbtt_Clicked(Label sender)
        {
        }

        void _connecttxt_OnSubmit(string text)
        {
        }

        #region Startup, Shutdown, Update
        public void Startup()
        {         
            NetworkManager.Disconnect();
            UserInterfaceManager.AddComponent(_mainmenubtt);
            UserInterfaceManager.AddComponent(_reslistbox);
            UserInterfaceManager.AddComponent(_chkfullscreen);
            UserInterfaceManager.AddComponent(_lblfullscreen);
            UserInterfaceManager.AddComponent(_applybtt);
        }


        public void Shutdown()
        {
            UserInterfaceManager.RemoveComponent(_mainmenubtt);
            UserInterfaceManager.RemoveComponent(_reslistbox);
            UserInterfaceManager.RemoveComponent(_chkfullscreen);
            UserInterfaceManager.RemoveComponent(_lblfullscreen);
            UserInterfaceManager.RemoveComponent(_applybtt);
        }

        public void Update(FrameEventArgs e)
        {
            //_connectbtt.Position = new Point(_connecttxt.Position.X, _connecttxt.Position.Y + _connecttxt.ClientArea.Height + 2);
            _reslistbox.Position = new Point(45, (int)(Gorgon.Screen.Height / 2.5f));
            _chkfullscreen.Position = new Point(_reslistbox.Position.X, _reslistbox.Position.Y + _reslistbox.ClientArea.Height + 5);
            _lblfullscreen.Position = new Point(_chkfullscreen.Position.X + _chkfullscreen.ClientArea.Width + 3, _chkfullscreen.Position.Y + (int)(_chkfullscreen.ClientArea.Height / 2f) - (int)(_lblfullscreen.ClientArea.Height / 2f));
            _mainmenubtt.Position = new Point(_reslistbox.Position.X + 650, _reslistbox.Position.Y);
            _applybtt.Position = new Point(_mainmenubtt.Position.X, _mainmenubtt.Position.Y + _mainmenubtt.ClientArea.Height + 5);

            _chkfullscreen.Value = ConfigurationManager.GetFullscreen();

            UserInterfaceManager.Update();
        }

        #endregion

        public void GorgonRender(FrameEventArgs e)
        {
            _background.Draw(new Rectangle(0, 0, Gorgon.Screen.Width, Gorgon.Screen.Height));
            _ticketbg.Draw(new Rectangle(0, (int)(Gorgon.Screen.Height / 2f - _ticketbg.Height / 2f), (int)_ticketbg.Width, (int)_ticketbg.Height));
            UserInterfaceManager.Render();
        }
        public void FormResize()
        {
        }

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
    }

}
