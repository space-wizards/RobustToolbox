using System.Drawing;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientServices.Network;
using ClientServices.Player;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    internal class JobTab : TabContainer
    {
        public readonly Showcase _shwJobs;
        public readonly Showcase _shwDepa;
        private readonly SimpleImage _imgWhatDep;
        public readonly Label _lblDep;
        private ImageButton _bttReady;

        public JobTab(string uniqueName, Size size, IResourceManager resourceManager)
            : base(uniqueName, size, resourceManager)
        {
            _bttReady = new ImageButton()
                {
                    ImageNormal = "lobby_ready"
                };
            _bttReady.Clicked += new ImageButton.ImageButtonPressHandler(_bttReady_Clicked);
            _bttReady.Update(0);

            _imgWhatDep = new SimpleImage()
                {
                    Sprite = "lobby_whatdep"
                };

            _imgWhatDep.Update(0);
            _imgWhatDep.Position = new Point((int)(size.Width / 2f - _imgWhatDep.ClientArea.Width / 2f),  30);
            _imgWhatDep.Update(0);

            _shwDepa = new Showcase
            {
                Position = new Point(60, _imgWhatDep.ClientArea.Bottom + 5),
                Size = new Size(675, 80),
                ButtonLeft = "job_arrowleft",
                ButtonRight = "job_arrowright",
                SelectionBackground = "job_glow",
                ItemSpacing = 20
            };

            _shwDepa.Update(0);

            _lblDep = new Label("DEPARTMENT", "MICROGBE", resourceManager)
            {
                BackgroundColor = Color.WhiteSmoke,
                DrawBackground = true,
                TextColor = Color.FromArgb(53,57,66)
            };

            _lblDep.Update(0);
            _lblDep.Position = new Point((int)(size.Width / 2f - _lblDep.ClientArea.Width / 2f), _shwDepa.ClientArea.Bottom + 5);
            _lblDep.Update(0);

            _shwJobs = new Showcase
            {
                Position = new Point(60, _lblDep.ClientArea.Bottom + 15),
                Size = new Size(675, 80),
                ButtonLeft = "job_arrowleft",
                ButtonRight = "job_arrowright",
                SelectionBackground = "job_glow"
            };
            _shwJobs.Update(0);

            _bttReady.Position = new Point(ClientArea.Width - _bttReady.ClientArea.Width - 5, _shwJobs.ClientArea.Bottom + 50);

            components.Add(_bttReady);
            components.Add(_lblDep);
            components.Add(_imgWhatDep);
            components.Add(_shwDepa);
            components.Add(_shwJobs);
        }

        void _bttReady_Clicked(ImageButton sender)
        {
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            playerManager.SendVerb("joingame", 0);
        }

        public override void Activated() //TODO: Maybe i shouldnt request this everytime the tab is selected?. Automatic updates?
        {
            var netManager = IoCManager.Resolve<INetworkManager>();
            NetOutgoingMessage jobListMsg = netManager.CreateMessage();
            jobListMsg.Write((byte)NetMessage.JobList); //This requests the job list.
            netManager.SendMessage(jobListMsg, NetDeliveryMethod.ReliableOrdered);
        }

        public override void Update(float frameTime)
        {
            ClientArea = new Rectangle(Position, Size);

            if(_lblDep != null)
                _lblDep.Position = new Point((int)(Size.Width / 2f - _lblDep.ClientArea.Width / 2f), _shwDepa.ClientArea.Bottom + 10);

            //if(_shwJobs != null)
            //    _shwJobs.Position = new Point(Position.X + (int)(ClientArea.Width / 2f - _shwJobs.ClientArea.Width / 2f), 5);

            base.Update(frameTime);
        }

        public override void Render()
        {
            base.Render();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return base.MouseDown(e);
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return base.MouseUp(e);
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            base.MouseMove(e);
        }
    }
}