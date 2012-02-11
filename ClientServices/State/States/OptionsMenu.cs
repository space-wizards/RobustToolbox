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
        private readonly Listbox _reslistbox;

        private Dictionary<string, VideoMode> vmList = new Dictionary<string, VideoMode>();

        #endregion

        #region Properties
        #endregion

        public OptionsMenu(IDictionary<Type, object> managers)
            : base(managers)
        {
            _background = ResourceManager.GetSprite("mainbg");
            _background.Smoothing = Smoothing.Smooth;

            _reslistbox = new Listbox(new Size(150, 250), 150, ResourceManager);
            _reslistbox.ItemSelected += new Listbox.ListboxPressHandler(_reslistbox_ItemSelected);

            var modes = from v in Gorgon.CurrentDriver.VideoModes
                        where v.Bpp == 32
                        where v.Height * v.Width >= 786432 //Shitty way to limit it to 1024 * 748 upwards.
                        select v;

            foreach (VideoMode vm in modes)
            {
                vmList.Add(GetVmString(vm), vm);
                _reslistbox.AddItem(GetVmString(vm));
            }

            if (vmList.Any(x => x.Value == Gorgon.CurrentVideoMode)) //Window border reduces actual size of gorgon render window. So this doesnt work.
            {
                var curr = vmList.FirstOrDefault(x => x.Value == Gorgon.CurrentVideoMode);
                _reslistbox.SelectItem(curr.Key, false);
            }

            _ticketbg = ResourceManager.GetSprite("ticketoverlay");

            _mainmenubtt = new Label("Main Menu", "CALIBRI", ResourceManager);
            _mainmenubtt.DrawBorder = true;
            _mainmenubtt.Clicked += new Label.LabelPressHandler(_mainmenubtt_Clicked);
        }

        void _reslistbox_ItemSelected(Label item)
        {
            if (vmList.ContainsKey(item.Text.Text))
            {
                var sel = vmList[item.Text.Text];
                ConfigurationManager.SetResolution((uint)sel.Width, (uint)sel.Height);
            }
        }

        private string GetVmString(VideoMode vm)
        {
            return vm.Width.ToString() + "x" + vm.Height.ToString() + " @ " + vm.RefreshRate.ToString() + " Hz";
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
        }


        public void Shutdown()
        {
            UserInterfaceManager.RemoveComponent(_mainmenubtt);
            UserInterfaceManager.RemoveComponent(_reslistbox);
        }

        public void Update(FrameEventArgs e)
        {
            //_connectbtt.Position = new Point(_connecttxt.Position.X, _connecttxt.Position.Y + _connecttxt.ClientArea.Height + 2);
            _reslistbox.Position = new Point(45, (int)(Gorgon.Screen.Height / 2.5f));
            _mainmenubtt.Position = new Point(_reslistbox.Position.X + 650, _reslistbox.Position.Y);

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
