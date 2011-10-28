using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using CGO;
using SS3D_shared.GO;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using CGO;
using SS3D_shared.GO;

namespace SS3D.HelperClasses
{
    static class Utilities
    {
        public static string GetObjectSpriteName(Type type)
        {
            if (type.IsSubclassOf(typeof(ClientServices.Map.Tiles.Tile)))
            {
                return "tilebuildoverlay";
            }
            /*else if (type.IsSubclassOf(typeof(Atom.Atom)))
            {
                Atom.Atom atom = (Atom.Atom)Activator.CreateInstance(type);
                string strName = atom.spritename;
                atom = null;
                return strName;
            }*/
            return "nosprite";
        }

        public static string GetAtomName(Type type)
        {
            /*if (type.IsSubclassOf(typeof(Atom.Atom)))
            {
                Atom.Atom atom = (Atom.Atom)Activator.CreateInstance(type);
                string strName = atom.name;
                atom = null;
                return strName;
            }*/
            return "missingName"; 
        }

        public static Sprite GetSpriteComponentSprite(Entity entity)
        {
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            entity.SendMessage(entity, ComponentMessageType.GetSprite, replies, null);
            if (replies.Where(l => l.messageType == ComponentMessageType.CurrentSprite).Any())
            {
                ComponentReplyMessage spriteMsg = replies.Where(l => l.messageType == ComponentMessageType.CurrentSprite).First();
                Sprite Sprite = (Sprite)spriteMsg.paramsList[0];
                return Sprite;
            }
            return null;
        }            
    }
}
