using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;

namespace Robust.Client.Graphics.Lighting
{
    public sealed class LightManager : ILightManager
    {
#pragma warning disable 649
        [Dependency] private readonly IConfigurationManager _configManager;
#pragma warning restore 649

        public bool Enabled { get; set; } = true;
    }
}
