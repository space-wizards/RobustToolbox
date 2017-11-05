using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    /// <summary>
    ///     It's like a regular panel, except EVERYTHING parented to it gets layed out in a column.
    ///     It does not resize itself based on its contents, and does not scroll contents.
    /// </summary>
    class ListPanel : Panel
    {
        public override void DoLayout()
        {
            var lastHeight = 0;
            foreach (var child in Children)
            {
                child.DoLayout(); // only called to set up the ClientRect
                child.LocalPosition = new Vector2i(0, lastHeight); // offset from top left of list
                child.Alignment = Align.None; // don't align yourself to anything special
                lastHeight += child.ClientArea.Height;
            }

            // do layout of myself and children
            base.DoLayout();

        }
    }
}
