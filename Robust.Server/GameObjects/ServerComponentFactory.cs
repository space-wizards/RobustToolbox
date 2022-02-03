using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Server.GameObjects
{
    internal class ServerComponentFactory : ComponentFactory
    {
        public ServerComponentFactory(IDynamicTypeFactoryInternal typeFactory, IReflectionManager reflectionManager, IConsoleHost conHost)
            : base(typeFactory, reflectionManager, conHost)
        {
            RegisterIgnore("Input");
            RegisterIgnore("AnimationPlayer");
            RegisterClass<OccluderComponent>();
        }
    }
}
