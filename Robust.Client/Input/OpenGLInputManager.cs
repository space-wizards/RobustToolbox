using Robust.Client.Interfaces.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Input
{
    internal class OpenGLInputManager : InputManager
    {
        [Dependency] private readonly IClyde _clyde;

        public override Vector2 MouseScreenPosition => _clyde.MouseScreenPosition;
    }
}
