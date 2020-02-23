using System;
using System.Buffers;
using System.Linq;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Containers;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    // "HLR" stands for "high level rendering" here.
    // The left side of my monitor only has so much space, OK?
    // The idea is this shouldn't contain too much GL specific stuff.
    internal partial class Clyde
    {
        public ClydeDebugLayers DebugLayers { get; set; }

        private readonly RefList<(SpriteComponent sprite, Matrix3 worldMatrix, Angle worldRotation)> _drawingSpriteList
            =
            new RefList<(SpriteComponent, Matrix3, Angle)>();

        public void Render()
        {
            var size = ScreenSize;
            if (size.X == 0 || size.Y == 0 || _isMinimized)
            {
                ClearFramebuffer(Color.Black);

                // We have to keep running swapbuffers here
                // or else the user's PC will turn into a heater!!
                SwapBuffers();
                return;
            }

            _debugStats.Reset();

            // Basic pre-render busywork.
            // Clear screen to black.
            ClearFramebuffer(Color.Black);

            // Update shared UBOs.
            _updateUniformConstants();

            SetSpaceFull(CurrentSpace.ScreenSpace);

            // Short path to render only the splash.
            if (_drawingSplash)
            {
                DrawSplash(_renderHandle);
                FlushRenderQueue();
                SwapBuffers();
                return;
            }

            void RenderOverlays(OverlaySpace space)
            {
                using (DebugGroup($"Overlays: {space}"))
                {
                    foreach (var overlay in _overlayManager.AllOverlays
                        .Where(o => o.Space == space)
                        .OrderBy(o => o.ZIndex))
                    {
                        overlay.ClydeRender(_renderHandle);
                    }

                    FlushRenderQueue();
                }
            }

            RenderOverlays(OverlaySpace.ScreenSpaceBelowWorld);

            SetSpaceFull(CurrentSpace.WorldSpace);

            // Calculate world-space AABB for camera, to cull off-screen things.
            var eye = _eyeManager.CurrentEye;
            var worldBounds = Box2.CenteredAround(eye.Position.Position,
                _framebufferSize / (float) EyeManager.PIXELSPERMETER * eye.Zoom);

            using (DebugGroup("Lights"))
            {
                DrawLightsAndFov(worldBounds, eye);
            }

            using (DebugGroup("Grids"))
            {
                _drawGrids(worldBounds);
            }

            using (DebugGroup("Entities"))
            {
                DrawEntities(worldBounds);
            }

            RenderOverlays(OverlaySpace.WorldSpace);

            ApplyFovToBuffer(eye);

            _lightingReady = false;

            SetSpaceFull(CurrentSpace.ScreenSpace);

            if (DebugLayers == ClydeDebugLayers.Fov)
            {
                // NOTE
                _renderHandle.UseShader(_fovDebugShaderInstance);
                _renderHandle.DrawingHandleScreen.SetTransform(Matrix3.Identity);
                var pos = UIBox2.FromDimensions(ScreenSize / 2 - (200, 200), (400, 400));
                _renderHandle.DrawingHandleScreen.DrawTextureRect(FovTexture, pos);
            }

            if (DebugLayers == ClydeDebugLayers.Light)
            {
                _renderHandle.UseShader(null);
                _renderHandle.DrawingHandleScreen.SetTransform(Matrix3.Identity);
                _renderHandle.DrawingHandleScreen.DrawTextureRect(_wallBleedIntermediateRenderTarget2.Texture, UIBox2.FromDimensions(Vector2.Zero, ScreenSize), new Color(1, 1, 1, 0.5f));
            }


            RenderOverlays(OverlaySpace.ScreenSpace);

            using (DebugGroup("UI"))
            {
                _userInterfaceManager.Render(_renderHandle);
                FlushRenderQueue();
            }

            // And finally, swap those buffers!
            SwapBuffers();
        }

        private void DrawEntities(Box2 worldBounds)
        {
            if (_eyeManager.CurrentMap == MapId.Nullspace || !_mapManager.HasMapEntity(_eyeManager.CurrentMap))
            {
                return;
            }

            // So we could calculate the correct size of the entities based on the contents of their sprite...
            // Or we can just assume that no entity is larger than 10x10 and get a stupid easy check.
            // TODO: Make this check more accurate.
            var widerBounds = worldBounds.Enlarged(5);

            var mapEntity = _mapManager.GetMapEntity(_eyeManager.CurrentMap);

            var identity = Matrix3.Identity;
            ProcessSpriteEntities(mapEntity, ref identity, Angle.Zero, widerBounds, _drawingSpriteList);

            // We use a separate list for indexing so that the sort is faster.
            var indexList = ArrayPool<int>.Shared.Rent(_drawingSpriteList.Count);

            for (var i = 0; i < _drawingSpriteList.Count; i++)
            {
                indexList[i] = i;
            }

            Array.Sort(indexList, 0, _drawingSpriteList.Count, new SpriteDrawingOrderComparer(_drawingSpriteList));

            for (var i = 0; i < _drawingSpriteList.Count; i++)
            {
                ref var entry = ref _drawingSpriteList[indexList[i]];
                Vector2i roundedPos = default;
                if (entry.sprite.PostShader != null)
                {
                    _renderHandle.UseRenderTarget(EntityPostRenderTarget);
                    _renderHandle.Clear(new Color());
                    // Calculate viewport so that the entity thinks it's drawing to the same position,
                    // which is necessary for light application,
                    // but it's ACTUALLY drawing into the center of the render target.
                    var spritePos = entry.sprite.Owner.Transform.WorldPosition;
                    var screenPos = _eyeManager.WorldToScreen(spritePos);
                    var (roundedX, roundedY) = roundedPos = (Vector2i) screenPos;
                    var flippedPos = new Vector2i(roundedX, ScreenSize.Y - roundedY);
                    flippedPos -= EntityPostRenderTarget.Size / 2;
                    _renderHandle.Viewport(Box2i.FromDimensions(-flippedPos, ScreenSize));
                }

                entry.sprite.Render(_renderHandle.DrawingHandleWorld, entry.worldMatrix, entry.worldRotation);

                if (entry.sprite.PostShader != null)
                {
                    _renderHandle.UseRenderTarget(null);
                    _renderHandle.Viewport(Box2i.FromDimensions(Vector2i.Zero, ScreenSize));

                    _renderHandle.UseShader(entry.sprite.PostShader);
                    _renderHandle.SetSpace(CurrentSpace.ScreenSpace);
                    _renderHandle.SetModelTransform(Matrix3.Identity);

                    var rounded = roundedPos - EntityPostRenderTarget.Size / 2;

                    var box = Box2i.FromDimensions(rounded, EntityPostRenderTarget.Size);

                    _renderHandle.DrawTexture(EntityPostRenderTarget.Texture, box.BottomLeft,
                        box.TopRight, Color.White, null, 0);

                    _renderHandle.SetSpace(CurrentSpace.WorldSpace);
                    _renderHandle.UseShader(null);
                }
            }

            _drawingSpriteList.Clear();

            FlushRenderQueue();
        }

        private void ProcessSpriteEntities(IEntity entity, ref Matrix3 parentTransform, Angle parentRotation,
            Box2 worldBounds, RefList<(SpriteComponent, Matrix3, Angle)> list)
        {
            entity.TryGetComponent(out ContainerManagerComponent containerManager);

            var localMatrix = entity.Transform.GetLocalMatrix();
            Matrix3.Multiply(ref localMatrix, ref parentTransform, out var matrix);
            var rotation = parentRotation + entity.Transform.LocalRotation;

            foreach (var child in entity.Transform.ChildEntityUids)
            {
                var childEntity = _entityManager.GetEntity(child);

                if (containerManager != null && containerManager.TryGetContainer(childEntity, out var container) &&
                    !container.ShowContents)
                {
                    continue;
                }

                var childTransform = childEntity.Transform;

                var worldPosition = Matrix3.Transform(matrix, childTransform.LocalPosition);

                if (worldBounds.Contains(worldPosition))
                {
                    if (childEntity.TryGetComponent(out SpriteComponent sprite) && sprite.Visible)
                    {
                        ref var entry = ref list.AllocAdd();

                        var childLocalMatrix = childTransform.GetLocalMatrix();
                        Matrix3.Multiply(ref childLocalMatrix, ref matrix, out entry.Item2);
                        var childWorldRotation = rotation + childTransform.LocalRotation;

                        entry.Item1 = sprite;
                        entry.Item3 = childWorldRotation;
                    }
                }

                if (childTransform.ChildCount > 0)
                {
                    ProcessSpriteEntities(childEntity, ref matrix, rotation, worldBounds, list);
                }
            }
        }

        private void DrawSplash(IRenderHandle handle)
        {
            var texture = _resourceCache.GetResource<TextureResource>("/Textures/Logo/logo.png").Texture;

            handle.DrawingHandleScreen.DrawTexture(texture, (ScreenSize - texture.Size) / 2);
        }
    }
}
