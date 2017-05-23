using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    [Component("ContextMenu")]
    public class ContextMenuComponent : Component
    {
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

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.Children.TryGetValue(new YamlScalarNode("entries"), out node))
            {
                foreach (YamlMappingNode entry in (YamlSequenceNode)node)
                {
                    string name = "";
                    string icon = "";
                    string message = "";

                    if (entry.Children.TryGetValue(new YamlScalarNode("name"), out node))
                    {
                        name = ((YamlScalarNode)node).Value;
                    }

                    if (entry.Children.TryGetValue(new YamlScalarNode("icon"), out node))
                    {
                        icon = ((YamlScalarNode)node).Value;
                    }

                    if (entry.Children.TryGetValue(new YamlScalarNode("message"), out node))
                    {
                        message = ((YamlScalarNode)node).Value;
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
