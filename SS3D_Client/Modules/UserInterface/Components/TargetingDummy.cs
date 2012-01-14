using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.UserInterface;
using Lidgren.Network;
using SS3D_shared;
using ClientResourceManager;

namespace SS3D.UserInterface
{
    class TargetingDummy : GuiComponent
    {
        List<TargetingDummyElement> elements = new List<TargetingDummyElement>();

        public TargetingDummy()
            : base()
        {
            TargetingDummyElement head = new TargetingDummyElement("dummy_head", BodyPart.head);
            TargetingDummyElement torso = new TargetingDummyElement("dummy_torso", BodyPart.torso);
            TargetingDummyElement groin = new TargetingDummyElement("dummy_groin", BodyPart.groin);
            TargetingDummyElement arm_l = new TargetingDummyElement("dummy_arm_l", BodyPart.arm_l);
            TargetingDummyElement arm_r = new TargetingDummyElement("dummy_arm_r", BodyPart.arm_r);
            TargetingDummyElement leg_l = new TargetingDummyElement("dummy_leg_l", BodyPart.leg_l);
            TargetingDummyElement leg_r = new TargetingDummyElement("dummy_leg_r", BodyPart.leg_r);
            elements.Add(head);
            elements.Add(torso);
            elements.Add(groin);
            elements.Add(arm_l);
            elements.Add(arm_r);
            elements.Add(leg_l);
            elements.Add(leg_r);
            head.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            torso.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            groin.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            arm_l.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            arm_r.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            leg_l.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            leg_r.Clicked += new TargetingDummyElement.TargetingDummyElementPressHandler(Selected);
            Update();
        }

        void Selected(TargetingDummyElement sender)
        {
        }

        public override void Update()
        {
            clientArea = new Rectangle(Position, new Size((int)elements[0].ClientArea.Width, (int)elements[0].ClientArea.Height));
            foreach (TargetingDummyElement current in elements)
            {
                current.Position = this.Position;
                current.Update();
            }
        }

        public override void Render()
        {
            foreach (TargetingDummyElement current in elements)
                current.Render();
        }

        public override void Dispose()
        {
            foreach (TargetingDummyElement current in elements)
                current.Dispose();

            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (!clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y))) return false;

            foreach (TargetingDummyElement toClear in elements) toClear.ClearSelected();

            foreach (TargetingDummyElement current in elements.ToArray()) //To array because list order changes in loop.
            {
                if (current.MouseDown(e))
                {
                    elements = (from a in elements
                                orderby (a == current) ascending
                                select a).ToList();
                    return true;
                }
            }

            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}
