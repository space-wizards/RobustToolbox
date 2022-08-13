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
            switch (Project)
            {
                case UnitTestProject.Client:
                    ClientIoC.RegisterIoC(GameController.DisplayMode.Headless, IoCManager.Instance!);
                    break;

                case UnitTestProject.Server:
                    ServerIoC.RegisterIoC(IoCManager.Instance!);
                    break;

                default:
                    throw new NotSupportedException($"Unknown testing project: {Project}");
            }

            IoCManager.Register<IModLoader, TestingModLoader>(overwrite: true);
            IoCManager.Register<IModLoaderInternal, TestingModLoader>(overwrite: true);
            IoCManager.Register<TestingModLoader, TestingModLoader>(overwrite: true);

            OverrideIoC();

            IoCManager.BuildGraph();
        }
    }
}
