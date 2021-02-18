using System.Collections.Generic;
using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.Credits
{
    /// <summary>
    ///     Contains credits information about the engine.
    /// </summary>
    public static class CreditsManager
    {
        /// <summary>
        ///     Gets a list of open source software used in the engine and their license.
        /// </summary>
        public static IEnumerable<LicenseEntry> GetLicenses()
        {
            var resM = IoCManager.Resolve<IResourceManager>();
            using var file = resM.ContentFileRead("/EngineCredits/Libraries.yml");
            using var reader = new StreamReader(file);
            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            foreach (var entry in (YamlSequenceNode)yamlStream.Documents[0].RootNode)
            {
                var mapNode = (YamlMappingNode) entry;
                var name = mapNode.GetNode("name").AsString();
                var license = mapNode.GetNode("license").AsString();

                yield return new LicenseEntry(name, license);
            }
        }

        public sealed class LicenseEntry
        {
            /// <summary>
            ///     Name of the software used.
            /// </summary>
            public string Name { get; }

            /// <summary>
            ///     The full license text of the project.
            /// </summary>
            public string License { get; }

            public LicenseEntry(string name, string license)
            {
                Name = name;
                License = license;
            }
        }
    }
}
