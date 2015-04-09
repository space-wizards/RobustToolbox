using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using Sprite = SS14.Client.Graphics.CluwneLib.Sprite.CluwneSprite;

namespace SS14.Client.GameObjects
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
