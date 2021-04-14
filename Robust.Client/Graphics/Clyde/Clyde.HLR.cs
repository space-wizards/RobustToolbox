using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared.Enums;

namespace Robust.Client.Graphics.Clyde
{
    // "HLR" stands for "high level rendering" here.
    // The left side of my monitor only has so much space, OK?
    // The idea is this shouldn't contain too much GL specific stuff.
    internal partial class Clyde
    {
        public ClydeDebugLayers DebugLayers { get; set; }

        private readonly RefList<(SpriteComponent sprite, Matrix3 worldMatrix, Angle worldRotation, float yWorldPos)>
            _drawingSpriteList
                =
                new();

        public void Render()
        {
            CheckTransferringScreenshots();

            var size = ScreenSize;
            if (size.X == 0 || size.Y == 0 || _isMinimized)
            {
                ClearFramebuffer(Color.Black);

                // We have to keep running swapbuffers here
                // or else the user's PC will turn into a heater!!
                SwapBuffers();
                return;
            }

            // Completely flush renderer state back to 0.
            // This should make the renderer more robust
            // in case an exception got thrown during rendering of the previous frame.
            ClearRenderState();

            _debugStats.Reset();

            // Basic pre-render busywork.
            // Clear screen to black.
            ClearFramebuffer(Color.Black);

            // Update shared UBOs.
            _updateUniformConstants(_framebufferSize);

            {
                CalcScreenMatrices(ScreenSize, out var proj, out var view);
                SetProjViewFull(proj, view);
            }

            // Short path to render only the splash.
            if (_drawingSplash)
            {
                DrawSplash(_renderHandle);
                FlushRenderQueue();
                SwapBuffers();
                return;
            }

            foreach (var weak in _viewports.Values)
            {
                if (weak.TryGetTarget(out var viewport) && viewport.AutomaticRender)
                    RenderViewport(viewport);
            }

            TakeScreenshot(ScreenshotType.BeforeUI);

            using (DebugGroup("UI"))
            {
                _userInterfaceManager.Render(_renderHandle);
                FlushRenderQueue();
            }

            TakeScreenshot(ScreenshotType.AfterUI);

            // And finally, swap those buffers!
            SwapBuffers();
        }

        private void RenderOverlays(Viewport vp, OverlaySpace space)
        {
            using (DebugGroup($"Overlays: {space}"))
            {
                var list = GetOverlaysForSpace(space);
                foreach (var overlay in list)
                {
                    if (overlay.RequestScreenTexture)
                    {
                        FlushRenderQueue();
                        UpdateOverlayScreenTexture(space, vp.RenderTarget);
                    }

                    if (overlay.OverwriteTargetFrameBuffer())
                    {
                        ClearFramebuffer(default);
                    }

                    overlay.ClydeRender(_renderHandle, space);
                }
            }
        }

        private void RenderOverlaysDirect(DrawingHandleBase handle, OverlaySpace space)
        {
            var list = GetOverlaysForSpace(space);

            foreach (var overlay in list)
            {
                overlay.Draw(handle, space);
            }
        }

        private List<Overlay> GetOverlaysForSpace(OverlaySpace space)
        {
            var list = new List<Overlay>();

            foreach (var overlay in _overlayManager.AllOverlays)
            {
                if ((overlay.Space & space) != 0)
                {
                    list.Add(overlay);
                }
            }

            list.Sort(OverlayComparer.Instance);

            return list;
        }

        private ClydeTexture? ScreenBufferTexture;
        private GLHandle screenBufferHandle;
        private Vector2 lastFrameSize;

