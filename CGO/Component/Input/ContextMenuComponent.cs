using System;
using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;
using System.Xml.Linq;

namespace CGO
{
    public class ContextMenuComponent : GameObjectComponent
    {
        private List<ContextMenuEntry> entries = new List<ContextMenuEntry>();

        public ContextMenuComponent()
        {
            family = ComponentFamily.ContextMenu;
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
                    ComponentReplyMessage compReply = new ComponentReplyMessage(ComponentMessageType.ContextGetEntries,entries);
                    reply.Add(compReply);
                    break;
            }
            
        }

        public void RemoveEntryByName(string name)
        {
            entries.RemoveAll(x => x.EntryName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public void RemoveEntryByMessage(string message)
        {
            entries.RemoveAll(x => x.ComponentMessage.Equals(message, StringComparison.InvariantCultureIgnoreCase));
        }

        public void AddEntry(ContextMenuEntry entry)
        {
            entries.Add(entry);
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

                var newEntry = new ContextMenuEntry();
                newEntry.EntryName = name;
                newEntry.IconName = icon;
                newEntry.ComponentMessage = message;

                entries.Add(newEntry);
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
        }
    }
}
