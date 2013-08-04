using System;
using System.Collections.Generic;
using System.Xml.Linq;
using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
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

        public override void HandleExtendedParameters(XElement extendedParameters)
        {
            foreach (XElement param in extendedParameters.Descendants("ContextEntry"))
            {
                string name = "NULL";
                string icon = "NULL";
                string message = "NULL";

                if (param.Attribute("name") != null)
                    name = param.Attribute("name").Value;

                if (param.Attribute("icon") != null)
                    icon = param.Attribute("icon").Value;

                if (param.Attribute("message") != null)
                    message = param.Attribute("message").Value;

                var newEntry = new ContextMenuEntry
                                   {
                                       EntryName = name,
                                       IconName = icon,
                                       ComponentMessage = message
                                   };

                _entries.Add(newEntry);
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
        }
    }
}