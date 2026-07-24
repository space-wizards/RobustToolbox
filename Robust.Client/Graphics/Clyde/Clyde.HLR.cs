using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Profiling;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    // "HLR" stands for "high level rendering" here.
    // The left side of my monitor only has so much space, OK?
    // The idea is this shouldn't contain too much GL specific stuff.
    internal partial class Clyde
    {
        public ClydeDebugLayers DebugLayers { get; set; }

        // TODO allow this scale to be passed with PostShader as variable
        /// <summary>
        ///     Some shaders that enlarge the final sprite, like emission or highlight effects, need to use a slightly larger render target.
        /// </summary>
        public static float PostShadeScale = 1.25f;

        private List<Overlay> _overlays = new();

        public void Render()
        {
            _entityPostRenderTargetFrame++;

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
                DisposeUnusedEntityPostRenderTargets();

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
            if (_drawingLoadingScreen)
            {
                DrawLoadingScreen(_renderHandle);
                FlushRenderQueue();
                SwapAllBuffers();
                DisposeUnusedEntityPostRenderTargets();
                return;
            }

            foreach (var weak in _viewports.Values)
            {
                if (weak.TryGetTarget(out var viewport) && viewport.AutomaticRender)
                    RenderViewport(viewport);
            }

            // Clear screen to correct color.
            ClearFramebuffer(ConvertClearFromSrgb(_userInterfaceManager.GetMainClearColor()));

            using (DebugGroup("UI"))
            using (_prof.Group("UI"))
            {
                _userInterfaceManager.Render(_renderHandle);
                FlushRenderQueue();
            }

            TakeScreenshot(ScreenshotType.Final);

            using (_prof.Group("Swap buffers"))
            {
                // And finally, swap those buffers!
                SwapAllBuffers();
            }

            DisposeUnusedEntityPostRenderTargets();

            using (_prof.Group("Stats"))
            {
                _prof.WriteValue("GL Draw Calls", ProfData.Int32(_debugStats.LastGLDrawCalls));
                _prof.WriteValue("Clyde Draw Calls", ProfData.Int32(_debugStats.LastClydeDrawCalls));
                _prof.WriteValue("Batches", ProfData.Int32(_debugStats.LastBatches));
                _prof.WriteValue("Max Batch Verts", ProfData.Int32(_debugStats.LargestBatchVertices));
                _prof.WriteValue("Max Batch Idxes", ProfData.Int32(_debugStats.LargestBatchIndices));
                _prof.WriteValue("Lights", ProfData.Int32(_debugStats.TotalLights));
                _prof.WriteValue("Shadow Lights", ProfData.Int32(_debugStats.ShadowLights));
                _prof.WriteValue("Occluders", ProfData.Int32(_debugStats.Occluders));
            }
        }

        public void RenderNow(IRenderTarget renderTarget, Action<IRenderHandle> callback)
        {
            ClearRenderState();

            _renderHandle.RenderInRenderTarget(
                renderTarget,
                () =>
                {
                    callback(_renderHandle);
                },
                null);
        }

        private void RenderSingleWorldOverlay(Overlay overlay, Viewport vp, OverlaySpace space, in Box2 worldBox, in Box2Rotated worldBounds)
        {
            // Check that entity manager has started.
            // This is required for us to be able to use MapSystem.
            DebugTools.Assert(_entityManager.Started, "Entity manager should be started/initialized before rendering world-space overlays");

            DebugTools.Assert(space != OverlaySpace.ScreenSpaceBelowWorld && space != OverlaySpace.ScreenSpace);

            var mapId = vp.Eye?.Position.MapId ?? MapId.Nullspace;
            var args = new OverlayDrawArgs(space, null, vp, _renderHandle, new UIBox2i((0, 0), vp.Size), _mapSystem.GetMapOrInvalid(mapId), mapId, worldBox, worldBounds);

            if (!overlay.BeforeDraw(args))
                return;

            if (overlay.RequestScreenTexture)
            {
                FlushRenderQueue();
                overlay.ScreenTexture = CopyScreenTexture(vp.RenderTarget);
            }

            if (overlay.OverwriteTargetFrameBuffer)
                ClearFramebuffer(default);

            try
            {
                overlay.Draw(args);
            }
            catch (Exception e)
            {
                _logManager.GetSawmill("clyde.overlay")
                    .Error($"Caught exception while drawing overlay {overlay.GetType()}. Exception: {e}");
            }
            finally
            {
                // cleanup state so shaders/transforms dont leak into future overlays
                // many overlays already do cleanup manually, but ideally they don't have to at all
                _renderHandle.SetModelTransform(Matrix3x2.Identity);
                _renderHandle.UseShader(null);
            }
        }

        private void RenderOverlays(Viewport vp, OverlaySpace space, in Box2 worldBox, in Box2Rotated worldBounds)
        {
            DebugTools.Assert(space != OverlaySpace.ScreenSpaceBelowWorld && space != OverlaySpace.ScreenSpace);
            using (DebugGroup($"Overlays: {space}"))
            {
                foreach (var overlay in GetOverlaysForSpace(space))
                {
                    RenderSingleWorldOverlay(overlay, vp, space, worldBox, worldBounds);
                }

                FlushRenderQueue();
            }
        }

        private void RenderOverlaysDirect(
            Viewport vp,
            IViewportControl vpControl,
            IRenderHandle handle,
            OverlaySpace space,
            in UIBox2i bounds)
        {
            using var _ = _prof.Group($"Overlays SS {space}");

            var list = GetOverlaysForSpace(space);

            var worldBounds = CalcWorldBounds(vp);
            var worldAABB = worldBounds.CalcBoundingBox();
            var mapId = vp.Eye?.Position.MapId ?? MapId.Nullspace;
            var mapUid = EntityUid.Invalid;

            // Screen space overlays may be getting used before entity manager & entity systems have been initialized.
            // This might mean that _mapSystem is currently null.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_entityManager.Started && _mapSystem != null)
                mapUid = _mapSystem.GetMapOrInvalid(mapId);

            DebugTools.Assert(_mapSystem != null || !_entityManager.Started);

            var args = new OverlayDrawArgs(space, vpControl, vp, handle, bounds, mapUid, mapId, worldAABB, worldBounds);

            foreach (var overlay in list)
            {
                try
                {
                    if (!overlay.BeforeDraw(args))
                        continue;

                    if (overlay.RequestScreenTexture)
                    {
                        FlushRenderQueue();
                        overlay.ScreenTexture = CopyScreenTexture(vp.RenderTarget);
                    }

                    if (overlay.OverwriteTargetFrameBuffer)
                        ClearFramebuffer(default);

                    overlay.Draw(args);
                }
                catch (Exception e)
                {
                    _logManager.GetSawmill("clyde.overlay")
                        .Error($"Caught exception while drawing overlay {overlay.GetType()}. Exception: {e}");
                }
                finally
                {
                    // cleanup state so shaders/transforms dont leak into future overlays
                    // many overlays already do cleanup manually, but ideally they don't have to at all
                    // screen and world handles are backed by the same renderhandle, so we only need to do this for one
                    handle.DrawingHandleScreen.SetTransform(Matrix3x2.Identity);
                    handle.DrawingHandleScreen.UseShader(null);
                }
            }
        }

        private List<Overlay> GetOverlaysForSpace(OverlaySpace space)
        {
            _overlays.Clear();

            foreach (var overlay in _overlayManager.AllOverlays)
            {
                if ((overlay.Space & space) != 0)
                {
                    _overlays.Add(overlay);
                }
            }

            return _overlays;
        }

        private ClydeTexture? ScreenBufferTexture;
        private GLHandle screenBufferHandle;
        private Vector2 lastFrameSize;
        // Entity post-shader targets are keyed by exact size so sprite-local UV/TEXTURE_PIXEL_SIZE
        // semantics match the old per-sprite allocation path.
        // If we broke all existing shaders you could just clip the bounds but this is easier for callers.
        private readonly Dictionary<EntityPostRenderTargetKey, CachedEntityPostRenderTarget> _entityPostRenderTargets = new();
        private readonly List<EntityPostRenderTargetKey> _staleEntityPostRenderTargets = new();
        private int _entityPostRenderTargetFrame;

        private readonly record struct EntityPostRenderTargetKey(Vector2i Size, bool Stencil);

        private sealed class CachedEntityPostRenderTarget
        {
            public readonly RenderTexture Texture;
            public int LastUsedFrame;

            public CachedEntityPostRenderTarget(RenderTexture texture)
            {
                Texture = texture;
            }
        }

        /// <summary>
        ///    Sends SCREEN_TEXTURE to all overlays in the given OverlaySpace that request it.
        /// </summary>
        private Texture? CopyScreenTexture(RenderTexture texture)
        {
            //This currently does NOT consider viewports and just grabs the current screen framebuffer. This will need to be improved upon in the future.

            if (ScreenBufferTexture == null)
                return null;

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
            return ScreenBufferTexture;
        }

        private void DrawEntities(Viewport viewport, Box2Rotated worldBounds, Box2 worldAABB, IEye eye)
        {
            var mapId = eye.Position.MapId;
            if (mapId == MapId.Nullspace)
                return;

            RenderOverlays(viewport, OverlaySpace.WorldSpaceBelowEntities, worldAABB, worldBounds);
            var worldOverlays = GetOverlaysForSpace(OverlaySpace.WorldSpaceEntities);

            var spriteSystem = _entityManager.System<SpriteSystem>();
            int[] indexList;
            using (_prof.Group("Gather Sprites"))
            {
                GetSprites(mapId, viewport, eye, worldBounds, out indexList);
            }

            var screenSize = viewport.Size;
            var overlayIndex = 0;

            bool flushed = false;
            using var _drawZone = _prof.Group("Draw");
            for (var i = 0; i < _drawingSpriteList.Count; i++)
            {
                ref var entry = ref _drawingSpriteList[indexList[i]];
                var postShaders = spriteSystem.GetPostShaders(entry.Sprite);
                RenderTexture? entityPostRenderTarget = null;
                RenderTexture? entityPostRenderTarget2 = null;

                for (; overlayIndex < worldOverlays.Count; overlayIndex++)
                {
                    var overlay = worldOverlays[overlayIndex];

                    if (overlay.ZIndex > entry.Sprite.DrawDepth)
                    {
                        flushed = false;
                        break;
                    }

                    if (!flushed)
                    {
                        FlushRenderQueue();
                        flushed = true;
                    }

                    RenderSingleWorldOverlay(overlay, viewport, OverlaySpace.WorldSpaceEntities, worldAABB, worldBounds);
                }

                Vector2i roundedPos = default;
                if (postShaders.Count != 0)
                {
                    bool exit = false;
                    if (PostShadersNeedScreenTexture(postShaders))
                    {
                        FlushRenderQueue();
                        var tex = CopyScreenTexture(viewport.RenderTarget);
                        if (tex == null)
                        {
                            exit = true;
                        }
                        else
                        {
                            foreach (var postShader in postShaders)
                            {
                                if (postShader.GetScreenTexture)
                                    postShader.Shader.SetParameter("SCREEN_TEXTURE", tex);
                            }
                        }
                    }

                    // check that sprite size is valid
                    if (!exit)
                    {
                        var entityPostRenderTargetSize = GetPostShaderTargetSize(entry.SpriteScreenBB);
                        entityPostRenderTarget = GetEntityPostRenderTarget(entityPostRenderTargetSize, true);
                        entityPostRenderTarget2 = postShaders.Count > 1
                            ? GetEntityPostRenderTarget(entityPostRenderTargetSize, false)
                            : null;

                        if (entityPostRenderTarget != null)
                        {
                            _renderHandle.UseRenderTarget(entityPostRenderTarget);
                            _renderHandle.Clear(default, 0, ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit);

                            // Calculate viewport so that the entity thinks it's drawing to the same position,
                            // which is necessary for light application,
                            // but it's ACTUALLY drawing into the center of the render target.
                            roundedPos = (Vector2i) entry.SpriteScreenBB.Center;
                            var flippedPos = new Vector2i(roundedPos.X, screenSize.Y - roundedPos.Y);
                            flippedPos -= entityPostRenderTarget.Size / 2;
                            _renderHandle.Viewport(Box2i.FromDimensions(-flippedPos, screenSize));

                            if (PostShadersRaiseEvent(postShaders))
                            {
                                _entityManager.EventBus.RaiseLocalEvent(entry.Uid,
                                    new BeforePostShaderRenderEvent(entry.Sprite, viewport),
                                    false);

                                postShaders = spriteSystem.GetPostShaders(entry.Sprite);
                                if (postShaders.Count > 1)
                                {
                                    entityPostRenderTarget2 ??= GetEntityPostRenderTarget(entityPostRenderTarget.Size, false);
                                }
                            }
                        }
                    }
                }

                spriteSystem.RenderSprite(new(entry.Uid, entry.Sprite), _renderHandle.DrawingHandleWorld, eye.Rotation, entry.WorldRot, entry.WorldPos);

                if (postShaders.Count != 0 && entityPostRenderTarget != null)
                {
                    var oldProj = _currentMatrixProj;
                    var oldView = _currentMatrixView;

                    DrawEntityPostShaders(viewport, screenSize, roundedPos, entityPostRenderTarget, entityPostRenderTarget2, postShaders);

                    _renderHandle.SetProjView(oldProj, oldView);
                    _renderHandle.UseShader(null);
                }
            }

            // draw remainder of overlays
            for (; overlayIndex < worldOverlays.Count; overlayIndex++)
            {
                if (!flushed)
                {
                    FlushRenderQueue();
                    flushed = true;
                }

                RenderSingleWorldOverlay(worldOverlays[overlayIndex], viewport, OverlaySpace.WorldSpaceEntities, worldAABB, worldBounds);
            }

            ArrayPool<int>.Shared.Return(indexList);

            _debugStats.Entities += _drawingSpriteList.Count;
            _drawingSpriteList.Clear();
            FlushRenderQueue();
        }

        private static Vector2i GetPostShaderTargetSize(Box2 spriteScreenBox)
        {
            // get the size of the sprite on screen, scaled slightly to allow for shaders that increase the final sprite size.
            var screenSpriteSize = (Vector2i)(spriteScreenBox.Size * PostShadeScale).Rounded();

            // I'm not 100% sure why it works, but without it post-shader
            // can be lower or upper by 1px than original sprite depending on sprite rotation or scale
            // probably some rotation rounding error
            if (screenSpriteSize.X % 2 != 0)
                screenSpriteSize.X++;
            if (screenSpriteSize.Y % 2 != 0)
                screenSpriteSize.Y++;

            return screenSpriteSize;
        }

        private RenderTexture? GetEntityPostRenderTarget(Vector2i size, bool stencil)
        {
            if (size.X <= 0 || size.Y <= 0)
                return null;

            var key = new EntityPostRenderTargetKey(size, stencil);
            if (!_entityPostRenderTargets.TryGetValue(key, out var cached))
            {
                cached = new CachedEntityPostRenderTarget(CreateEntityPostRenderTarget(
                    size,
                    new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, stencil),
                    stencil ? "entity-post-render-target" : "entity-post-render-target-ping-pong"));
                _entityPostRenderTargets.Add(key, cached);
            }

            cached.LastUsedFrame = _entityPostRenderTargetFrame;
            return cached.Texture;
        }

        private void DisposeUnusedEntityPostRenderTargets()
        {
            foreach (var (key, cached) in _entityPostRenderTargets)
            {
                if (cached.LastUsedFrame != _entityPostRenderTargetFrame)
                    _staleEntityPostRenderTargets.Add(key);
            }

            foreach (var key in _staleEntityPostRenderTargets)
            {
                _entityPostRenderTargets[key].Texture.DisposeDeferred();
                _entityPostRenderTargets.Remove(key);
            }

            _staleEntityPostRenderTargets.Clear();
        }

        private RenderTexture CreateEntityPostRenderTarget(Vector2i size, RenderTargetFormatParameters format, string name)
        {
            return CreateRenderTarget(size,
                format,
                name: name);
        }

        private static bool PostShadersNeedScreenTexture(IReadOnlyList<SpriteComponent.PostShaderEntry> postShaders)
        {
            foreach (var postShader in postShaders)
            {
                if (postShader.GetScreenTexture)
                    return true;
            }

            return false;
        }

        private static bool PostShadersRaiseEvent(IReadOnlyList<SpriteComponent.PostShaderEntry> postShaders)
        {
            foreach (var postShader in postShaders)
            {
                if (postShader.RaiseShaderEvent)
                    return true;
            }

            return false;
        }

        private void DrawEntityPostShaders(
            Viewport viewport,
            Vector2i screenSize,
            Vector2i roundedPos,
            RenderTexture entityPostRenderTarget,
            RenderTexture? entityPostRenderTarget2,
            IReadOnlyList<SpriteComponent.PostShaderEntry> postShaders)
        {
            var source = entityPostRenderTarget;

            for (var i = 0; i < postShaders.Count; i++)
            {
                var finalPass = i == postShaders.Count - 1;
                var shader = postShaders[i].Shader;

                if (finalPass)
                {
                    _renderHandle.UseRenderTarget(viewport.RenderTarget);
                    _renderHandle.Viewport(Box2i.FromDimensions(Vector2i.Zero, screenSize));
                    // The sprite has already been drawn into a transparent RT, so its color is already multiplied
                    // by alpha. Use premultiplied blending for the final viewport composite to avoid applying
                    // translucent sprite alpha twice. Stuff like an interaction outline would make the entire sprite double transparent.
                    _renderHandle.UseShader(GetPremultipliedBlendShaderInstance(shader));
                    CalcScreenMatrices(viewport.Size, out var finalProj, out var finalView);
                    _renderHandle.SetProjView(finalProj, finalView);
                    _renderHandle.SetModelTransform(Matrix3x2.Identity);

                    var rounded = roundedPos - source.Size / 2;
                    var finalBox = Box2i.FromDimensions(rounded, source.Size);

                    _renderHandle.DrawTextureScreen(source.Texture,
                        finalBox.BottomLeft, finalBox.BottomRight, finalBox.TopLeft, finalBox.TopRight,
                        Color.White, null);
                    continue;
                }

                DebugTools.AssertNotNull(entityPostRenderTarget2);
                var destination = source == entityPostRenderTarget ? entityPostRenderTarget2! : entityPostRenderTarget;

                _renderHandle.UseRenderTarget(destination);
                _renderHandle.Clear(default, 0, ClearBufferMask.ColorBufferBit);
                _renderHandle.Viewport(Box2i.FromDimensions(Vector2i.Zero, destination.Size));
                // Intermediate post-shader passes are texture transforms. Write the
                // shader output directly so stuff like alpha-cut passes do not get alpha-applied before the final viewport draw.
                // The easiest way to know if this happens is your sprite getting darker / changing from the multiple passes when it shouldn't be.
                _renderHandle.UseShader(GetNoBlendShaderInstance(shader));
                CalcScreenMatrices(destination.Size, out var intermediateProj, out var intermediateView);
                _renderHandle.SetProjView(intermediateProj, intermediateView);
                _renderHandle.SetModelTransform(Matrix3x2.Identity);

                var intermediateBox = Box2i.FromDimensions(Vector2i.Zero, source.Size);
                _renderHandle.DrawTextureScreen(source.Texture,
                    intermediateBox.BottomLeft, intermediateBox.BottomRight, intermediateBox.TopLeft, intermediateBox.TopRight,
                    Color.White, null);

                source = destination;
            }
        }

        private void DrawLoadingScreen(IRenderHandle handle)
        {
            ClearFramebuffer(Color.Black);

            _loadingScreenManager.DrawLoadingScreen(handle, ScreenSize);
        }

        private void RenderInRenderTarget(RenderTargetBase rt, Action a, Color? clearColor=default)
        {
            // TODO: for the love of god all this state pushing/popping needs to be cleaned up.

            var oldTransform = _currentMatrixModel;
            var oldScissor = _currentScissorState;
            var oldMatrixProj = _currentMatrixProj;
            var oldMatrixView = _currentMatrixView;
            var oldBoundTarget = _currentBoundRenderTarget;
            var oldRenderTarget = _currentRenderTarget;
            var oldShader = _queuedShaderInstance;
            var oldCaps = _glCaps;

            // Need to get state before flushing render queue in case they modify the original state.
            var state = PushRenderStateFull();

            // Have to flush the render queue so that all commands finish rendering to the previous framebuffer.
            FlushRenderQueue();

            {
                BindRenderTargetFull(RtToLoaded(rt));
                if (clearColor is not null)
                    ClearFramebuffer(ConvertClearFromSrgb(clearColor.Value));

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

            _currentMatrixModel = oldTransform;

            DebugTools.Assert(oldCaps.Equals(_glCaps));
            DebugTools.Assert(_currentMatrixModel.Equals(oldTransform));
            DebugTools.Assert(_currentScissorState.Equals(oldScissor));
            DebugTools.Assert(_currentMatrixProj.Equals(oldMatrixProj));
            DebugTools.Assert(oldMatrixView.Equals(_currentMatrixView));
            DebugTools.Assert(oldRenderTarget.Equals(_currentRenderTarget));
            DebugTools.Assert(oldBoundTarget.Equals(_currentBoundRenderTarget));
            DebugTools.Assert(oldShader.Equals(_queuedShaderInstance));
        }

        private void RenderViewport(Viewport viewport)
        {
            if (viewport.Eye == null || viewport.Eye.Position.MapId == MapId.Nullspace)
            {
                if (viewport.ClearWhenMissingEye)
                    RenderInRenderTarget(viewport.RenderTarget, () => { }, viewport.ClearColor);

                return;
            }

            using var _ = _prof.Group("Viewport");

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
                var worldBounds = CalcWorldBounds(viewport);
                var worldAABB = worldBounds.CalcBoundingBox();

                if (eye.Position.MapId != MapId.Nullspace)
                {
                    using (DebugGroup("Lights"))
                    using (_prof.Group("Lights"))
                    {
                        DrawLightsAndFov(viewport, worldBounds, worldAABB, eye);
                    }

                    using (_prof.Group("Overlays WSBW"))
                    {
                        RenderOverlays(viewport, OverlaySpace.WorldSpaceBelowWorld, worldAABB, worldBounds);
                    }

                    using (DebugGroup("Grids"))
                    using (_prof.Group("Grids"))
                    {
                        _drawGrids(viewport, worldAABB, worldBounds, eye);
                    }

                    // We will also render worldspace overlays here so we can do them under / above entities as necessary
                    using (DebugGroup("Entities"))
                    using (_prof.Group("Entities"))
                    {
                        DrawEntities(viewport, worldBounds, worldAABB, eye);
                    }

                    using (_prof.Group("Overlays WSBFOV"))
                    {
                        RenderOverlays(viewport, OverlaySpace.WorldSpaceBelowFOV, worldAABB, worldBounds);
                    }

                    if (_lightManager.Enabled && _lightManager.DrawHardFov && eye.DrawLight && eye.DrawFov)
                    {
                        var mapUid = _mapSystem.GetMap(eye.Position.MapId);
                        if (_entityManager.GetComponent<MapComponent>(mapUid).LightingEnabled)
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
                    _renderHandle.DrawingHandleScreen.SetTransform(Matrix3x2.Identity);
                    var pos = UIBox2.FromDimensions(viewport.Size / 2 - new Vector2(200, 200), new Vector2(400, 400));
                    _renderHandle.DrawingHandleScreen.DrawTextureRect(FovTexture, pos);
                }

                if (DebugLayers == ClydeDebugLayers.Light)
                {
                    _renderHandle.UseShader(null);
                    _renderHandle.DrawingHandleScreen.SetTransform(Matrix3x2.Identity);
                    _renderHandle.DrawingHandleScreen.DrawTextureRect(
                        viewport.WallBleedIntermediateRenderTarget2.Texture,
                        UIBox2.FromDimensions(Vector2.Zero, viewport.Size), new Color(1, 1, 1, 0.5f));
                }

                if (eye.Position.MapId != MapId.Nullspace)
                {
                    using (_prof.Group("Overlays WS"))
                    {
                        RenderOverlays(viewport, OverlaySpace.WorldSpace, worldAABB, worldBounds);
                    }
                }

                _currentViewport = oldVp;
            }, viewport.ClearColor);
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
    }
}
