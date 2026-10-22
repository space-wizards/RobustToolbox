using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;

namespace Robust.Server.GameObjects;

internal sealed class ServerComponentFactory : ComponentFactory
{
    public ServerComponentFactory(IDynamicTypeFactoryInternal typeFactory, IReflectionManager reflectionManager, ISerializationManager serManager, ILogManager logManager) : base(typeFactory, reflectionManager, serManager, logManager)
    {
        RegisterIgnore("Input");
        RegisterIgnore("AnimationPlayer");
        RegisterIgnore("GenericVisualizer");
        RegisterIgnore("Sprite"); // Fucking finally
    }
}
