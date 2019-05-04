using System;
using System.Linq;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Lighting
{
    partial class LightManager
    {
        sealed class Occluder : IOccluder
        {
            private bool visible = true;

            public bool Enabled
            {
                get => visible;
                set
                {
                    visible = value;
                    UpdateEnabled();
                }
            }

            public bool Disposed { get; private set; }

            private LightManager Manager;

            private bool Deferred => Manager.System == LightingSystem.Deferred;

            public OccluderCullMode CullMode
            {
                get => default;
                set
                {
                }
            }

            public Occluder(LightManager manager)
            {
                Manager = manager;
            }

            public void Dispose()
            {
                // Already disposed.
                if (Disposed)
                {
                    return;
                }

                Manager.RemoveOccluder(this);
                Disposed = true;
            }

            public void SetPolygon(Vector2[] polygon)
            {
            }

            public void DeParent()
            {
            }

            public void ParentTo(ITransformComponent node)
            {
            }


            private void UpdateEnabled()
            {
            }

            public void FrameProcess(FrameEventArgs args)
            {
            }
        }
    }
}
