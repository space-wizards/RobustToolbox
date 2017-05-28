using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    public class ContextMenuComponent : Component
    {
        public override string Name => "ContextMenu";
        private readonly List<ContextMenuEntry> _entries = new List<ContextMenuEntry>();

        public ContextMenuComponent()
        {
            Family = ComponentFamily.ContextMenu;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.ContextAdd:
                    AddEntry((ContextMenuEntry) list[0]);
                    break;

                case ComponentMessageType.ContextRemove:
                    RemoveEntryByName((string) list[0]);
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

        public override void LoadParameters(Dictionary<string, YamlNode> mapping)
        {
            YamlNode node;
            if (mapping.TryGetValue("entries", out node))
            {
                foreach (YamlMappingNode entry in ((YamlSequenceNode)node).Cast<YamlMappingNode>())
                {
                    string name = "NULL";
                    string icon = "NULL";
                    string message = "NULL";

                    if (entry.Children.TryGetValue(new YamlScalarNode("name"), out node))
                    {
                        name = node.AsString();
                    }

                    if (entry.Children.TryGetValue(new YamlScalarNode("icon"), out node))
                    {
                        icon = node.AsString();
                    }

                    if (entry.Children.TryGetValue(new YamlScalarNode("message"), out node))
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

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
        }
    }
}
