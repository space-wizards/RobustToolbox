using System;
using System.Collections.Generic;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class JobTab : TabContainer
    {
        private SimpleImage image;
        public JobTab(string uniqueName, Size size, IResourceManager resourceManager) 
            : base(uniqueName, size, resourceManager)
        {
            image = new SimpleImage() { Sprite = "floating_dude", Position = new Point(50,50)};
            components.Add(image);
        }

        public override void Update(float frameTime)
        {
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
