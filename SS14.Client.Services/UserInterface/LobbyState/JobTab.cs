using Lidgren.Network;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using SFML.Window;
using SS14.Shared;
using SS14.Shared.IoC;
using System.Drawing;
using SS14.Client.Graphics;
using BlendMode = SFML.Graphics.BlendMode;
using Color = SFML.Graphics.Color;
using SS14.Client.Services.Helpers;
using SS14.Client.GameObjects;
using SFML.System;
using SS14.Shared.Maths;
using SFML.Graphics;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class JobTab : TabContainer
    {
        public readonly LobbyShowcase _shwJobs;
        public readonly LobbyShowcase _shwDepa;
        private readonly SimpleImage _imgWhatDep;
        public readonly Label _lblDep;
        private SimpleImage _imgJobDesc;
        public Label _lbljobDesc;
        public Label _lbljobName;
        private SimpleImage _imgJobFluff;
        private SimpleImage _imgDepGrad;
        public SimpleImage _imgJobGrad;


        public JobTab(string uniqueName, Vector2i size, IResourceManager resourceManager)
            : base(uniqueName, size, resourceManager)
        {
            _imgWhatDep = new SimpleImage()
                {
                    Sprite = "lobby_whatdep"
                };

            _imgWhatDep.Update(0);
            _imgWhatDep.Position = new Vector2i((int)(size.X / 2f - _imgWhatDep.ClientArea.Width / 2f),  30);
            _imgWhatDep.Update(0);

            _imgJobDesc = new SimpleImage()
            {
                Sprite = "lobby_descbg"
            };

            _shwDepa = new LobbyShowcase
            {
                Position = new Vector2i(60, _imgWhatDep.ClientArea.Bottom() + 5),
                Size = new Vector2i(675, 80),
                ButtonLeft = "job_arrowleft",
                ButtonRight = "job_arrowright",
                SelectionBackground = "dep_glow",
                ItemSpacing = 20,
                //ItemOffsets = new Vector2i(40, 0)
            };
            _shwDepa.Update(0);

            _imgDepGrad = new SimpleImage()
            {
                Sprite = "lobby_depgrad",
                Color = Color.White.WithAlpha(120)
            };
            _imgDepGrad.Update(0);
            _imgDepGrad.Position = new Vector2i(_shwDepa.ClientArea.Left + (int)(_shwDepa.ClientArea.Width / 2f - _imgDepGrad.ClientArea.Width / 2f), _shwDepa.ClientArea.Top);

            _lblDep = new Label("DEPARTMENT", "MICROGBE", resourceManager)
            {
                BackgroundColor = new Color(245, 245, 245),
                DrawBackground = true,
                TextColor = new Color(53,57,66)
            };

            _lblDep.Update(0);
            _lblDep.Position = new Vector2i((int)(size.X / 2f - _lblDep.ClientArea.Width / 2f), _shwDepa.ClientArea.Bottom() + 5);
            _lblDep.Update(0);

            _imgJobFluff = new SimpleImage()
            {
                Sprite = "lobby_jobfluff"
            };
            _imgJobFluff.Position = new Vector2i(_lblDep.ClientArea.Left + (int)(_lblDep.ClientArea.Width / 2f - _imgJobFluff.ClientArea.Width / 2f), _lblDep.ClientArea.Bottom());

            _shwJobs = new LobbyShowcase
            {
                Position = new Vector2i(60, _lblDep.ClientArea.Bottom() + 25),
                Size = new Vector2i(675, 80),
                ButtonLeft = "job_arrowleft",
                ButtonRight = "job_arrowright",
                SelectionBackground = "job_glow"
            };
            _shwJobs.Update(0);

            _imgJobGrad = new SimpleImage()
            {
                Sprite = "lobby_jobgrad",
                Color = Color.White.WithAlpha(120)
            };
            _imgJobGrad.Update(0);
            _imgJobGrad.Position = new Vector2i(_shwJobs.ClientArea.Left + (int)(_shwJobs.ClientArea.Width / 2f - _imgJobGrad.ClientArea.Width / 2f), _shwJobs.ClientArea.Top);

            _imgJobDesc.Update(0);
            _imgJobDesc.Position = new Vector2i(0, _shwJobs.ClientArea.Bottom() - 12);
            _imgJobDesc.Update(0);

            _lbljobName = new Label(" ", "MICROGBE", resourceManager)
            {
                TextColor = new Color(245, 245, 245)
            };
            _lbljobName.Position = new Vector2i(3, _imgJobDesc.Position.Y + 4);

            _lbljobName.Update(0);

            _lbljobDesc = new Label(" ", "MICROGME", resourceManager)
            {
                TextColor = new Color(245, 245, 245)
            };
            _lbljobDesc.Position = new Vector2i(3, _lbljobName.ClientArea.Bottom() + 5);

            components.Add(_lblDep);
            components.Add(_imgWhatDep);
            components.Add(_imgDepGrad);
            components.Add(_shwDepa);
            components.Add(_imgJobGrad);
            components.Add(_shwJobs);
            components.Add(_imgJobFluff);
            components.Add(_imgJobDesc);
            components.Add(_lbljobDesc);
            components.Add(_lbljobName);
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
            ClientArea = new IntRect(Position, Size);

            if (_lblDep != null)
            {
                _lblDep.Position = new Vector2i((int) (Size.X/2f - _lblDep.ClientArea.Width/2f), _shwDepa.ClientArea.Bottom() + 10);
                if(_imgJobFluff != null)
                    _imgJobFluff.Position = new Vector2i(_lblDep.ClientArea.Left + (int) (_lblDep.ClientArea.Width/2f - _imgJobFluff.ClientArea.Width/2f), _lblDep.ClientArea.Bottom());
            }

            //if(_shwJobs != null)
            //    _shwJobs.Position = new Vector2i(Position.X + (int)(ClientArea.Width / 2f - _shwJobs.ClientArea.Width / 2f), 5);

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

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            return base.MouseDown(e);
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return base.MouseUp(e);
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            base.MouseMove(e);
        }
    }
}