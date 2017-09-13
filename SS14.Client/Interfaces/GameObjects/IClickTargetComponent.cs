<<<<<<< HEAD
﻿using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;
using SS14.Shared.Map;
=======
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;
>>>>>>> master-wizfederation

namespace SS14.Client.Interfaces.GameObjects
{
    /// <summary>
    /// A component that can be clicked on. Handles whether a coordinate is a valid place to click us on.
    /// </summary>
    public interface IClickTargetComponent : IComponent
    {
        bool WasClicked(LocalCoordinates worldPos);
        DrawDepth DrawDepth { get; }
    }
}
