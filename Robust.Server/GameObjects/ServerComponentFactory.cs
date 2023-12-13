using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;

namespace Robust.Server.GameObjects;

internal sealed class ServerComponentFactory : ComponentFactory
{
    public ServerComponentFactory(IDynamicTypeFactoryInternal typeFactory, IReflectionManager reflectionManager, ILogManager logManager) : base(typeFactory, reflectionManager, logManager)
    {
        RegisterIgnore("Input");
        RegisterIgnore("AnimationPlayer");
        RegisterIgnore("GenericVisualizer");
        RegisterIgnore("Sprite"); // Fucking finally
    }
}
