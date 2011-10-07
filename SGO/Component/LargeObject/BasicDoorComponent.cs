using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO
{
    public class BasicDoorComponent : BasicLargeObjectComponent
    {
        bool Open = false;
        string openSprite = "";
        string closedSprite = "";

        public BasicDoorComponent()
            :base()
        {
            family = SS3D_shared.GO.ComponentFamily.LargeObject;
        }

        /// <summary>
        /// Entry point for interactions between an item and this object
        /// Basically, the actor uses an item on this object
        /// </summary>
        /// <param name="actor">the actor entity</param>
        protected override void HandleItemToLargeObjectInteraction(Entity actor)
        {
            //Get the item
            //Get item type infos to apply to this object
            //Message item to tell it it was applied to this object
        }

        /// <summary>
        /// Entry point for interactions between an empty hand and this object
        /// Basically, actor "uses" this object
        /// </summary>
        /// <param name="actor">The actor entity</param>
        protected override void HandleEmptyHandToLargeObjectInteraction(Entity actor)
        {
            //Apply actions
            if (Open)
            {
                Open = !Open;
                Owner.SendMessage(this, ComponentMessageType.EnableCollision, null);
                Owner.SendMessage(this, ComponentMessageType.SetSpriteByKey, null, closedSprite);
            }
            else
            {
                Open = !Open;
                Owner.SendMessage(this, ComponentMessageType.DisableCollision, null);
                Owner.SendMessage(this, ComponentMessageType.SetSpriteByKey, null, openSprite);
            }
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "OpenSprite":
                    openSprite = (string)parameter.Parameter;
                    break;
                case "ClosedSprite":
                    closedSprite = (string)parameter.Parameter;
                    break;
            }
        }
    }
}