        /// <summary>
        ///    Sends SCREEN_TEXTURE to all overlays in the given OverlaySpace that request it.
        /// </summary>
        private bool UpdateOverlayScreenTexture(OverlaySpace space, RenderTexture texture)
        {
            //This currently does NOT consider viewports and just grabs the current screen framebuffer. This will need to be improved upon in the future.
            List<Overlay> oTargets = new List<Overlay>();
            foreach (var overlay in _overlayManager.AllOverlays)
            {
                if (overlay.RequestScreenTexture && overlay.Space == space)
                {
                    oTargets.Add(overlay);
                }
            }

            if (oTargets.Count > 0 && ScreenBufferTexture != null)
            {
                if (lastFrameSize != _framebufferSize)
                {
                    GL.BindTexture(TextureTarget.Texture2D, screenBufferHandle.Handle);
                    GL.TexImage2D(TextureTarget.Texture2D, 0,
                        _hasGLSrgb ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Rgba8, _framebufferSize.X,
                        _framebufferSize.Y, 0,
                        PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                }

                lastFrameSize = _framebufferSize;
                CopyRenderTextureToTexture(texture, ScreenBufferTexture);
                foreach (Overlay overlay in oTargets)
                {
                    overlay.ScreenTexture = ScreenBufferTexture;
                }

                oTargets.Clear();
                return true;
            }

            return false;
        }


        private void DrawEntities(Viewport viewport, Box2 worldBounds)
        {
            if (_eyeManager.CurrentMap == MapId.Nullspace || !_mapManager.HasMapEntity(_eyeManager.CurrentMap))
            {
                return;
            }

            RenderOverlays(viewport, OverlaySpace.WorldSpaceBelowEntities);

            var screenSize = viewport.Size;

            // So we could calculate the correct size of the entities based on the contents of their sprite...
            // Or we can just assume that no entity is larger than 10x10 and get a stupid easy check.
            // TODO: Make this check more accurate.
            var widerBounds = worldBounds.Enlarged(5);

            ProcessSpriteEntities(_eyeManager.CurrentMap, widerBounds, _drawingSpriteList);

            var worldOverlays = new List<Overlay>();

            foreach (var overlay in _overlayManager.AllOverlays)
            {
                if ((overlay.Space & OverlaySpace.WorldSpace) != 0)
                {
                    worldOverlays.Add(overlay);
                }
            }

            worldOverlays.Sort(OverlayComparer.Instance);

            // We use a separate list for indexing so that the sort is faster.
            var indexList = ArrayPool<int>.Shared.Rent(_drawingSpriteList.Count);

            for (var i = 0; i < _drawingSpriteList.Count; i++)
            {
                indexList[i] = i;
            }

            var overlayIndex = 0;
            Array.Sort(indexList, 0, _drawingSpriteList.Count, new SpriteDrawingOrderComparer(_drawingSpriteList));

            for (var i = 0; i < _drawingSpriteList.Count; i++)
            {
                ref var entry = ref _drawingSpriteList[indexList[i]];
                var flushed = false;

                for (var j = overlayIndex; j < worldOverlays.Count; j++)
                {
                    var overlay = worldOverlays[j];

                    if (overlay.ZIndex <= entry.sprite.DrawDepth)
                    {
                        if (!flushed)
                        {
                            FlushRenderQueue();
                            flushed = true;
                        }

                        overlay.ClydeRender(_renderHandle, OverlaySpace.WorldSpace);
                        overlayIndex = j;
                        continue;
                    }

                    break;
                }


                RenderTexture? entityPostRenderTarget = null;
                Vector2i roundedPos = default;
                if (entry.sprite.PostShader != null)
                {
                    // calculate world bounding box
                    var spriteBB = entry.sprite.CalculateBoundingBox();
                    var spriteLB = spriteBB.BottomLeft;
                    var spriteRT = spriteBB.TopRight;

                    // finally we can calculate screen bounding in pixels
                    var screenLB = viewport.WorldToLocal(spriteLB);
                    var screenRT = viewport.WorldToLocal(spriteRT);

                    // we need to scale RT a for effects like emission or highlight
                    // scale can be passed with PostShader as variable in future
                    var postShadeScale = 1.25f;
                    var screenSpriteSize = (Vector2i) ((screenRT - screenLB) * postShadeScale).Rounded();
                    screenSpriteSize.Y = -screenSpriteSize.Y;

                    // I'm not 100% sure why it works, but without it post-shader
                    // can be lower or upper by 1px than original sprite depending on sprite rotation or scale
                    // probably some rotation rounding error
                    if (screenSpriteSize.X % 2 != 0)
                        screenSpriteSize.X++;
                    if (screenSpriteSize.Y % 2 != 0)
                        screenSpriteSize.Y++;

                    // check that sprite size is valid
                    if (screenSpriteSize.X > 0 && screenSpriteSize.Y > 0)
                    {
                        // create new render texture with correct sprite size
                        entityPostRenderTarget = CreateRenderTarget(screenSpriteSize,
                            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                            name: nameof(entityPostRenderTarget));
                        _renderHandle.UseRenderTarget(entityPostRenderTarget);
                        _renderHandle.Clear(new Color());

                        // Calculate viewport so that the entity thinks it's drawing to the same position,
                        // which is necessary for light application,
                        // but it's ACTUALLY drawing into the center of the render target.
                        var spritePos = spriteBB.Center;
                        var screenPos = _eyeManager.WorldToScreen(spritePos);
                        var (roundedX, roundedY) = roundedPos = (Vector2i) screenPos;
                        var flippedPos = new Vector2i(roundedX, screenSize.Y - roundedY);
                        flippedPos -= entityPostRenderTarget.Size / 2;
                        _renderHandle.Viewport(Box2i.FromDimensions(-flippedPos, screenSize));
                    }
                }

                var matrix = entry.worldMatrix;
                var worldPosition = new Vector2(matrix.R0C2, matrix.R1C2);
                entry.sprite.Render(_renderHandle.DrawingHandleWorld, in entry.worldRotation, in worldPosition);

                if (entry.sprite.PostShader != null && entityPostRenderTarget != null)
                {
                    var oldProj = _currentMatrixProj;
                    var oldView = _currentMatrixView;

                    _renderHandle.UseRenderTarget(viewport.RenderTarget);
                    _renderHandle.Viewport(Box2i.FromDimensions(Vector2i.Zero, screenSize));

                    _renderHandle.UseShader(entry.sprite.PostShader);
                    CalcScreenMatrices(viewport.Size, out var proj, out var view);
                    _renderHandle.SetProjView(proj, view);
                    _renderHandle.SetModelTransform(Matrix3.Identity);

                    var rounded = roundedPos - entityPostRenderTarget.Size / 2;

                    var box = Box2i.FromDimensions(rounded, entityPostRenderTarget.Size);

                    _renderHandle.DrawTextureScreen(entityPostRenderTarget.Texture,
                        box.BottomLeft, box.BottomRight, box.TopLeft, box.TopRight,
                        Color.White, null);

                    _renderHandle.SetProjView(oldProj, oldView);
                    _renderHandle.UseShader(null);
                }
            }

            ArrayPool<int>.Shared.Return(indexList);

            _drawingSpriteList.Clear();
            FlushRenderQueue();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ProcessSpriteEntities(MapId map, Box2 worldBounds,
            RefList<(SpriteComponent sprite, Matrix3 matrix, Angle worldRot, float yWorldPos)> list)
        {
            var spriteSystem = _entitySystemManager.GetEntitySystem<RenderingTreeSystem>();

            var tree = spriteSystem.GetSpriteTreeForMap(map);

            tree.QueryAabb(ref list, ((
                ref RefList<(SpriteComponent sprite, Matrix3 matrix, Angle worldRot, float yWorldPos)> state,
                in SpriteComponent value) =>
            {
                if (value.ContainerOccluded || !value.Visible)
                {
                    return true;
                }

                var entity = value.Owner;
                var transform = entity.Transform;

                ref var entry = ref state.AllocAdd();
                entry.sprite = value;
                entry.worldRot = transform.WorldRotation;
                entry.matrix = transform.WorldMatrix;
                var worldPos = entry.matrix.Transform(transform.LocalPosition);
                entry.yWorldPos = worldPos.Y;
                return true;
            }), worldBounds, approx: true);
        }

        private void DrawSplash(IRenderHandle handle)
        {
            var texture = _resourceCache.GetResource<TextureResource>("/Textures/Logo/logo.png").Texture;

            handle.DrawingHandleScreen.DrawTexture(texture, (ScreenSize - texture.Size) / 2);
        }

        private void RenderViewport(Viewport viewport)
        {
            if (viewport.Eye == null || viewport.Eye.Position.MapId == MapId.Nullspace)
            {
                return;
            }

            using var _ = DebugGroup($"Viewport: {viewport.Name}");

            // TODO: for the love of god all this state pushing/popping needs to be cleaned up.

            var oldTransform = _currentMatrixModel;
            var oldScissor = _currentScissorState;

            // Have to flush the render queue so that all commands finish rendering to the previous framebuffer.
            FlushRenderQueue();

            var eye = viewport.Eye;

            var oldVp = _currentViewport;
            _currentViewport = viewport;

            var state = PushRenderStateFull();

            {
                // Actual code that isn't just pushing/popping renderer state so we can return safely.

                var rt = _currentViewport.RenderTarget;
                BindRenderTargetFull(RtToLoaded(rt));
                ClearFramebuffer(default);
                SetViewportImmediate(Box2i.FromDimensions(Vector2i.Zero, rt.Size));
                _updateUniformConstants(viewport.Size);

                CalcWorldMatrices(rt.Size, viewport.RenderScale, eye, out var proj, out var view);
                SetProjViewFull(proj, view);

                // Calculate world-space AABB for camera, to cull off-screen things.
                var worldBounds = Box2.CenteredAround(eye.Position.Position,
                    viewport.Size / (float) EyeManager.PixelsPerMeter * eye.Zoom);

                if (_eyeManager.CurrentMap != MapId.Nullspace)
                {
                    using (DebugGroup("Lights"))
                    {
                        DrawLightsAndFov(viewport, worldBounds, eye);
                    }

                    using (DebugGroup("Grids"))
                    {
                        _drawGrids(worldBounds);
                    }

                    // We will also render worldspace overlays here so we can do them under / above entities as necessary
                    using (DebugGroup("Entities"))
                    {
                        DrawEntities(viewport, worldBounds);
                    }

                    RenderOverlays(viewport, OverlaySpace.WorldSpaceBelowFOV);

                    if (_lightManager.Enabled && _lightManager.DrawHardFov && eye.DrawFov)
                    {
                        ApplyFovToBuffer(viewport, eye);
                    }
                }

                _lightingReady = false;

                if (DebugLayers == ClydeDebugLayers.Fov)
                {
                    // I'm refactoring this code and I found this comment:
                    // NOTE
                    // Yes, it just says "NOTE". Thank you past me.
                    // Anyways I'm 99% sure this was about the fact that this debug layer is actually broken.
                    // Because the math is wrong.
                    // So there are distortions from incorrect projection.
                    _renderHandle.UseShader(_fovDebugShaderInstance);
                    _renderHandle.DrawingHandleScreen.SetTransform(Matrix3.Identity);
                    var pos = UIBox2.FromDimensions(ScreenSize / 2 - (200, 200), (400, 400));
                    _renderHandle.DrawingHandleScreen.DrawTextureRect(FovTexture, pos);
                }

                if (DebugLayers == ClydeDebugLayers.Light)
                {
                    _renderHandle.UseShader(null);
                    _renderHandle.DrawingHandleScreen.SetTransform(Matrix3.Identity);
                    _renderHandle.DrawingHandleScreen.DrawTextureRect(
                        viewport.WallBleedIntermediateRenderTarget2.Texture,
                        UIBox2.FromDimensions(Vector2.Zero, ScreenSize), new Color(1, 1, 1, 0.5f));
                }

                RenderOverlays(viewport, OverlaySpace.WorldSpace);
                FlushRenderQueue();
            }

            PopRenderStateFull(state);
            _updateUniformConstants(oldVp?.Size ?? _framebufferSize);

            SetScissorFull(oldScissor);
            _currentMatrixModel = oldTransform;
            _currentViewport = oldVp;
        }

        private sealed class OverlayComparer : IComparer<Overlay>
        {
            public static readonly OverlayComparer Instance = new();

            public int Compare(Overlay? x, Overlay? y)
            {
                var zX = x?.ZIndex ?? 0;
                var zY = y?.ZIndex ?? 0;
                return zX.CompareTo(zY);
            }
        }
    }
}
