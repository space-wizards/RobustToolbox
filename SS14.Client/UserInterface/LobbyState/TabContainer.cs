using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Interfaces.Resource;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    internal class TabContainer : ScrollableContainer
    {
        public string tabName = "";
        public Sprite tabSprite = null;

        public TabContainer(string uniqueName, Vector2i size, IResourceCache resourceCache)
            : base(uniqueName, size, resourceCache)
        {
            DrawBorder = false;
        }

        public string tabSpriteName
        {
            set { tabSprite = ResourceCache.GetSprite(value); }
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
