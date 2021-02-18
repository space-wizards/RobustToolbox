using Robust.Shared.Enums;
﻿using Robust.Client.Graphics;


namespace Robust.Client.Placement
{
    public partial class PlacementManager
    {
        internal class PlacementOverlay : Overlay
        {
            private readonly PlacementManager _manager;
            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public PlacementOverlay(PlacementManager manager)
            {
                _manager = manager;
                ZIndex = 100;
            }

            protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
            {
                _manager.Render((DrawingHandleWorld) handle);
            }
        }
    }
}
