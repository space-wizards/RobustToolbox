using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.GameObjects.Components.Renderable
{
    public partial class SpriteComponentData : ISerializationHooks
    {
        [DataClassTarget("rsi")]
        public RSI? RSI;

        [DataClassTarget("layerDatums")]
        public List<SharedSpriteComponent.PrototypeLayerData>? LayerDatums;

        [DataField("sprite", readOnly: true)] private string? rsi;
        [DataField("layers", readOnly: true)] private List<SharedSpriteComponent.PrototypeLayerData> layerDatums = new ();

        [DataField("state", readOnly: true)] private string? state;
        [DataField("texture", readOnly: true)] private string? texture;

        public void AfterDeserialization()
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();

            {
                if (!string.IsNullOrWhiteSpace(rsi))
                {
                    var rsiPath = SpriteComponent.TextureRoot / rsi;
                    try
                    {
                        if(rsi.EndsWith("apc.rsi")) System.Console.Write("a");
                        RSI = resourceCache.GetResource<RSIResource>(rsiPath).RSI;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS(SpriteComponent.LogCategory, "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, e);
                    }
                }
            }

            if(layerDatums.Count == 0){
                if (state != null || texture != null)
                {
                    layerDatums.Insert(0, new SharedSpriteComponent.PrototypeLayerData
                    {
                        TexturePath = string.IsNullOrWhiteSpace(texture) ? null : texture,
                        State = string.IsNullOrWhiteSpace(state) ? null : state,
                        Color = Color.White,
                        Scale = Vector2.One,
                        Visible = true,
                    });
                }
            }

            if (layerDatums.Count != 0)
                LayerDatums = layerDatums;
        }
    }
}
