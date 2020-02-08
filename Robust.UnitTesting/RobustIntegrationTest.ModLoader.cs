using System.IO;
using System.Reflection;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Resources;

namespace Robust.UnitTesting
{
    public abstract partial class RobustIntegrationTest
    {
        private sealed class ModLoader : Robust.Shared.ContentPack.ModLoader, IModLoader
        {
            public Assembly ClientContentAssembly { get; set; }
            public Assembly ServerContentAssembly { get; set; }
            public Assembly SharedContentAssembly { get; set; }

            public override void LoadGameAssembly<T>(Stream assembly, Stream symbols = null)
            {
                if (TryLoadPreset<T>())
                {
                    return;
                }

                base.LoadGameAssembly<T>(assembly, symbols);
            }

            public override void LoadGameAssembly<T>(string diskPath)
            {
                if (TryLoadPreset<T>())
                {
                    return;
                }

                base.LoadGameAssembly<T>(diskPath);
            }

            public override bool TryLoadAssembly<T>(IResourceManager resMan, string assemblyName)
            {
                if (TryLoadPreset<T>())
                {
                    return true;
                }

                return base.TryLoadAssembly<T>(resMan, assemblyName);
            }

            private bool TryLoadPreset<T>() where T : GameShared
            {
                if (typeof(T) == typeof(GameShared) && SharedContentAssembly != null)
                {
                    InitMod<T>(SharedContentAssembly);
                    return true;
                }

                if (typeof(T) == typeof(GameServer) && ServerContentAssembly != null)
                {
                    InitMod<T>(ServerContentAssembly);
                    return true;
                }

                if (typeof(T) == typeof(GameClient) && ClientContentAssembly != null)
                {
                    InitMod<T>(ClientContentAssembly);
                    return true;
                }

                return false;
            }
        }
    }
}
