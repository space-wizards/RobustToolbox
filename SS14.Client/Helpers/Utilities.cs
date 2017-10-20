using SS14.Client.Graphics.Sprites;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.Helpers
{
    internal static class Utilities
    {
        public static Sprite GetIconSprite(IEntity entity)
        {
            Sprite icon = null;
            if (entity.TryGetComponent<IconComponent>(out var component))
            {
                icon = component.Icon;
            }
            return icon ?? IoCManager.Resolve<IResourceCache>().DefaultSprite();
        }
    }
}
