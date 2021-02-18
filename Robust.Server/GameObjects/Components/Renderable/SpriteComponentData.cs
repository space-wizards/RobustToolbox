using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Server.GameObjects
{
    public partial class SpriteComponentData : ISerializationHooks
    {
        [DataClassTarget("layers")]
        [DataField("layers", priority: 2, readOnly: true)]
        private List<SharedSpriteComponent.PrototypeLayerData>? Layers;

        [DataField("state", readOnly: true)] private string? state;
        [DataField("texture", readOnly: true)] private string? texture;

        public void AfterDeserialization()
        {
            if (Layers == null) Layers = new();
            if(Layers.Count == 0){
                if (state != null || texture != null)
                {
                    var layerZeroData = SharedSpriteComponent.PrototypeLayerData.New();
                    if (!string.IsNullOrWhiteSpace(state))
                    {
                        layerZeroData.State = state;
                    }

                    if (!string.IsNullOrWhiteSpace(texture))
                    {
                        layerZeroData.TexturePath = texture;
                    }

                    Layers.Insert(0, layerZeroData);
                }
            }

            Layers = Layers.Count == 0 ? null : Layers;
        }
    }
}
