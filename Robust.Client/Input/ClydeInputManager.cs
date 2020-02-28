using Robust.Client.Interfaces.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Input
{
    internal sealed class ClydeInputManager : InputManager
    {
#pragma warning disable 649
        [Dependency] private readonly IClydeInternal _clyde;
#pragma warning restore 649

        public override Vector2 MouseScreenPosition => _clyde.MouseScreenPosition;

        public override string GetKeyName(Keyboard.Key key)
        {
            return _clyde.GetKeyName(key);
        }
    }
}
