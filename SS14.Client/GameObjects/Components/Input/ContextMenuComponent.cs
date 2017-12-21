using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    public class ContextMenuComponent : Component
    {
        public override string Name => "ContextMenu";
        private readonly List<ContextMenuEntry> _entries = new List<ContextMenuEntry>();

        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.ContextAdd:
                    AddEntry((ContextMenuEntry)list[0]);
                    break;

                case ComponentMessageType.ContextRemove:
                    RemoveEntryByName((string)list[0]);
                    break;

                case ComponentMessageType.ContextGetEntries:
                    reply = new ComponentReplyMessage(ComponentMessageType.ContextGetEntries, _entries);
                    break;
            }

            return reply;
        }

        public void RemoveEntryByName(string name)
        {
            _entries.RemoveAll(x => x.EntryName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public void RemoveEntryByMessage(string message)
        {
            _entries.RemoveAll(x => x.ComponentMessage.Equals(message, StringComparison.InvariantCultureIgnoreCase));
        }

        public void AddEntry(ContextMenuEntry entry)
        {
            _entries.Add(entry);
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            if (mapping.TryGetNode<YamlSequenceNode>("entries", out var sequence))
            {
                foreach (YamlMappingNode entry in sequence.Cast<YamlMappingNode>())
                {
                    string name = "NULL";
                    string icon = "NULL";
                    string message = "NULL";
                    YamlNode node;

                    if (entry.TryGetNode("name", out node))
                    {
                        name = node.AsString();
                    }

                    if (entry.TryGetNode("icon", out node))
                    {
                        icon = node.AsString();
                    }

                    if (entry.TryGetNode("message", out node))
                    {
                        message = node.AsString();
                    }

                    var newEntry = new ContextMenuEntry
                    {
                        EntryName = name,
                        IconName = icon,
                        ComponentMessage = message
                    };

                    _entries.Add(newEntry);
                }
            }
        }
    }
}
