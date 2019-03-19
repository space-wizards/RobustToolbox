using SS14.Client.Interfaces.Graphics;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.Input
{
    internal class OpenGLInputManager : InputManager
    {
        [Dependency] private readonly IClyde _clyde;

        public override Vector2 MouseScreenPosition => _clyde.MouseScreenPosition;
    }
}
