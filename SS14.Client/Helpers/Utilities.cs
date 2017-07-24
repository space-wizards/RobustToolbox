using SFML.Graphics;
using SFML.System;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Map;
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
            return icon ?? IoCManager.Resolve<IResourceManager>().GetNoSprite();
        }
    }
}
