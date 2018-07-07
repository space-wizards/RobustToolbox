using System.Collections.Generic;
using System.IO;
using SS14.Server.Interfaces.Console;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.Utility;
using YamlDotNet.Serialization;

namespace SS14.Server.Console
{
    /// <inheritdoc />
    internal class PermGroupContainer : IPermGroupContainer
    {
        private static readonly ResourcePath _groupPath = new ResourcePath("/Groups/groups.yml");

        private readonly IResourceManager _resMan;
        private readonly ISawmill _logger;

        private readonly Dictionary<int, IPermGroup> _groups = new Dictionary<int, IPermGroup>();
        
        /// <inheritdoc />
        public IReadOnlyDictionary<int, IPermGroup> Groups => _groups;

        public PermGroupContainer(IResourceManager resMan, ISawmill logger)
        {
            _resMan = resMan;
            _logger = logger;
        }

        /// <inheritdoc />
        public void LoadGroups()
        {
            if (_resMan.UserData.Exists(_groupPath))
            {
                _logger.Info($"Loading permGroups from UserData: {_groupPath}");
                var file = _resMan.UserData.Open(_groupPath, FileMode.Open);
                LoadGroupYamlStream(file);
                return;
            }

            if (_resMan.TryContentFileRead(_groupPath, out var memoryStream))
            {
                _logger.Info($"Loading permGroups from content: {_groupPath}");
                LoadGroupYamlStream(memoryStream);
            }

            _logger.Warning($"Permission group file not found: {_groupPath}");
        }

        private void LoadGroupYamlStream(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var groupList = new Deserializer().Deserialize<List<PermGroup>>(reader);

                foreach (var permGroup in groupList)
                {
                    _groups.Add(permGroup.Index, permGroup);
                }
            }
        }

        /// <inheritdoc />
        public void SaveGroups()
        {
            _logger.Info($"Saving permGroups to UserData: {_groupPath}");
            _resMan.UserData.CreateDir(_groupPath);
            var file = _resMan.UserData.Open(_groupPath, FileMode.Create);

            using (var sw = new StreamWriter(file))
            {
                var serializer = new Serializer();
                serializer.Serialize(sw, _groups.Values);
            }
        }
    }
}
