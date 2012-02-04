using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GorgonLibrary;
using GorgonLibrary.Framework;
using GorgonLibrary.GUI;
using GorgonLibrary.Graphics;
using GorgonLibrary.Graphics.Utilities;
using GorgonLibrary.InputDevices;
using ClientResourceManager;
using SS13.Modules;
using CGO;

namespace SS13
{
    public class DragDropInfo
    {
        public Entity dragEntity { get; private set; }
        public Sprite dragSprite { get; private set; }
        public bool isEntity { get; private set;}
        //Ability

        public void Reset()
        {
            dragEntity = null;
            dragSprite = null;
            isEntity = true;
            //Ability
        }

        public void StartDrag(Entity ent)
        {
            dragEntity = ent;
            dragSprite = Utilities.GetSpriteComponentSprite(ent);
            isEntity = true;
        }

        //public void StartDrag(Ability ab)
        //{
        //    isEntity = false;
        //}
    }
}
