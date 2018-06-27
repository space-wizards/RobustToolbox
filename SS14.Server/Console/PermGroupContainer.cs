using System.Collections.Generic;
using System.IO;
using SS14.Server.Interfaces.Console;
using SS14.Shared.Interfaces;
using SS14.Shared.Utility;
using YamlDotNet.Serialization;

namespace SS14.Server.Console
{
    /// <inheritdoc />
    internal class PermGroupContainer : IPermGroupContainer
    {
        private static readonly ResourcePath _groupPath = new ResourcePath("/Groups/groups.yml");

        private readonly Dictionary<int, IPermGroup> _groups = new Dictionary<int, IPermGroup>();

        /// <inheritdoc />
        public IReadOnlyDictionary<int, IPermGroup> Groups => _groups;

        /// <inheritdoc />
        public void LoadGroups(IResourceManager resMan)
        {
            var rootPath = resMan.ConfigDirectory;
            var path = Path.Combine(rootPath, "."+_groupPath.ToString());
            var filePath = Path.GetFullPath(path);

            if (!File.Exists(filePath))
                return;

            if (!resMan.TryContentFileRead(_groupPath, out var memoryStream))
                return;

            using (var reader = new StreamReader(memoryStream))
            {
                var groupList = new Deserializer().Deserialize<List<PermGroup>>(reader);

                foreach (var permGroup in groupList)
                {
                    _groups.Add(permGroup.Index, permGroup);
                }
            }
        }

        /// <inheritdoc />
        public void SaveGroups(IResourceManager resMan)
        {
            var rootPath = resMan.ConfigDirectory;
            var path = Path.Combine(rootPath, "."+_groupPath.ToString());
            var filePath = Path.GetFullPath(path);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var sw = new StreamWriter(filePath))
            {
                var serializer = new Serializer();
                serializer.Serialize(sw, _groups.Values);
            }
        }
    }
}
