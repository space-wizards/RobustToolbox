using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.FileSystems;
using GorgonLibrary.Framework;
using GorgonLibrary.GUI;
using GorgonLibrary.Graphics.Utilities;

namespace SS3D.UserInterface
{
    public sealed class UIDesktop : Desktop
    {
        private static UIDesktop instance;

        private UIDesktop(Input input, GUISkin skin)
        : base(input, skin)
        { }

        public static UIDesktop Singleton
        {
            get
            {
                if (instance == null)
                    throw new Exception("UIDesktop not initialized.");
                else
                    return instance;
            }
        }

        public static void Initialize(Input input, GUISkin skin)
        {
            instance = new UIDesktop(input, skin);
        }
    }
 
}
