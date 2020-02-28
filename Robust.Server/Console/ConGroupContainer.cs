using System;
using System.Collections.Generic;
using System.IO;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Utility;
using YamlDotNet.Serialization;

namespace Robust.Server.Console
{
    /// <summary>
    /// Contains the collection of groups for a console shell.
    /// </summary>
    internal class ConGroupContainer
    {
        private static readonly ResourcePath _groupPath = new ResourcePath("/Groups/groups.yml");

        private readonly IResourceManager _resMan;
        private readonly ISawmill _logger;
        private readonly Dictionary<ConGroupIndex, ConGroup> _groups = new Dictionary<ConGroupIndex, ConGroup>();

        /// <summary>
        ///     Map (groupIndex -> PermGroup) of all groups inside the container.
        /// </summary>
        public IReadOnlyDictionary<ConGroupIndex, ConGroup> Groups => _groups;

        /// <summary>
        ///     Creates a new instance of <see cref="ConGroupContainer" /> .
        /// </summary>
        /// <param name="resMan">ResourceManager to use for file I/O.</param>
        /// <param name="logger">Sawmill to use for logging messages.</param>
        public ConGroupContainer(IResourceManager resMan, ISawmill logger)
        {
            _resMan = resMan;
            _logger = logger;
        }

        /// <summary>
        ///     Loads groups from the yaml file. Existing groups will not be overwritten.
        ///     Call Clear() if you want to empty the container first.
        /// </summary>
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
                return;
            }

            _logger.Warning($"Permission group file not found: {_groupPath}");
        }

        private void LoadGroupYamlStream(Stream stream)
        {
            try
            {
                using (var reader = new StreamReader(stream))
                {
                    var groupList = new Deserializer().Deserialize<List<ConGroup>>(reader);

                    foreach (var permGroup in groupList)
                    {
                        var grpIndex = new ConGroupIndex(permGroup.Index);
                        if (!_groups.ContainsKey(grpIndex))
                            _groups.Add(grpIndex, permGroup);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Could not parse the yaml group file! {e.Message}");
            }
        }

        /// <summary>
        ///     Saves groups to the yaml file.
        /// </summary>
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

        /// <summary>
        ///     Removes all groups from the container.
        /// </summary>
        public void Clear()
        {
            _groups.Clear();
        }

        /// <summary>
        /// Tests if a console group has a command defined.
        /// </summary>
        /// <param name="groupIndex">Group to test.</param>
        /// <param name="cmdName">Name of command to test for.</param>
        /// <returns>Result of test.</returns>
        public bool HasCommand(ConGroupIndex groupIndex, string cmdName)
        {
            if (_groups.TryGetValue(groupIndex, out var group))
            {
                return group.Commands.Contains(cmdName);
            }

            _logger.Error($"Unknown groupIndex: {groupIndex}");
            return false;
        }

        public bool CanViewVar(ConGroupIndex groupIndex)
        {
            if (_groups.TryGetValue(groupIndex, out var group))
            {
                return group.CanViewVar;
            }

            _logger.Error($"Unknown groupIndex: {groupIndex}");
            return false;
        }

        public bool GroupExists(ConGroupIndex index)
        {
            return _groups.ContainsKey(index);
        }

        public bool CanAdminPlace(ConGroupIndex groupIndex)
        {
            if (_groups.TryGetValue(groupIndex, out var group))
            {
                return group.CanAdminPlace;
            }

            _logger.Error($"Unknown groupIndex: {groupIndex}");
            return false;
        }
    }
}
