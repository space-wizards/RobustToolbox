using System.Drawing;
using ClientInterfaces.Resource;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    internal class JobTab : TabContainer
    {
        private readonly Showcase _shwJobs;

        public JobTab(string uniqueName, Size size, IResourceManager resourceManager)
            : base(uniqueName, size, resourceManager)
        {
            _shwJobs = new Showcase
                           {
                               Position = new Point(60, 5),
                               Size = new Size(675, 80),
                               ButtonLeft = "job_arrowleft",
                               ButtonRight = "job_arrowright",
                               SelectionBackground = "job_glow"
                           };

            var job_robo = new ImageButton
                               {
                                   ImageNormal = "job_roboticist"
                               };

            var job_gene = new ImageButton
                               {
                                   ImageNormal = "job_geneticist"
                               };

            var job_medic = new ImageButton
                                {
                                    ImageNormal = "job_medic"
                                };

            _shwJobs.AddItem(job_gene, "1");
            _shwJobs.AddItem(job_medic, "2");
            _shwJobs.AddItem(job_robo, "3");

            components.Add(_shwJobs);
        }

        public override void Update(float frameTime)
        {
            ClientArea = new Rectangle(Position, Size);

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