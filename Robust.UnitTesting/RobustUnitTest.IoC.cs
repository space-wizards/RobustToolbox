using System;
using Robust.Client;
using Robust.Server;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace Robust.UnitTesting
{
    public partial class RobustUnitTest
    {
        /// <summary>
        /// Registers all the types into the <see cref="IoCManager"/> with <see cref="IoCManager.Register{TInterface, TImplementation}"/>
        /// </summary>
        private void RegisterIoC()
        {
            var dependencies = IoCManager.Instance!;

            switch (Project)
            {
                case UnitTestProject.Client:
                    ClientIoC.RegisterIoC(GameController.DisplayMode.Headless, dependencies);
                    break;

                case UnitTestProject.Server:
                    ServerIoC.RegisterIoC(dependencies);
                    break;

                default:
                    throw new NotSupportedException($"Unknown testing project: {Project}");
            }

            dependencies.Register<IModLoader, TestingModLoader>(overwrite: true);
            dependencies.Register<IModLoaderInternal, TestingModLoader>(overwrite: true);
            dependencies.Register<TestingModLoader, TestingModLoader>(overwrite: true);

            OverrideIoC();

            dependencies.BuildGraph();
        }
    }
}
