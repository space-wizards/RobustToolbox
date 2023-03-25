using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Server.GameObjects;

internal sealed class ServerComponentFactory : ComponentFactory
{
    public ServerComponentFactory(IDynamicTypeFactoryInternal typeFactory, IReflectionManager reflectionManager) : base(typeFactory, reflectionManager)
    {
        RegisterIgnore("Input");
        RegisterIgnore("AnimationPlayer");
        RegisterIgnore("GenericVisualizer");
    }
}
