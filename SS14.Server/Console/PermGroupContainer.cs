using System.Collections.Generic;
using System.IO;
using SS14.Server.Interfaces.Console;
using SS14.Shared.Interfaces;
using SS14.Shared.Utility;
using YamlDotNet.Serialization;

namespace SS14.Server.Console
{
    internal class PermGroupContainer : IPermGroupContainer
    {
        private static readonly ResourcePath GroupPath = new ResourcePath("/Groups/groups.yml");

        private Dictionary<int, IPermGroup> _groups = new Dictionary<int, IPermGroup>();

        public IReadOnlyDictionary<int, IPermGroup> Groups => _groups;

        public void LoadGroups(IResourceManager resMan)
        {
            if (!resMan.TryContentFileRead(GroupPath, out var memoryStream))
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
        
    }
}
