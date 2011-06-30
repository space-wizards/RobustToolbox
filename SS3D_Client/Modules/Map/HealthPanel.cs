using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mogre;
using Miyagi;
using Miyagi.UI;
using Miyagi.UI.Controls;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Resources;
using Miyagi.Common.Events;
using Miyagi.TwoD;

namespace SS3D.Modules.Map
{
    public class HealthPanel
    {
        public OgreManager mEngine;
        public Panel control;
        public PictureBox bodyGraphic;
        Size size;
        Point location;
        Skin HealthPanelSkin;
        System.Drawing.Bitmap fullHealthImg;
        System.Drawing.Bitmap midHealthImg;
        System.Drawing.Bitmap lowHealthImg;
        
        public HealthPanel(OgreManager _mEngine)
        {
            mEngine = _mEngine;
            size = new Size(48, 105);
            location = new Point(10, mEngine.GetScreenSize().Y - 115);
            HealthPanelSkin = MiyagiResources.Singleton.Skins["HealthPanelSkin"];
            fullHealthImg = (System.Drawing.Bitmap)System.Drawing.Image.FromFile("../../../Media/GUI/HuD/healthgreen.png");
            midHealthImg = (System.Drawing.Bitmap)System.Drawing.Image.FromFile("../../../Media/GUI/HuD/healthyellow.png");
            lowHealthImg = (System.Drawing.Bitmap)System.Drawing.Image.FromFile("../../../Media/GUI/HuD/healthred.png");
        }

        public void SetSize(int x, int y)
        {
            size = new Size(x, y);
            control.Size = size;
        }

        public void SetLocation(int x, int y)
        {
            location = new Point(x, y);
            control.Location = location;
        }
        
        public void Initialize()
        {
            control = new Panel("healthPanel")
            {
                Size = size,
                Location = location,
                Skin = HealthPanelSkin,
            };

            // The actual health graphic - this will need to be changed if we do regional damage.
            bodyGraphic = new PictureBox("healthBodyBox")
            {
                Size = new Size(42, 99),
                Location = new Point(2, 3),
                Bitmap = fullHealthImg
            };

            control.Controls.Add(bodyGraphic);
        }
        /// <summary>
        /// sets the skin from a percentage health.
        /// </summary>
        public void SetHealth(int healthPercent)
        {
            if (healthPercent >= 90)
                bodyGraphic.Bitmap = fullHealthImg;
            else if (healthPercent >= 35)
                bodyGraphic.Bitmap = midHealthImg;
            else
                bodyGraphic.Bitmap = lowHealthImg;
        }
    }
}
