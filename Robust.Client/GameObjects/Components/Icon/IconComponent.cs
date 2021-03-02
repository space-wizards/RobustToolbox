using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    public class IconComponent : Component, ISerializationHooks
    {
        public override string Name => "Icon";
        public IDirectionalTextureProvider? Icon { get; private set; }

        [DataField("sprite")]
        private ResourcePath? rsi;
        [DataField("state")]
        private string? stateID;

        void ISerializationHooks.AfterDeserialization()
        {
            if (rsi != null && stateID != null)
            {
                Icon = new SpriteSpecifier.Rsi(rsi, stateID).Frame0();
            }
        }

        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public const string LogCategory = "go.comp.icon";
        const string SerializationCache = "icon";

        private static IRsiStateLike TextureForConfig(IconComponent compData, IResourceCache resourceCache)
        {
            return compData.Icon?.Default ?? resourceCache.GetFallback<TextureResource>().Texture;
        }

        public static IRsiStateLike? GetPrototypeIcon(EntityPrototype prototype, IResourceCache resourceCache)
        {
            if (!prototype.Components.TryGetValue("Icon", out var compData))
            {
                return null;
            }

            return TextureForConfig((IconComponent)compData, resourceCache);
        }
    }
}
