using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
using System.Text;
using System.Text.RegularExpressions;

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
            entries.RemoveAll(x => x.entryName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public void RemoveEntryByMessage(string message)
        {
            entries.RemoveAll(x => x.componentMessage.Equals(message, StringComparison.InvariantCultureIgnoreCase));
        }

        public void AddEntry(ContextMenuEntry entry)
        {
            entries.Add(entry);
        }

        public override void Shutdown()
        {
            base.Shutdown();
        }

        public override void OnRemove()
        {
            base.OnRemove();
        }

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName.ToLowerInvariant())
            {
                case "addentry":
                    ContextMenuEntry newEntry = new ContextMenuEntry();

                    string[] splitParam = Regex.Split((string)parameter.Parameter, ";");
                    if (splitParam.Count() != 3) throw new ArgumentException("Context Entry Missing Parameter.");

                    foreach (string current in splitParam)
                    {
                        string[] splitSub = Regex.Split(current, "=");
                        if (splitSub.Count() != 2) throw new ArgumentException("Malformed Context-Entry Parameter : '" + current + "'");

                        switch (splitSub[0].ToLowerInvariant())
                        {
                            case "name":
                                newEntry.entryName = splitSub[1];
                                break;

                            case "iconname":
                                newEntry.iconName = splitSub[1];
                                break;

                            case "message":
                                newEntry.componentMessage = splitSub[1];
                                break;
                        }
                    }
                    entries.Add(newEntry);
                    break;
            }
            return;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
        }
    }
}
