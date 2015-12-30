using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using System.Drawing;
using SFML.Window;
using SFML.Graphics;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class TabContainer : ScrollableContainer
    {
        public string tabName = "";
        public Sprite tabSprite = null;

        public TabContainer(string uniqueName, Size size, IResourceManager resourceManager)
            : base(uniqueName, size, resourceManager)
        {
            DrawBorder = false;
        }

        public string tabSpriteName
        {
            set { tabSprite = _resourceManager.GetSprite(value); }
        }

        public virtual void Activated() //Called when tab is selected.
        {

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