using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces.Resource;
using GameObject;
using SS13.IoC;
using SS13_Shared.GO;
using GorgonLibrary.Graphics;

namespace CGO
{
    public class IconComponent : Component
    {
        public Sprite Icon;

        public IconComponent()
        {
            Family = ComponentFamily.Icon;
        }

        /// <summary>
        /// Set parameters :)
        /// </summary>
        /// <param name="parameter"></param>
        public override void SetParameter(ComponentParameter parameter)
        {
            //base.SetParameter(parameter);
            switch (parameter.MemberName)
            {
                case "icon":
                    var iconName = parameter.GetValue<string>();
                    SetIcon(iconName);
                    break;
            }
        }

        public void SetIcon(string name)
        {
            Icon = IoCManager.Resolve<IResourceManager>().GetSprite(name);
        }
    }
}
