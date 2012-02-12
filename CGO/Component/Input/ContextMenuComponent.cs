using System;
using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;
using System.Xml.Linq;

namespace CGO
{
    public class ContextMenuComponent : GameObjectComponent
    {
        private readonly List<ContextMenuEntry> _entries = new List<ContextMenuEntry>();

        public override ComponentFamily Family
        {
            get { return ComponentFamily.ContextMenu; }
        }
        
        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> reply, params object[] list)
        {
            base.RecieveMessage(sender, type, reply, list);

            if (sender == this) //Don't listen to our own messages!
                return;

            switch (type)
            {
                case ComponentMessageType.ContextAdd:
                    AddEntry((ContextMenuEntry)list[0]);
                    break;

                case ComponentMessageType.ContextRemove:
                    RemoveEntryByName((string)list[0]);
                    break;

                case ComponentMessageType.ContextGetEntries:
                    var compReply = new ComponentReplyMessage(ComponentMessageType.ContextGetEntries,_entries);
                    reply.Add(compReply);
                    break;
            }
            
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
            foreach (var param in extendedParameters.Descendants("ContextEntry"))
            {
                var name = "NULL";
                var icon = "NULL";
                var message = "NULL";

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

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
        }
    }
}
