using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Graphics.Clyde
{
    // "HLR" stands for "high level rendering" here.
    // The left side of my monitor only has so much space, OK?
    // The idea is this shouldn't contain too much GL specific stuff.
    internal partial class Clyde
    {
        public ClydeDebugLayers DebugLayers { get; set; }

        private readonly RefList<(SpriteComponent sprite, Vector2 worldPos, Angle worldRotation, Box2 spriteScreenBB)>
            _drawingSpriteList
                =
                new();

        // TODO allow this scale to be passed with PostShader as variable
        /// <summary>
        ///     Some shaders that enlarge the final sprite, like emission or highlight effects, need to use a slightly larger render target.
        /// </summary>
        public static float PostShadeScale = 1.25f;

        public void Render()
        {
            CheckTransferringScreenshots();

            var allMinimized = true;
            foreach (var windowReg in _windows)
            {
                if (!windowReg.IsMinimized)
                {
                    allMinimized = false;
                    break;
                }
            }

            var size = ScreenSize;
            if (size.X == 0 || size.Y == 0 || allMinimized)
            {
                ClearFramebuffer(Color.Black);

                // Sleep to avoid turning the computer into a heater.
                Thread.Sleep(16);
                return;
            }

            // Completely flush renderer state back to 0.
            // This should make the renderer more robust
            // in case an exception got thrown during rendering of the previous frame.
            ClearRenderState();

            _debugStats.Reset();

            // Basic pre-render busywork.

            // Update shared UBOs.
            _updateUniformConstants(_mainWindow!.FramebufferSize);

            {
                CalcScreenMatrices(ScreenSize, out var proj, out var view);
                SetProjViewFull(proj, view);
            }

            // Short path to render only the splash.
            if (_drawingSplash)
            {
                DrawSplash(_renderHandle);
                FlushRenderQueue();
                SwapAllBuffers();
                return;
            }

            foreach (var weak in _viewports.Values)
            {
                if (weak.TryGetTarget(out var viewport) && viewport.AutomaticRender)
                    RenderViewport(viewport);
            }

            // Clear screen to correct color.
            ClearFramebuffer(_userInterfaceManager.GetMainClearColor());

            using (DebugGroup("UI"))
            {
                _userInterfaceManager.Render(_renderHandle);
                FlushRenderQueue();
            }

            TakeScreenshot(ScreenshotType.Final);

            // And finally, swap those buffers!
            SwapAllBuffers();
        }

        private void RenderOverlays(Viewport vp, OverlaySpace space, in Box2 worldBox, in Box2Rotated worldBounds)
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

                    overlay.ClydeRender(_renderHandle, space, null, vp, new UIBox2i((0, 0), vp.Size), worldBox, worldBounds);
                }

                FlushRenderQueue();
            }
        }

        private void RenderOverlaysDirect(
            Viewport vp,
            IViewportControl vpControl,
            DrawingHandleBase handle,
            OverlaySpace space,
            in UIBox2i bounds)
        {
            var list = GetOverlaysForSpace(space);

            var worldAABB = CalcWorldAABB(vp);
            var worldBounds = CalcWorldBounds(vp);
            var args = new OverlayDrawArgs(space, vpControl, vp, handle, bounds, vp.Eye!.Position.MapId, worldAABB, worldBounds);

            foreach (var overlay in list)
            {
                overlay.Draw(args);
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
                if (lastFrameSize != texture.Size)
                {
                    GL.BindTexture(TextureTarget.Texture2D, screenBufferHandle.Handle);
                    GL.TexImage2D(TextureTarget.Texture2D, 0,
                        _hasGLSrgb ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Rgba8, texture.Size.X,
                        texture.Size.Y, 0,
                        PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                }

                lastFrameSize = texture.Size;
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


        private void DrawEntities(Viewport viewport, Box2Rotated worldBounds, Box2 worldAABB, IEye eye)
        {
            var mapId = eye.Position.MapId;
            if (mapId == MapId.Nullspace || !_mapManager.HasMapEntity(mapId))
            {
                return;
            }

            RenderOverlays(viewport, OverlaySpace.WorldSpaceBelowEntities, worldAABB, worldBounds);

            var screenSize = viewport.Size;

            ProcessSpriteEntities(mapId, viewport, eye, worldBounds, _drawingSpriteList);

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

                        overlay.ClydeRender(
                            _renderHandle,
                            OverlaySpace.WorldSpace,
                            null,
                            viewport,
                            new UIBox2i((0, 0), viewport.Size),
                            worldAABB,
                            worldBounds);
                        overlayIndex = j;
                        continue;
                    }

                    break;
                }

                RenderTexture? entityPostRenderTarget = null;
                Vector2i roundedPos = default;
                if (entry.sprite.PostShader != null)
                {
                    // get the size of the sprite on screen, scaled slightly to allow for shaders that increase the final sprite size.
                    var screenSpriteSize = (Vector2i) (entry.spriteScreenBB.Size * PostShadeScale).Rounded();

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
                        roundedPos = (Vector2i) entry.spriteScreenBB.Center;
                        var flippedPos = new Vector2i(roundedPos.X, screenSize.Y - roundedPos.Y);
                        flippedPos -= entityPostRenderTarget.Size / 2;
                        _renderHandle.Viewport(Box2i.FromDimensions(-flippedPos, screenSize));
                    }
                }

                entry.sprite.Render(_renderHandle.DrawingHandleWorld, eye.Rotation, in entry.worldRotation, in entry.worldPos);

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

                    // TODO: cache this properly across frames.
                    entityPostRenderTarget.DisposeDeferred();
                }
            }

            ArrayPool<int>.Shared.Return(indexList);

            _drawingSpriteList.Clear();
            FlushRenderQueue();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ProcessSpriteEntities(MapId map, Viewport view, IEye eye, Box2Rotated worldBounds,
            RefList<(SpriteComponent sprite, Vector2 worldPos, Angle worldRot, Box2 spriteScreenBB)> list)
        {
            var xforms = _entityManager.GetEntityQuery<TransformComponent>();

            // Construct a matrix equivalent for Viewport.WorldToLocal()
            eye.GetViewMatrix(out var viewMatrix, view.RenderScale);
            var uiProjmatrix = Matrix3.Identity;
            uiProjmatrix.R0C0 = EyeManager.PixelsPerMeter;
            uiProjmatrix.R1C1 = -EyeManager.PixelsPerMeter;
            uiProjmatrix.R0C2 = view.Size.X / 2f;
            uiProjmatrix.R1C2 = view.Size.Y / 2f;
            var worldToLocal = viewMatrix * uiProjmatrix;

            foreach (var comp in _entitySystemManager.GetEntitySystem<RenderingTreeSystem>().GetRenderTrees(map, worldBounds))
            {
                var bounds = xforms.GetComponent(comp.Owner).InvWorldMatrix.TransformBox(worldBounds);

                comp.SpriteTree.QueryAabb(ref list, (
                    ref RefList<(SpriteComponent sprite, Vector2 worldPos, Angle worldRot, Box2 spriteScreenBB)> state,
                    in SpriteComponent value) =>
                {
                    var entity = value.Owner;
                    var transform = xforms.GetComponent(entity);

                    ref var entry = ref state.AllocAdd();
                    entry.sprite = value;
                    (entry.worldPos, entry.worldRot) = transform.GetWorldPositionRotation();

                    var spriteWorldBB = value.CalculateRotatedBoundingBox(entry.worldPos, entry.worldRot, eye);
                    entry.spriteScreenBB = worldToLocal.TransformBox(spriteWorldBB);
                    return true;

                }, bounds, true);
            }
        }

        private void DrawSplash(IRenderHandle handle)
        {
            // Clear screen to black for splash.
            ClearFramebuffer(Color.Black);

            var splashTex = _cfg.GetCVar(CVars.DisplaySplashLogo);
            if (string.IsNullOrEmpty(splashTex))
                return;

            var texture = _resourceCache.GetResource<TextureResource>(splashTex).Texture;

            handle.DrawingHandleScreen.DrawTexture(texture, (ScreenSize - texture.Size) / 2);
        }

        private void RenderInRenderTarget(RenderTargetBase rt, Action a, Color? clearColor=default)
        {
            // TODO: for the love of god all this state pushing/popping needs to be cleaned up.

            var oldTransform = _currentMatrixModel;
            var oldScissor = _currentScissorState;

            // Have to flush the render queue so that all commands finish rendering to the previous framebuffer.
            FlushRenderQueue();

            var state = PushRenderStateFull();

            {
                BindRenderTargetFull(RtToLoaded(rt));
                if (clearColor is not null)
                    ClearFramebuffer(clearColor.Value);
                SetViewportImmediate(Box2i.FromDimensions(Vector2i.Zero, rt.Size));
                _updateUniformConstants(rt.Size);
                CalcScreenMatrices(rt.Size, out var proj, out var view);
                SetProjViewFull(proj, view);

                // Smugleaf moment
                a();

                FlushRenderQueue();
            }

            FenceRenderTarget(rt);

            PopRenderStateFull(state);
            _updateUniformConstants(_currentRenderTarget.Size);

            SetScissorFull(oldScissor);
            _currentMatrixModel = oldTransform;
        }

        private void RenderViewport(Viewport viewport)
        {
            if (viewport.Eye == null || viewport.Eye.Position.MapId == MapId.Nullspace)
            {
                return;
            }

            RenderInRenderTarget(viewport.RenderTarget, () =>
            {
                using var _ = DebugGroup($"Viewport: {viewport.Name}");

                var oldVp = _currentViewport;

                _currentViewport = viewport;
                var eye = viewport.Eye;

                // Actual code that isn't just pushing/popping renderer state so we can return safely.

                CalcWorldMatrices(viewport.RenderTarget.Size, viewport.RenderScale, eye, out var proj, out var view);
                SetProjViewFull(proj, view);

                // Calculate world-space AABB for camera, to cull off-screen things.
                var worldAABB = CalcWorldAABB(viewport);
                var worldBounds = CalcWorldBounds(viewport);

                if (_eyeManager.CurrentMap != MapId.Nullspace)
                {
                    using (DebugGroup("Lights"))
                    {
                        DrawLightsAndFov(viewport, worldBounds, worldAABB, eye);
                    }

                    RenderOverlays(viewport, OverlaySpace.WorldSpaceBelowWorld, worldAABB, worldBounds);

                    using (DebugGroup("Grids"))
                    {
                        _drawGrids(viewport, worldBounds, eye);
                    }

                    // We will also render worldspace overlays here so we can do them under / above entities as necessary
                    using (DebugGroup("Entities"))
                    {
                        DrawEntities(viewport, worldBounds, worldAABB, eye);
                    }

                    RenderOverlays(viewport, OverlaySpace.WorldSpaceBelowFOV, worldAABB, worldBounds);

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
                    var pos = UIBox2.FromDimensions(viewport.Size / 2 - (200, 200), (400, 400));
                    _renderHandle.DrawingHandleScreen.DrawTextureRect(FovTexture, pos);
                }

                if (DebugLayers == ClydeDebugLayers.Light)
                {
                    _renderHandle.UseShader(null);
                    _renderHandle.DrawingHandleScreen.SetTransform(Matrix3.Identity);
                    _renderHandle.DrawingHandleScreen.DrawTextureRect(
                        viewport.WallBleedIntermediateRenderTarget2.Texture,
                        UIBox2.FromDimensions(Vector2.Zero, viewport.Size), new Color(1, 1, 1, 0.5f));
                }

                RenderOverlays(viewport, OverlaySpace.WorldSpace, worldAABB, worldBounds);

                _currentViewport = oldVp;
            }, viewport.ClearColor);
        }

        private static Box2 CalcWorldAABB(Viewport viewport)
        {
            var eye = viewport.Eye;
            if (eye == null)
                return default;

            // Will be larger than the actual viewport due to rotation.
            return CalcWorldBounds(viewport).CalcBoundingBox();
        }

        private static Box2 GetAABB(IEye eye, Viewport viewport)
        {
            return Box2.CenteredAround(eye.Position.Position + eye.Offset,
                viewport.Size / viewport.RenderScale / EyeManager.PixelsPerMeter * eye.Zoom);
        }

        private static Box2Rotated CalcWorldBounds(Viewport viewport)
        {
            var eye = viewport.Eye;
            if (eye == null)
                return default;

            var rotation = -eye.Rotation;
            var aabb = GetAABB(eye, viewport);

            return new Box2Rotated(aabb, rotation, aabb.Center);
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
