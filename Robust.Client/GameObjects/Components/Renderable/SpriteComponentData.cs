using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using SharpFont;

namespace Robust.Client.GameObjects.Components.Renderable
{
    public partial class SpriteComponentData
    {
        [DataClassTarget("rsi")]
        public RSI? RSI;

        [DataClassTarget("layerDatums")]
        public List<SharedSpriteComponent.PrototypeLayerData>? LayerDatums;

        public void ExposeData(ObjectSerializer serializer)
        {
            var resourceCache = IoCManager.Resolve<IResourceCache>();

            {
                var rsi = serializer.ReadDataField<string?>("sprite", null);
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

            LayerDatums =
                serializer.ReadDataField("layers", new List<SharedSpriteComponent.PrototypeLayerData>());

            if(LayerDatums.Count == 0){
                var baseState = serializer.ReadDataField<string?>("state", null);
                var texturePath = serializer.ReadDataField<string?>("texture", null);

                if (baseState != null || texturePath != null)
                {
                    LayerDatums.Insert(0, new SharedSpriteComponent.PrototypeLayerData
                    {
                        TexturePath = string.IsNullOrWhiteSpace(texturePath) ? null : texturePath,
                        State = string.IsNullOrWhiteSpace(baseState) ? null : baseState,
                        Color = Color.White,
                        Scale = Vector2.One,
                        Visible = true,
                    });
                }
            }

            if (LayerDatums.Count == 0)
                LayerDatums = null;
        }
    }
}
