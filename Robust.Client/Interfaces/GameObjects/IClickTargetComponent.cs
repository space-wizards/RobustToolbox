using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Map;

namespace Robust.Client.Interfaces.GameObjects
{
    /// <summary>
    /// A component that can be clicked on. Handles whether a coordinate is a valid place to click us on.
    /// </summary>
    public interface IClickTargetComponent : IComponent
    {
        int DrawDepth { get; }
    }
}
