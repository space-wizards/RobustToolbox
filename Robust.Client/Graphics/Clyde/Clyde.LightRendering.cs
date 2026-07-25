using System;
using System.Collections.Generic;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.InteropServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using TKStencilOp = OpenToolkit.Graphics.OpenGL4.StencilOp;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Utility;
using TextureWrapMode = Robust.Shared.Graphics.TextureWrapMode;

namespace Robust.Client.Graphics.Clyde
{
    // This file handles everything about light rendering.
    // That includes shadow casting and also FOV.
    // A detailed explanation of how all this works can be found here:
    // https://docs.spacestation14.io/en/engine/lighting-fov

    internal partial class Clyde
    {
        // Horizontal width, in pixels, of the shadow maps used to render regular lights.
        private const int ShadowMapSize = 512;

        private const float SharedOccluderEdgeTolerance = 0.001f;
        private const float SharedOccluderEdgeToleranceSquared = SharedOccluderEdgeTolerance * SharedOccluderEdgeTolerance;
        private const float SharedOccluderNeighbourQueryPadding = 1f + SharedOccluderEdgeTolerance;
        // Horizontal width, in pixels, of the shadow maps used to render FOV.
        // I figured this was more accuracy sensitive than lights so resolution is significantly higher.
        private const int FovMapSize = 2048;

        private ClydeShaderInstance _fovDebugShaderInstance = default!;

        // Various shaders used in the light rendering process.
        // We keep ClydeHandles into the _loadedShaders dict so they can be reloaded.
        // They're all .swsl now.
        private ClydeHandle _lightSoftShaderHandle;
        private ClydeHandle _lightHardShaderHandle;
        private ClydeHandle _fovShaderHandle;
        private ClydeHandle _fovLightShaderHandle;
        private ClydeHandle _wallBleedBlurShaderHandle;
        private ClydeHandle _lightBlurShaderHandle;
        private ClydeHandle _mergeWallLayerShaderHandle;

        // Sampler used to sample the FovTexture with linear filtering, used in the lighting FOV pass
        // (it uses VSM unlike final FOV).
        private GLHandle _fovFilterSampler;

        // Shader program used to calculate depth for shadows/FOV.
        // Sadly not .swsl since it has a different vertex format and such.
        private GLShaderProgram _fovCalculationProgram = default!;

        // Occlusion geometry used to render shadows and FOV.

        // Amount of indices in _occlusionEbo, so how much we have to draw when drawing _occlusionVao.
        private int _occlusionDataLength;

        // Actual GL objects used for rendering.
        private GLBuffer _occlusionVbo = default!;
        private GLBuffer _occlusionVIVbo = default!;
        private GLBuffer _occlusionEbo = default!;
        private GLHandle _occlusionVao;


        // Occlusion mask geometry that represents the area with occluders.
        // This is used to merge _wallBleedIntermediateRenderTarget2 onto _lightRenderTarget after wall bleed is done.

        // Amount of indices in _occlusionMaskEbo, so how much we have to draw when drawing _occlusionMaskVao.
        private int _occlusionMaskDataLength;

        // Actual GL objects used for rendering.
        private GLBuffer _occlusionMaskVbo = default!;
        private GLBuffer _occlusionMaskEbo = default!;
        private GLHandle _occlusionMaskVao;

        // For depth calculation for FOV.
        private RenderTexture _fovRenderTarget = default!;

        // For depth calculation of lighting shadows.
        private RenderTexture _shadowRenderTarget = default!;

        // Used because otherwise a MaxLightsPerScene change callback getting hit on startup causes interesting issues (read: bugs)
        private bool _shadowRenderTargetCanInitializeSafely = false;

        // Proxies to textures of the above render targets.
        private ClydeTexture FovTexture => _fovRenderTarget.Texture;
        private ClydeTexture ShadowTexture => _shadowRenderTarget.Texture;

        private LightRenderData[] _lightsToRenderList = default!;

        private LightCapacityComparer _lightCap = new();
        private ShadowCapacityComparer _shadowCap = new ShadowCapacityComparer();

        // Cached shared occluder edges from ClientOccluderSystem because we have very specific occluder rules.
        private readonly HashSet<OccluderEdgeKey> _occluderSharedBoundaryEdges = new();
        private readonly List<Vector4> _occluderBoundarySegments = new();
        private readonly HashSet<OccluderVertexKey> _occluderVisibleBoundaryVertices = new();
        private readonly HashSet<OccluderVertexKey> _occluderConvexBoundaryVertices = new();
        private readonly Dictionary<OccluderVertexKey, BoundaryVertexDirections> _occluderBoundaryVertexDirections = new();
        private readonly Dictionary<OccluderVertexKey, List<Vector4>> _occluderSharedVertexEdges = new();
        private readonly HashSet<OccluderEdgeKey> _occluderUniqueSharedEdges = new();
        private readonly List<OccluderVertexKey> _occluderStaleSharedVertices = new();
        private readonly List<OccluderRenderEntry> _occluderRenderEntries = new();
        private readonly List<Vector2> _occluderRenderVertices = new();
        private readonly List<Vector4> _occluderRenderEdges = new();
        private readonly List<bool> _occluderRenderSharedEdges = new();

        private float _maxLightRadius;

        private unsafe void InitLighting()
        {
            _cfg.OnValueChanged(CVars.MaxLightRadius, val => { _maxLightRadius = val;}, true);

            // Other...
            LoadLightingShaders();

            {
                // Occlusion VAO.
                // Only handles positions, no other vertex data necessary.
                _occlusionVao = new GLHandle(GenVertexArray());
                BindVertexArray(_occlusionVao.Handle);
                CheckGlError();

                ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, _occlusionVao, nameof(_occlusionVao));

                // aPos
                _occlusionVbo = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                    nameof(_occlusionVbo));
                GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, sizeof(Vector4), IntPtr.Zero);
                GL.EnableVertexAttribArray(0);

                CheckGlError();

                // subVertex
                _occlusionVIVbo = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                    nameof(_occlusionVIVbo));
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.UnsignedByte, true, sizeof(byte) * 2, IntPtr.Zero);
                GL.EnableVertexAttribArray(1);

                // index
                _occlusionEbo = new GLBuffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                    nameof(_occlusionEbo));

                CheckGlError();
            }

            {
                // Occlusion mask VAO.
                // Only handles positions, no other vertex data necessary.

                _occlusionMaskVao = new GLHandle(GenVertexArray());
                BindVertexArray(_occlusionMaskVao.Handle);
                CheckGlError();

                ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, _occlusionMaskVao, nameof(_occlusionMaskVao));

                _occlusionMaskVbo = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                    nameof(_occlusionMaskVbo));

                _occlusionMaskEbo = new GLBuffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                    nameof(_occlusionMaskEbo));

                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(Vector2), IntPtr.Zero);
                GL.EnableVertexAttribArray(0);
                CheckGlError();
            }

            // FOV FBO.
            _fovRenderTarget = CreateRenderTarget((FovMapSize, 2),
                new RenderTargetFormatParameters(
                    _hasGLFloatFramebuffers ? RenderTargetColorFormat.RG32F : RenderTargetColorFormat.Rgba8, true),
                new TextureSampleParameters { WrapMode = TextureWrapMode.Repeat },
                nameof(_fovRenderTarget));

            if (_hasGLSamplerObjects)
            {
                _fovFilterSampler = new GLHandle(GL.GenSampler());
                GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureMagFilter, (int)All.Linear);
                GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureMinFilter, (int)All.Linear);
                GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureWrapS, (int)All.Repeat);
                GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureWrapT, (int)All.Repeat);
                CheckGlError();
            }

            // Shadow FBO.
            _shadowRenderTargetCanInitializeSafely = true;
            MaxShadowcastingLightsChanged(_maxShadowcastingLights);
        }

        private void LoadLightingShaders()
        {
            var depthVert = ReadEmbeddedShader("shadow-depth.vert");
            var depthFrag = ReadEmbeddedShader("shadow-depth.frag");

            (string, uint)[] attribLocations =
            {
                ("aPos", 0),
                ("subVertex", 1)
            };

            _fovCalculationProgram = _compileProgram(depthVert, depthFrag, attribLocations, "Shadow Depth Program");

            var debugShader = _resourceCache.GetResource<ShaderSourceResource>("/Shaders/Internal/depth-debug.swsl");
            _fovDebugShaderInstance = (ClydeShaderInstance)InstanceShader(debugShader);

            ClydeHandle LoadShaderHandle(string path)
            {
                if (_resourceCache.TryGetResource(path, out ShaderSourceResource? resource))
                {
                    return resource.ClydeHandle;
                }

                _clydeSawmill.Warning($"Can't load shader {path}\n");
                return default;
            }

            _lightSoftShaderHandle = LoadShaderHandle("/Shaders/Internal/light-soft.swsl");
            _lightHardShaderHandle = LoadShaderHandle("/Shaders/Internal/light-hard.swsl");
            _fovShaderHandle = LoadShaderHandle("/Shaders/Internal/fov.swsl");
            _fovLightShaderHandle = LoadShaderHandle("/Shaders/Internal/fov-lighting.swsl");
            _wallBleedBlurShaderHandle = LoadShaderHandle("/Shaders/Internal/wall-bleed-blur.swsl");
            _lightBlurShaderHandle = LoadShaderHandle("/Shaders/Internal/light-blur.swsl");
            _mergeWallLayerShaderHandle = LoadShaderHandle("/Shaders/Internal/wall-merge.swsl");
        }

        private void DrawFov(Viewport viewport, IEye eye)
        {
            using var _ = DebugGroup(nameof(DrawFov));
            using var _p = _prof.Group("DrawFov");

            PrepareDepthDraw(RtToLoaded(_fovRenderTarget));

            if (eye.DrawFov)
            {
                // Calculate maximum distance for the projection based on screen size.
                var screenSizeCut = viewport.Size / EyeManager.PixelsPerMeter;
                var maxDist = (float)Math.Max(screenSizeCut.X, screenSizeCut.Y);

                // FOV is rendered twice.
                // Once with back face culling like regular lighting.
                // Then once with front face culling for the final FOV pass (so you see "into" walls).
                GL.CullFace(CullFaceMode.Back);
                CheckGlError();

                DrawOcclusionDepth(eye.Position.Position, _fovRenderTarget.Size.X, maxDist, 0);

                GL.CullFace(CullFaceMode.Front);
                CheckGlError();

                DrawOcclusionDepth(eye.Position.Position, _fovRenderTarget.Size.X, maxDist, 1);
            }

            FinalizeDepthDraw();
        }

        /// <summary>
        ///     Draws depths for lighting & FOV into the currently bound framebuffer.
        /// </summary>
        /// <param name="lightPos">The position of the light source.</param>
        /// <param name="width">The width of the current framebuffer.</param>
        /// <param name="maxDist">The maximum distance of this light.</param>
        /// <param name="viewportY">Y index of the row to render the depth at in the framebuffer.</param>
        private void DrawOcclusionDepth(Vector2 lightPos, int width, float maxDist, int viewportY)
        {
            // The light is now the center of the universe.
            _fovCalculationProgram.SetUniform("shadowLightCentre", lightPos);

            // Shift viewport around so we write to the correct quadrant of the depth map.
            GL.Viewport(0, viewportY, width, 1);
            CheckGlError();

            // Make two draw calls. This allows a faked "generation" of additional polygons.
            _fovCalculationProgram.SetUniform("shadowOverlapSide", 0.0f);
            GL.DrawElements(GetQuadGLPrimitiveType(), _occlusionDataLength, DrawElementsType.UnsignedShort, 0);
            CheckGlError();
            _debugStats.LastGLDrawCalls += 1;
            // Yup, it's the other draw call.
            _fovCalculationProgram.SetUniform("shadowOverlapSide", 1.0f);
            GL.DrawElements(GetQuadGLPrimitiveType(), _occlusionDataLength, DrawElementsType.UnsignedShort, 0);
            CheckGlError();
            _debugStats.LastGLDrawCalls += 1;
        }

        private void PrepareDepthDraw(LoadedRenderTarget target)
        {
            const float arbitraryDistanceMax = 1234;

            IsBlending = false;

            GL.Enable(EnableCap.DepthTest);
            CheckGlError();
            GL.DepthFunc(DepthFunction.Lequal);
            CheckGlError();
            GL.DepthMask(true);
            CheckGlError();

            GL.Enable(EnableCap.CullFace);
            CheckGlError();
            GL.FrontFace(FrontFaceDirection.Cw);
            CheckGlError();

            BindRenderTargetImmediate(target);
            CheckGlError();
            GL.ClearDepth(1);
            CheckGlError();
            if (_hasGLFloatFramebuffers)
            {
                GL.ClearColor(arbitraryDistanceMax, arbitraryDistanceMax * arbitraryDistanceMax, 0, 1);
            }
            else
            {
                GL.ClearColor(1, 1, 1, 1);
            }

            CheckGlError();
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
            CheckGlError();

            BindVertexArray(_occlusionVao.Handle);
            CheckGlError();

            _fovCalculationProgram.Use();

            SetupGlobalUniformsImmediate(_fovCalculationProgram, null);
        }

        private void FinalizeDepthDraw()
        {
            GL.Disable(EnableCap.CullFace);
            CheckGlError();

            GL.DepthMask(false);
            CheckGlError();
            GL.Disable(EnableCap.DepthTest);
            CheckGlError();

            IsBlending = true;
        }

        private void DrawLightsAndFov(Viewport viewport, Box2Rotated worldBounds, Box2 worldAABB, IEye eye)
        {
            if (!_lightManager.Enabled || !eye.DrawLight)
            {
                return;
            }

            var mapId = eye.Position.MapId;
            if (mapId == MapId.Nullspace)
                return;

            // If this map has lighting disabled, return
            var mapUid = _mapSystem.GetMapOrInvalid(mapId);
            if (!_entityManager.TryGetComponent<MapComponent>(mapUid, out var map) || !map.LightingEnabled)
            {
                return;
            }

            int count;
            Box2 expandedBounds;
            using (_prof.Group("LightsToRender"))
            {
                (count, expandedBounds) = GetLightsToRender(mapId, worldBounds, worldAABB);
            }

            UpdateOcclusionGeometry(mapId, expandedBounds, eye.Position.Position);

            DrawFov(viewport, eye);

            if (!_lightManager.DrawLighting)
            {
                BindRenderTargetFull(viewport.RenderTarget);
                GL.Viewport(0, 0, viewport.Size.X, viewport.Size.Y);
                CheckGlError();
                return;
            }

            using (DebugGroup("Draw shadow depth"))
            using (_prof.Group("Draw shadow depth"))
            {
                PrepareDepthDraw(RtToLoaded(_shadowRenderTarget));
                GL.CullFace(CullFaceMode.Back);
                CheckGlError();

                if (_lightManager.DrawShadows)
                {
                    for (var i = 0; i < count; i++)
                    {
                        ref var lightData = ref _lightsToRenderList[i];
                        var light = lightData.Light;

                        if (lightData.ShadowMapIndex < 0) continue;

                        DrawOcclusionDepth(
                            lightData.Position,
                            ShadowMapSize,
                            light.Radius,
                            lightData.ShadowMapIndex);
                    }
                }

                FinalizeDepthDraw();
            }

            IsStencilling = true;

            var (lightW, lightH) = GetLightMapSize(viewport.Size);
            GL.Viewport(0, 0, lightW, lightH);
            CheckGlError();

            BindRenderTargetImmediate(RtToLoaded(viewport.LightRenderTarget));
            DebugTools.Assert(_currentBoundRenderTarget.TextureHandle.Equals(viewport.LightRenderTarget.Texture.TextureId));
            CheckGlError();

            var clearEv = new GetClearColorEvent();
            _entityManager.EventBus.RaiseEvent(EventSource.Local, ref clearEv);

            var clearColor = clearEv.Color ?? GetClearColor(mapUid);
            GLClearColor(clearColor);
            GL.ClearStencil(0xFF);
            GL.StencilMask(0xFF);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit);
            CheckGlError();

            var oldTarget = _currentRenderTarget;
            var oldProj = _currentMatrixProj;
            var oldShader = _queuedShaderInstance;
            var oldModel = _currentMatrixModel;
            var oldScissor = _currentScissorState;
            var state = PushRenderStateFull();

            RenderOverlays(viewport, OverlaySpace.BeforeLighting, worldAABB, worldBounds);
            PopRenderStateFull(state);

            DebugTools.Assert(oldScissor.Equals(_currentScissorState));
            DebugTools.Assert(oldModel.Equals(_currentMatrixModel));
            DebugTools.Assert(oldShader.Equals(_queuedShaderInstance));
            DebugTools.Assert(oldProj.Equals(_currentMatrixProj));
            DebugTools.Assert(oldTarget.Equals(_currentRenderTarget));
            DebugTools.Assert(_currentBoundRenderTarget.TextureHandle.Equals(viewport.LightRenderTarget.Texture.TextureId));

            ApplyLightingFovToBuffer(viewport, eye);

            var lightShader = _loadedShaders[_enableSoftShadows ? _lightSoftShaderHandle : _lightHardShaderHandle]
                .Program;
            lightShader.Use();

            SetupGlobalUniformsImmediate(lightShader, ShadowTexture);

            SetTexture(TextureUnit.Texture1, ShadowTexture);
            lightShader.SetUniformTextureMaybe("shadowMap", TextureUnit.Texture1);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            CheckGlError();

            GL.StencilFunc(StencilFunction.Equal, 0xFF, 0xFF);
            CheckGlError();
            GL.StencilOp(TKStencilOp.Keep, TKStencilOp.Keep, TKStencilOp.Keep);
            CheckGlError();

            var lastRange = float.NaN;
            var lastPower = float.NaN;
            var lastColor = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
            var lastSoftness = float.NaN;
            var lastFalloff = float.NaN;
            var lastCurveFactor = float.NaN;
            Texture? lastMask = null;

            using (_prof.Group("Draw Lights"))
            {
                for (var i = 0; i < count; i++)
                {
                    ref var lightData = ref _lightsToRenderList[i];
                    var component = lightData.Light;
                    var lightPos = lightData.Position;
                    var rot = lightData.Rotation;

                    Texture? mask = null;
                    var rotation = Angle.Zero;
                    if (component.Mask != null)
                    {
                        mask = component.Mask;
                        rotation = SharedPointLightSystem.GetMaskWorldRotation(component, rot);
                    }

                    var maskTexture = mask ?? _stockTextureWhite;
                    if (lastMask != maskTexture)
                    {
                        SetTexture(TextureUnit.Texture0, maskTexture);
                        lastMask = maskTexture;
                        lightShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
                    }

                    if (!MathHelper.CloseToPercent(lastRange, component.Radius))
                    {
                        lastRange = component.Radius;
                        lightShader.SetUniformMaybe("lightRange", lastRange);
                    }

                    if (!MathHelper.CloseToPercent(lastPower, component.Energy))
                    {
                        lastPower = component.Energy;
                        lightShader.SetUniformMaybe("lightPower", lastPower);
                    }

                    if (lastColor != component.Color)
                    {
                        lastColor = component.Color;
                        lightShader.SetUniformMaybe("lightColor", lastColor);
                    }

                    if (_enableSoftShadows && !MathHelper.CloseToPercent(lastSoftness, component.Softness))
                    {
                        lastSoftness = component.Softness;
                        lightShader.SetUniformMaybe("lightSoftness", lastSoftness);
                    }

                    if (!MathHelper.CloseToPercent(lastFalloff, component.Falloff))
                    {
                        lastFalloff = component.Falloff;
                        lightShader.SetUniformMaybe("lightFalloff", lastFalloff);
                    }

                    if (!MathHelper.CloseToPercent(lastCurveFactor, component.CurveFactor))
                    {
                        lastCurveFactor = component.CurveFactor;
                        lightShader.SetUniformMaybe("lightCurveFactor", lastCurveFactor);
                    }

                    lightShader.SetUniformMaybe("lightCenter", lightPos);
                    lightShader.SetUniformMaybe("lightIndex",
                        lightData.ShadowMapIndex >= 0 ? (lightData.ShadowMapIndex + 0.5f) / ShadowTexture.Height : -1);

                    var offset = new Vector2(component.Radius, component.Radius);

                    Matrix3x2 matrix;
                    if (mask == null)
                    {
                        matrix = Matrix3x2.Identity;
                    }
                    else
                    {
                        // Only apply rotation if a mask is said, because else it doesn't matter.
                        matrix = Matrix3Helpers.CreateRotation(rotation);
                    }

                    (matrix.M31, matrix.M32) = lightPos;

                    _drawQuad(-offset, offset, matrix, lightShader);
                }
            }

            ResetBlendFunc();
            IsStencilling = false;

            CheckGlError();

            if (_cfg.GetCVar(CVars.LightBlur))
                BlurRenderTarget(viewport, viewport.LightRenderTarget, viewport.LightBlurTarget, eye, 14f);

            using (_prof.Group("BlurOntoWalls"))
            {
                BlurOntoWalls(viewport, eye);
            }

            using (_prof.Group("MergeWallLayer"))
            {
                MergeWallLayer(viewport);
            }

            BindRenderTargetFull(viewport.RenderTarget);
            GL.Viewport(0, 0, viewport.Size.X, viewport.Size.Y);
            CheckGlError();

            _lightingReady = true;
            Array.Clear(_lightsToRenderList, 0, count);
        }

        private static bool LightQuery(ref (
            Clyde clyde,
            MapId map,
            int count,
            int shadowCastingCount,
            EntityQuery<TransformComponent> xforms,
            Box2 worldAABB) state,
            in ComponentTreeEntry<SharedPointLightComponent> value)
        {
            ref var count = ref state.count;
            ref var shadowCount = ref state.shadowCastingCount;

            // If there are too many lights, exit the query
            if (count >= state.clyde._maxLights)
                return false;

            var (light, transform) = value;
            if (light is not PointLightComponent pointLight)
                return true;

            var (lightPos, rot) = state.clyde._transformSystem.GetWorldPositionRotation(transform, state.xforms);
            lightPos += rot.RotateVec(light.Offset);
            var circle = new Circle(lightPos, light.Radius);

            // If the light doesn't touch anywhere the camera can see, it doesn't matter.
            // The tree query is not fully accurate because the viewport may be rotated relative to a grid.
            if (!circle.Intersects(state.worldAABB))
                return true;

            if (light.CastShadows)
            {
                // Shadow-casting lights embedded inside an occluder cannot work consistently.
                // As such we just disable them! If you want light inside an occluder use non-shadow casting lights!
                if (state.clyde.IsLightEmbeddedInOccluder(state.map, lightPos, state.xforms))
                    return true;

                // If the light is a shadow casting light, keep a separate track of that.
                shadowCount++;
            }

            var distanceSquared = (state.worldAABB.Center - lightPos).LengthSquared();
            state.clyde._lightsToRenderList[count++] = new LightRenderData(
                pointLight,
                lightPos,
                distanceSquared,
                rot);

            return true;
        }

        private struct LightRenderData
        {
            public PointLightComponent Light;
            public Vector2 Position;
            public float DistanceSquared;
            public Angle Rotation;
            public bool CastShadows;
            public int ShadowMapIndex;

            public LightRenderData(
                PointLightComponent light,
                Vector2 position,
                float distanceSquared,
                Angle rotation)
            {
                Light = light;
                Position = position;
                DistanceSquared = distanceSquared;
                Rotation = rotation;
                CastShadows = light.CastShadows;
                ShadowMapIndex = -1;
            }
        }

        private sealed class LightCapacityComparer : IComparer<LightRenderData>
        {
            public int Compare(LightRenderData x, LightRenderData y)
            {
                if (x.CastShadows && !y.CastShadows) return 1;
                if (!x.CastShadows && y.CastShadows) return -1;
                return 0;
            }
        }

        private sealed class ShadowCapacityComparer : IComparer<LightRenderData>
        {
            public int Compare(LightRenderData x, LightRenderData y)
            {
                return x.DistanceSquared.CompareTo(y.DistanceSquared);
            }
        }

        private (int count, Box2 expandedBounds) GetLightsToRender(
            MapId map,
            in Box2Rotated worldBounds,
            in Box2 worldAABB)
        {
            // Use worldbounds for this one as we only care if the light intersects our actual bounds
            var xforms = _entityManager.GetEntityQuery<TransformComponent>();
            var state = (this, map, count: 0, shadowCastingCount: 0, xforms, worldAABB);
            var lightAabb = worldAABB.Enlarged(_maxLightRadius);

            foreach (var (uid, comp) in _lightTreeSystem.GetIntersectingTrees(map, lightAabb))
            {
                var bounds = _transformSystem.GetInvWorldMatrix(uid, xforms).TransformBox(worldBounds);
                comp.Tree.QueryAabb(ref state, LightQuery, bounds);
            }

            if (state.shadowCastingCount > _maxShadowcastingLights)
            {
                // There are too many lights casting shadows to fit in the scene.
                // This check must occur before occluder expansion, or else bad things happen.

                // First, partition the array based on whether the lights are shadow casting or not
                // (non shadow casting lights should be the first partition, shadow casting lights the second)
                Array.Sort(_lightsToRenderList, 0, state.count, _lightCap);

                // Next, sort just the shadow casting lights by distance.
                Array.Sort(_lightsToRenderList, state.count - state.shadowCastingCount, state.shadowCastingCount, _shadowCap);

                // Then effectively delete the furthest lights, by setting the end of the array to exclude N
                // number of shadow casting lights (where N is the number above the max number per scene.)
                state.count -= state.shadowCastingCount - _maxShadowcastingLights;
            }

            // When culling occluders later, we can't just remove any occluders outside the worldBounds.
            // As they could still affect the shadows of (large) light sources.
            // We expand the world bounds so that it encompasses the center of every light source.
            // This should make it so no culled occluder can make a difference.
            // (if the occluder is in the current lights at all, it's still not between the light and the world bounds).
            var expandedBounds = worldAABB;

            for (var i = 0; i < state.count; i++)
            {
                expandedBounds = expandedBounds.ExtendToContain(_lightsToRenderList[i].Position);
            }

            var renderedShadowCastingCount = AssignShadowMapRows(_lightsToRenderList.AsSpan(0, state.count), _maxShadowcastingLights);

            _debugStats.TotalLights += state.count;
            _debugStats.ShadowLights += renderedShadowCastingCount;

            return (state.count, expandedBounds);
        }

        private static int AssignShadowMapRows(Span<LightRenderData> lights, int maxShadowcastingLights)
        {
            var shadowMapIndex = 0;

            for (var i = 0; i < lights.Length; i++)
            {
                ref var lightData = ref lights[i];
                lightData.ShadowMapIndex = -1;

                if (!lightData.CastShadows || shadowMapIndex >= maxShadowcastingLights)
                    continue;

                lightData.ShadowMapIndex = shadowMapIndex;
                shadowMapIndex++;
            }

            return shadowMapIndex;
        }

        private bool IsLightEmbeddedInOccluder(
            MapId map,
            Vector2 lightPosition,
            EntityQuery<TransformComponent> xforms)
        {
            // Shadow-casting lights inside an occluder produce unstable/inside-out shadows.
            // Do a narrow tree query around the light and only run the expensive polygon TestPoint
            // for occluders whose cached AABB can contain the light.
            var pointBounds = new Box2(lightPosition, lightPosition).Enlarged(SharedOccluderEdgeTolerance);

            foreach (var (treeUid, comp) in _occluderSystem.GetIntersectingTrees(map, pointBounds))
            {
                var treeBounds = _transformSystem.GetInvWorldMatrix(treeUid, xforms).TransformBox(pointBounds);
                var state = new LightEmbeddedOccluderQueryState(
                    _fixtureSystem,
                    _transformSystem,
                    xforms,
                    lightPosition);

                comp.Tree.QueryAabb(ref state, CheckLightEmbeddedInOccluder, treeBounds, approx: true);

                if (state.Embedded)
                    return true;
            }

            return false;
        }

        private static bool CheckLightEmbeddedInOccluder(
            ref LightEmbeddedOccluderQueryState state,
            in ComponentTreeEntry<OccluderComponent> entry)
        {
            var occluder = entry.Component;
            if (!occluder.Enabled)
                return true;

            var (worldPosition, worldRotation) = state.TransformSystem.GetWorldPositionRotation(
                entry.Transform,
                state.Xforms);

            if (!OccluderOverlapsPoint(
                    state.FixtureSystem,
                    occluder.PolygonArray,
                    new Transform(worldPosition, worldRotation),
                    state.LightPosition))
            {
                return true;
            }

            state.Embedded = true;
            return false;
        }

        private struct LightEmbeddedOccluderQueryState(
            FixtureSystem fixtureSystem,
            TransformSystem transformSystem,
            EntityQuery<TransformComponent> xforms,
            Vector2 lightPosition)
        {
            public readonly FixtureSystem FixtureSystem = fixtureSystem;
            public readonly TransformSystem TransformSystem = transformSystem;
            public readonly EntityQuery<TransformComponent> Xforms = xforms;
            public readonly Vector2 LightPosition = lightPosition;
            public bool Embedded;
        }

        /// <inheritdoc/>
        [Pure]
        public Color GetClearColor(EntityUid mapUid)
        {
            return _entityManager.GetComponentOrNull<MapLightComponent>(mapUid)?.AmbientLightColor ??
                MapLightComponent.DefaultColor;
        }

        /// <inheritdoc/>
        public void BlurRenderTarget(IClydeViewport viewport, IRenderTarget target, IRenderTarget blurBuffer, IEye eye, float multiplier)
        {
            if (target is not RenderTexture rTexture || blurBuffer is not RenderTexture blurTexture)
                return;

            using var _ = DebugGroup(nameof(BlurRenderTarget));

            var state = PushRenderStateFull();
            IsBlending = false;
            CalcScreenMatrices(viewport.Size, out var proj, out var view);
            SetProjViewBuffer(proj, view);

            var shader = _loadedShaders[_lightBlurShaderHandle].Program;
            shader.Use();

            SetupGlobalUniformsImmediate(shader, rTexture.Texture);

            var size = target.Size;
            shader.SetUniformMaybe("size", (Vector2)size);
            shader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            GL.Viewport(0, 0, size.X, size.Y);
            CheckGlError();

            // Initially we're pulling from the light render target.
            // So we set it out of the loop so
            // _wallBleedIntermediateRenderTarget2 gets bound at the end of the loop body.
            SetTexture(TextureUnit.Texture0, rTexture.Texture);

            // Have to scale the blurring radius based on viewport size and camera zoom.
            var facBase = _cfg.GetCVar(CVars.LightBlurFactor);
            var cameraSize = eye.Zoom.Y * viewport.Size.Y * (1 / viewport.RenderScale.Y) / EyeManager.PixelsPerMeter;
            // 7e-3f is just a magic factor that makes it look ok.
            var factor = facBase * (multiplier / cameraSize);

            // Multi-iteration gaussian blur.
            for (var i = 3; i > 0; i--)
            {
                var scale = (i + 1) * factor;
                // Set factor.
                shader.SetUniformMaybe("radius", scale);

                BindRenderTargetImmediate(RtToLoaded(blurBuffer));

                // Blur horizontally to _wallBleedIntermediateRenderTarget1.
                shader.SetUniformMaybe("direction", Vector2.UnitX);
                _drawQuad(Vector2.Zero, viewport.Size, Matrix3x2.Identity, shader);

                SetTexture(TextureUnit.Texture0, blurTexture.Texture);

                BindRenderTargetImmediate(RtToLoaded(rTexture));

                // Blur vertically to _wallBleedIntermediateRenderTarget2.
                shader.SetUniformMaybe("direction", Vector2.UnitY);
                _drawQuad(Vector2.Zero, viewport.Size, Matrix3x2.Identity, shader);

                SetTexture(TextureUnit.Texture0, rTexture.Texture);
            }

            PopRenderStateFull(state);
        }

        private void BlurOntoWalls(Viewport viewport, IEye eye)
        {
            using var _ = DebugGroup(nameof(BlurOntoWalls));

            IsBlending = false;
            CalcScreenMatrices(viewport.Size, out var proj, out var view);
            SetProjViewBuffer(proj, view);

            var shader = _loadedShaders[_wallBleedBlurShaderHandle].Program;
            shader.Use();

            SetupGlobalUniformsImmediate(shader, viewport.LightRenderTarget.Texture);

            shader.SetUniformMaybe("size", (Vector2)viewport.WallBleedIntermediateRenderTarget1.Size);
            shader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            var size = viewport.WallBleedIntermediateRenderTarget1.Size;
            GL.Viewport(0, 0, size.X, size.Y);
            CheckGlError();

            // Initially we're pulling from the light render target.
            // So we set it out of the loop so
            // _wallBleedIntermediateRenderTarget2 gets bound at the end of the loop body.
            SetTexture(TextureUnit.Texture0, viewport.LightRenderTarget.Texture);

            // Have to scale the blurring radius based on viewport size and camera zoom.
            const float refCameraHeight = 14;
            var cameraSize = eye.Zoom.Y * viewport.Size.Y * (1 / viewport.RenderScale.Y) / EyeManager.PixelsPerMeter;
            // 7e-3f is just a magic factor that makes it look ok.
            var factor = 7e-3f * (refCameraHeight / cameraSize);

            // Multi-iteration gaussian blur.
            for (var i = 3; i > 0; i--)
            {
                var scale = (i + 1) * factor;
                // Set factor.
                shader.SetUniformMaybe("radius", scale);

                BindRenderTargetFull(viewport.WallBleedIntermediateRenderTarget1);

                // Blur horizontally to _wallBleedIntermediateRenderTarget1.
                shader.SetUniformMaybe("direction", Vector2.UnitX);
                _drawQuad(Vector2.Zero, viewport.Size, Matrix3x2.Identity, shader);

                SetTexture(TextureUnit.Texture0, viewport.WallBleedIntermediateRenderTarget1.Texture);
                BindRenderTargetFull(viewport.WallBleedIntermediateRenderTarget2);

                // Blur vertically to _wallBleedIntermediateRenderTarget2.
                shader.SetUniformMaybe("direction", Vector2.UnitY);
                _drawQuad(Vector2.Zero, viewport.Size, Matrix3x2.Identity, shader);

                SetTexture(TextureUnit.Texture0, viewport.WallBleedIntermediateRenderTarget2.Texture);
            }

            IsBlending = true;
            // We didn't trample over the old _currentMatrices so just roll it back.
            SetProjViewBuffer(_currentMatrixProj, _currentMatrixView);
        }

        private void MergeWallLayer(Viewport viewport)
        {
            using var _ = DebugGroup(nameof(MergeWallLayer));

            BindRenderTargetFull(viewport.LightRenderTarget);

            GL.Viewport(0, 0, viewport.LightRenderTarget.Size.X, viewport.LightRenderTarget.Size.Y);
            CheckGlError();
            IsBlending = false;

            var shader = _loadedShaders[_mergeWallLayerShaderHandle].Program;
            shader.Use();

            var tex = viewport.WallBleedIntermediateRenderTarget2.Texture;

            SetupGlobalUniformsImmediate(shader, tex);

            SetTexture(TextureUnit.Texture0, tex);

            shader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            BindVertexArray(_occlusionMaskVao.Handle);
            CheckGlError();

            GL.DrawElements(PrimitiveType.Triangles, _occlusionMaskDataLength, DrawElementsType.UnsignedShort,
                IntPtr.Zero);
            CheckGlError();

            IsBlending = true;
        }

        private void ApplyFovToBuffer(Viewport viewport, IEye eye)
        {
            GL.Clear(ClearBufferMask.StencilBufferBit);
            GL.Enable(EnableCap.StencilTest);
            GL.StencilOp(OpenToolkit.Graphics.OpenGL4.StencilOp.Keep, OpenToolkit.Graphics.OpenGL4.StencilOp.Keep,
                OpenToolkit.Graphics.OpenGL4.StencilOp.Replace);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilMask(0xFF);

            // Applies FOV to the final framebuffer.

            var fovShader = _loadedShaders[_fovShaderHandle].Program;
            fovShader.Use();

            SetupGlobalUniformsImmediate(fovShader, FovTexture);

            SetTexture(TextureUnit.Texture0, FovTexture);

            fovShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            if (!Color.TryParse(_cfg.GetCVar(CVars.RenderFOVColor), out var color))
                color = Color.Black;

            fovShader.SetUniformMaybe("occludeColor", color);
            FovSetTransformAndBlit(viewport, eye.Position.Position, fovShader);

            GL.StencilMask(0x00);
            IsStencilling = false;
        }

        private void ApplyLightingFovToBuffer(Viewport viewport, IEye eye)
        {
            // Applies FOV to the lighting framebuffer.

            var fovShader = _loadedShaders[_fovLightShaderHandle].Program;
            fovShader.Use();

            SetupGlobalUniformsImmediate(fovShader, FovTexture);

            SetTexture(TextureUnit.Texture0, FovTexture);

            // Have to swap to linear filtering on the shadow map here.
            // VSM wants it.
            if (_hasGLSamplerObjects)
            {
                GL.BindSampler(0, _fovFilterSampler.Handle);
                CheckGlError();
            }
            else
            {
                // OpenGL why do you torture me so.
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
                CheckGlError();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
                CheckGlError();
            }

            fovShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            GL.StencilMask(0xFF);
            CheckGlError();
            GL.StencilFunc(StencilFunction.Always, 0, 0);
            CheckGlError();
            GL.StencilOp(TKStencilOp.Keep, TKStencilOp.Keep, TKStencilOp.Replace);
            CheckGlError();

            fovShader.SetUniformMaybe("occludeColor", Color.Black);
            FovSetTransformAndBlit(viewport, eye.Position.Position, fovShader);

            if (_hasGLSamplerObjects)
            {
                GL.BindSampler(0, 0);
                CheckGlError();
            }
            else
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
                CheckGlError();
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
                CheckGlError();
            }
        }

        private void FovSetTransformAndBlit(Viewport vp, Vector2 fovCentre, GLShaderProgram fovShader)
        {
            // It might be an idea if there was a proper way to get the LocalToWorld matrix.
            // But actually constructing the matrix tends to be more trouble than it's worth in most cases.
            // (Maybe if there was some way to observe Eye matrix changes that wouldn't be the case, as viewport could dynamically update.)
            // This is expected to run a grand total of twice per frame for 6 LocalToWorld calls.
            // Something else to note is that modifications must be made anyway.

            // Something ELSE to note is that it's absolutely critical that this be calculated in the "right way" due to precision issues!

            // Bit of an interesting little trick here - need to set things up correctly.
            // 0, 0 in clip-space is the centre of the screen, and 1, 1 is the top-right corner.
            var halfSize = vp.Size / 2.0f;
            var uZero = vp.LocalToWorld(halfSize).Position;
            var uX = vp.LocalToWorld(halfSize + (Vector2.UnitX * halfSize.X)).Position - uZero;
            var uY = vp.LocalToWorld(halfSize - (Vector2.UnitY * halfSize.Y)).Position - uZero;

            // Second modification is that output must be fov-centred (difference-space)
            uZero -= fovCentre;

            var clipToDiff = new Matrix3x2(uX.X, uX.Y, uY.X, uY.Y, uZero.X, uZero.Y);

            fovShader.SetUniformMaybe("clipToDiff", clipToDiff);
            _drawQuad(Vector2.Zero, Vector2.One, Matrix3x2.Identity, fovShader);
        }

        private static int BuildOccluderEdges(
            ReadOnlySpan<Vector2> polygon,
            Matrix3x2 worldTransform,
            Span<Vector4> edges)
        {
            if (polygon.Length < 3)
                return 0;

            Span<Vector2> worldVertices = polygon.Length <= 64
                ? stackalloc Vector2[polygon.Length]
                : new Vector2[polygon.Length];

            // Occluder polygons are stored as physics hulls, i.e. generally CCW.
            // The depth shader is authored for clockwise wall edges, so normalize the order here.
            // TODO: Make the shader CCW to get back CPU perf here.
            var clockwise = SignedArea(polygon) < 0f;
            for (var i = 0; i < polygon.Length; i++)
            {
                var sourceIndex = clockwise ? i : polygon.Length - 1 - i;
                worldVertices[i] = Vector2.Transform(polygon[sourceIndex], worldTransform);
            }

            var edgeCount = 0;
            for (var i = 0; i < worldVertices.Length && edgeCount < edges.Length; i++)
            {
                edges[edgeCount++] = EdgeToVector4(worldVertices[i], worldVertices[(i + 1) % worldVertices.Length]);
            }

            return edgeCount;
        }

        private static void AddOccluderBoundaryEdges(
            ReadOnlySpan<Vector2> polygon,
            Matrix3x2 worldTransform,
            uint sharedEdgeMask,
            HashSet<OccluderEdgeKey> sharedBoundaryEdges,
            List<Vector4> boundarySegments)
        {
            if (polygon.Length < 3)
                return;

            var clockwise = SignedArea(polygon) < 0f;
            for (var i = 0; i < polygon.Length; i++)
            {
                var sourceIndex = clockwise ? i : polygon.Length - 1 - i;
                var nextIndex = clockwise ? (i + 1) % polygon.Length : (polygon.Length - 2 - i + polygon.Length) % polygon.Length;
                var a = Vector2.Transform(polygon[sourceIndex], worldTransform);
                var b = Vector2.Transform(polygon[nextIndex], worldTransform);
                var edge = EdgeToVector4(a, b);

                boundarySegments.Add(edge);
                if ((sharedEdgeMask & (1u << i)) != 0)
                    sharedBoundaryEdges.Add(OccluderEdgeKey.From(edge));
            }
        }

        private static void BuildVisibleBoundaryVertices(
            IReadOnlyList<Vector4> boundarySegments,
            IReadOnlySet<OccluderEdgeKey> sharedBoundaryEdges,
            Vector2 eyePosition,
            HashSet<OccluderVertexKey> visibleBoundaryVertices)
        {
            visibleBoundaryVertices.Clear();

            foreach (var edge in boundarySegments)
            {
                if (sharedBoundaryEdges.Contains(OccluderEdgeKey.From(edge)) || !EdgeFacesPoint(edge, eyePosition))
                    continue;

                visibleBoundaryVertices.Add(OccluderVertexKey.From(new Vector2(edge.X, edge.Y)));
                visibleBoundaryVertices.Add(OccluderVertexKey.From(new Vector2(edge.Z, edge.W)));
            }
        }

        private static void BuildConvexBoundaryVertices(
            IReadOnlyList<Vector4> boundarySegments,
            IReadOnlySet<OccluderEdgeKey> sharedBoundaryEdges,
            Dictionary<OccluderVertexKey, BoundaryVertexDirections> boundaryVertexDirections,
            HashSet<OccluderVertexKey> convexBoundaryVertices)
        {
            boundaryVertexDirections.Clear();
            convexBoundaryVertices.Clear();

            foreach (var edge in boundarySegments)
            {
                if (sharedBoundaryEdges.Contains(OccluderEdgeKey.From(edge)))
                    continue;

                var a = new Vector2(edge.X, edge.Y);
                var b = new Vector2(edge.Z, edge.W);
                var direction = b - a;

                var aKey = OccluderVertexKey.From(a);
                boundaryVertexDirections.TryGetValue(aKey, out var aDirections);
                aDirections.Outgoing = direction;
                aDirections.OutgoingCount++;
                boundaryVertexDirections[aKey] = aDirections;

                var bKey = OccluderVertexKey.From(b);
                boundaryVertexDirections.TryGetValue(bKey, out var bDirections);
                bDirections.Incoming = direction;
                bDirections.IncomingCount++;
                boundaryVertexDirections[bKey] = bDirections;
            }

            foreach (var (vertex, directions) in boundaryVertexDirections)
            {
                if (directions.IncomingCount != 1 || directions.OutgoingCount != 1)
                    continue;

                if (Vector2.Cross(directions.Incoming, directions.Outgoing) < -SharedOccluderEdgeTolerance)
                    convexBoundaryVertices.Add(vertex);
            }
        }

        private static void BuildSharedVertexEdges(
            IReadOnlyList<Vector4> boundarySegments,
            IReadOnlySet<OccluderEdgeKey> sharedBoundaryEdges,
            Dictionary<OccluderVertexKey, List<Vector4>> sharedVertexEdges,
            HashSet<OccluderEdgeKey> uniqueSharedEdges,
            List<OccluderVertexKey>? staleVertices = null)
        {
            foreach (var edges in sharedVertexEdges.Values)
            {
                edges.Clear();
            }
            uniqueSharedEdges.Clear();

            foreach (var edge in boundarySegments)
            {
                var edgeKey = OccluderEdgeKey.From(edge);
                if (!sharedBoundaryEdges.Contains(edgeKey) || !uniqueSharedEdges.Add(edgeKey))
                    continue;

                AddSharedVertexEdge(new Vector2(edge.X, edge.Y), edge, sharedVertexEdges);
                AddSharedVertexEdge(new Vector2(edge.Z, edge.W), edge, sharedVertexEdges);
            }

            if (staleVertices == null)
                return;

            staleVertices.Clear();
            foreach (var (vertex, edges) in sharedVertexEdges)
            {
                if (edges.Count == 0)
                    staleVertices.Add(vertex);
            }

            foreach (var vertex in staleVertices)
            {
                sharedVertexEdges.Remove(vertex);
            }
        }

        private static void AddSharedVertexEdge(
            Vector2 vertex,
            Vector4 edge,
            Dictionary<OccluderVertexKey, List<Vector4>> sharedVertexEdges)
        {
            var key = OccluderVertexKey.From(vertex);
            if (!sharedVertexEdges.TryGetValue(key, out var edges))
            {
                edges = new List<Vector4>();
                sharedVertexEdges[key] = edges;
            }

            edges.Add(edge);
        }

        private static bool OccluderOverlapsPoint(
            FixtureSystem fixtures,
            Vector2[] polygon,
            in Transform occluderTransform,
            Vector2 worldPoint)
        {
            if (polygon.Length < 3)
                return false;

            var occluderShape = new Polygon(polygon);
            return occluderShape.VertexCount >= 3 && fixtures.TestPoint(occluderShape, occluderTransform, worldPoint);
        }

        private static bool PointsMatch(Vector2 a, Vector2 b)
        {
            return Vector2.DistanceSquared(a, b) <= SharedOccluderEdgeToleranceSquared;
        }

        private static bool ShouldSuppressSharedOccluderEdge(
            int edgeIndex,
            ReadOnlySpan<Vector4> edges,
            ReadOnlySpan<bool> sharedEdges,
            IReadOnlySet<OccluderVertexKey> visibleBoundaryVertices,
            IReadOnlySet<OccluderVertexKey> convexBoundaryVertices,
            IReadOnlyDictionary<OccluderVertexKey, List<Vector4>> sharedVertexEdges,
            Vector2 eyePosition)
        {
            if (!sharedEdges[edgeIndex])
                return false;

            var edge = edges[edgeIndex];

            // Corner-handling for occlusion.
            if (EdgeViewedAsCap(edge, eyePosition)
                || SharedEdgeContinuesThroughEyeProjection(edge, sharedVertexEdges, eyePosition)
                || edges.Length == 3 && SharedEdgeTurnsAwayFromEyeAtCorner(edge, sharedVertexEdges, eyePosition))
            {
                return false;
            }

            var previous = edgeIndex == 0 ? edges.Length - 1 : edgeIndex - 1;
            var next = edgeIndex + 1 == edges.Length ? 0 : edgeIndex + 1;
            var a = new Vector2(edge.X, edge.Y);
            var b = new Vector2(edge.Z, edge.W);

            var startVisible = !sharedEdges[previous] && EdgeFacesPoint(edges[previous], eyePosition);
            if (!startVisible && HasBoundaryVertex(a, convexBoundaryVertices))
                startVisible = HasBoundaryVertex(a, visibleBoundaryVertices);

            var endVisible = !sharedEdges[next] && EdgeFacesPoint(edges[next], eyePosition);
            if (!endVisible && HasBoundaryVertex(b, convexBoundaryVertices))
                endVisible = HasBoundaryVertex(b, visibleBoundaryVertices);

            return startVisible || endVisible;
        }

        private static bool SharedEdgeContinuesThroughEyeProjection(
            Vector4 edge,
            IReadOnlyDictionary<OccluderVertexKey, List<Vector4>> sharedVertexEdges,
            Vector2 eyePosition)
        {
            var a = new Vector2(edge.X, edge.Y);
            var b = new Vector2(edge.Z, edge.W);
            var edgeDelta = b - a;
            var edgeLengthSquared = edgeDelta.LengthSquared();
            if (edgeLengthSquared <= SharedOccluderEdgeToleranceSquared)
                return false;

            var eyeFromA = eyePosition - a;
            var signedArea = Vector2.Cross(edgeDelta, eyeFromA);
            if (signedArea * signedArea <= SharedOccluderEdgeToleranceSquared * edgeLengthSquared)
                return false;

            var projected = Vector2.Dot(eyeFromA, edgeDelta) / edgeLengthSquared;
            // Handle centres of squares essentially, mostly around diagonal walls and ensuring they function
            // similarly to normal walls in a block of 2x2 for example.
            if (MathF.Abs(projected) <= SharedOccluderEdgeTolerance)
                return HasOppositeCollinearSharedEdge(a, b - a, edge, sharedVertexEdges);

            if (MathF.Abs(projected - 1f) <= SharedOccluderEdgeTolerance)
                return HasOppositeCollinearSharedEdge(b, a - b, edge, sharedVertexEdges);

            return false;
        }

        private static bool SharedEdgeTurnsAwayFromEyeAtCorner(
            Vector4 edge,
            IReadOnlyDictionary<OccluderVertexKey, List<Vector4>> sharedVertexEdges,
            Vector2 eyePosition)
        {
            var a = new Vector2(edge.X, edge.Y);
            var b = new Vector2(edge.Z, edge.W);
            var edgeDelta = b - a;
            var edgeLengthSquared = edgeDelta.LengthSquared();
            if (edgeLengthSquared <= SharedOccluderEdgeToleranceSquared)
                return false;

            var projected = Vector2.Dot(eyePosition - a, edgeDelta) / edgeLengthSquared;
            if (projected > SharedOccluderEdgeTolerance && projected < 1f - SharedOccluderEdgeTolerance)
                return false;

            var junction = projected <= SharedOccluderEdgeTolerance ? a : b;
            var currentFromJunction = projected <= SharedOccluderEdgeTolerance ? b - a : a - b;
            var currentLengthSquared = currentFromJunction.LengthSquared();
            var eyeFromJunction = eyePosition - junction;
            if (eyeFromJunction.LengthSquared() <= SharedOccluderEdgeToleranceSquared)
                return false;

            var key = OccluderVertexKey.From(junction);
            var currentKey = OccluderEdgeKey.From(edge);
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (!sharedVertexEdges.TryGetValue(
                            new OccluderVertexKey(key.X + dx, key.Y + dy),
                            out var candidates))
                        continue;

                    foreach (var candidate in candidates)
                    {
                        if (OccluderEdgeKey.From(candidate) == currentKey)
                            continue;

                        var candidateA = new Vector2(candidate.X, candidate.Y);
                        var candidateB = new Vector2(candidate.Z, candidate.W);
                        Vector2 candidateFromJunction;
                        if (PointsMatch(candidateA, junction))
                            candidateFromJunction = candidateB - junction;
                        else if (PointsMatch(candidateB, junction))
                            candidateFromJunction = candidateA - junction;
                        else
                            continue;

                        var candidateLengthSquared = candidateFromJunction.LengthSquared();
                        if (candidateLengthSquared <= SharedOccluderEdgeToleranceSquared)
                            continue;

                        var cross = Vector2.Cross(currentFromJunction, candidateFromJunction);
                        if (cross * cross <= SharedOccluderEdgeToleranceSquared * currentLengthSquared * candidateLengthSquared)
                            continue;

                        // The shared edge is one side of a shared corner. If the eye is opposite the corner's
                        // outgoing wedge, this edge is behind a wall.
                        var wedgeDirection = currentFromJunction + candidateFromJunction;
                        if (wedgeDirection.LengthSquared() <= SharedOccluderEdgeToleranceSquared)
                            continue;

                        if (Vector2.Dot(eyeFromJunction, wedgeDirection) < 0f)
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool HasOppositeCollinearSharedEdge(
            Vector2 junction,
            Vector2 currentFromJunction,
            Vector4 currentEdge,
            IReadOnlyDictionary<OccluderVertexKey, List<Vector4>> sharedVertexEdges)
        {
            var key = OccluderVertexKey.From(junction);
            var currentKey = OccluderEdgeKey.From(currentEdge);
            var currentLengthSquared = currentFromJunction.LengthSquared();

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (!sharedVertexEdges.TryGetValue(
                            new OccluderVertexKey(key.X + dx, key.Y + dy),
                            out var candidates))
                        continue;

                    foreach (var candidate in candidates)
                    {
                        if (OccluderEdgeKey.From(candidate) == currentKey)
                            continue;

                        var candidateA = new Vector2(candidate.X, candidate.Y);
                        var candidateB = new Vector2(candidate.Z, candidate.W);
                        Vector2 candidateFromJunction;
                        if (PointsMatch(candidateA, junction))
                            candidateFromJunction = candidateB - junction;
                        else if (PointsMatch(candidateB, junction))
                            candidateFromJunction = candidateA - junction;
                        else
                            continue;

                        var candidateLengthSquared = candidateFromJunction.LengthSquared();
                        if (candidateLengthSquared <= SharedOccluderEdgeToleranceSquared)
                            continue;

                        var cross = Vector2.Cross(currentFromJunction, candidateFromJunction);
                        if (cross * cross > SharedOccluderEdgeToleranceSquared * currentLengthSquared * candidateLengthSquared)
                            continue;

                        if (Vector2.Dot(currentFromJunction, candidateFromJunction) < 0f)
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool HasBoundaryVertex(
            Vector2 vertex,
            IReadOnlySet<OccluderVertexKey> boundaryVertices)
        {
            var key = OccluderVertexKey.From(vertex);
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (boundaryVertices.Contains(new OccluderVertexKey(key.X + dx, key.Y + dy)))
                        return true;
                }
            }

            return false;
        }

        private static bool EdgeViewedAsCap(Vector4 edge, Vector2 eyePosition)
        {
            // Corner-handling so we only suppress from the relevant angles as it depends on the eye position.
            var a = new Vector2(edge.X, edge.Y);
            var b = new Vector2(edge.Z, edge.W);
            var edgeDelta = b - a;
            var edgeLengthSquared = edgeDelta.LengthSquared();
            if (edgeLengthSquared <= SharedOccluderEdgeToleranceSquared)
                return false;

            var eyeFromA = eyePosition - a;
            var projected = Vector2.Dot(eyeFromA, edgeDelta) / edgeLengthSquared;
            if (projected <= SharedOccluderEdgeTolerance || projected >= 1f - SharedOccluderEdgeTolerance)
                return false;

            var signedArea = Vector2.Cross(edgeDelta, eyeFromA);
            return signedArea * signedArea > SharedOccluderEdgeToleranceSquared * edgeLengthSquared;
        }

        private static bool EdgeFacesPoint(Vector4 edge, Vector2 point)
        {
            var a = new Vector2(edge.X, edge.Y) - point;
            var b = new Vector2(edge.Z, edge.W) - point;
            return Vector2.Cross(a, b) > 0f;
        }

        private readonly record struct OccluderEdgeKey(long AX, long AY, long BX, long BY)
        {
            public static OccluderEdgeKey From(Vector4 edge)
            {
                return From(new Vector2(edge.X, edge.Y), new Vector2(edge.Z, edge.W));
            }

            private static OccluderEdgeKey From(Vector2 a, Vector2 b)
            {
                var ax = Quantize(a.X);
                var ay = Quantize(a.Y);
                var bx = Quantize(b.X);
                var by = Quantize(b.Y);

                if (ax > bx || ax == bx && ay > by)
                    return new OccluderEdgeKey(bx, by, ax, ay);

                return new OccluderEdgeKey(ax, ay, bx, by);
            }

            private static long Quantize(float value)
            {
                // We don't want fp inaccuracies to cause issues with edges not being considered together.
                return (long) MathF.Round(value / SharedOccluderEdgeTolerance);
            }

        }

        private readonly record struct OccluderVertexKey(long X, long Y)
        {
            public static OccluderVertexKey From(Vector2 vertex)
            {
                return new OccluderVertexKey(Quantize(vertex.X), Quantize(vertex.Y));
            }

            private static long Quantize(float value)
            {
                return (long) MathF.Round(value / SharedOccluderEdgeTolerance);
            }
        }

        private struct BoundaryVertexDirections
        {
            public Vector2 Incoming;
            public Vector2 Outgoing;
            public int IncomingCount;
            public int OutgoingCount;
        }

        private readonly record struct OccluderRenderEntry(int EdgeOffset, int EdgeCount);

        private static float SignedArea(ReadOnlySpan<Vector2> vertices)
        {
            var area = 0f;
            for (var i = 0; i < vertices.Length; i++)
            {
                var j = (i + 1) % vertices.Length;
                area += vertices[i].X * vertices[j].Y;
                area -= vertices[i].Y * vertices[j].X;
            }

            return area * 0.5f;
        }

        private static Vector4 EdgeToVector4(Vector2 a, Vector2 b)
        {
            return new Vector4(a.X, a.Y, b.X, b.Y);
        }

        private void UpdateOcclusionGeometry(MapId map, Box2 expandedBounds, Vector2 eyePosition)
        {
            using var _ = _prof.Group("UpdateOcclusionGeometry");
            using var _p = DebugGroup(nameof(UpdateOcclusionGeometry));

            var xforms = _entityManager.GetEntityQuery<TransformComponent>();
            var sharedBoundaryEdges = _occluderSharedBoundaryEdges;
            var boundarySegments = _occluderBoundarySegments;
            var visibleBoundaryVertices = _occluderVisibleBoundaryVertices;
            var convexBoundaryVertices = _occluderConvexBoundaryVertices;
            var boundaryVertexDirections = _occluderBoundaryVertexDirections;
            var sharedVertexEdges = _occluderSharedVertexEdges;
            var uniqueSharedEdges = _occluderUniqueSharedEdges;
            var staleSharedVertices = _occluderStaleSharedVertices;

            sharedBoundaryEdges.Clear();
            boundarySegments.Clear();
            visibleBoundaryVertices.Clear();
            _occluderRenderEntries.Clear();
            _occluderRenderVertices.Clear();
            _occluderRenderEdges.Clear();
            _occluderRenderSharedEdges.Clear();

            BuildFrameOccluderGeometry(map, expandedBounds, xforms);

            BuildSharedVertexEdges(
                boundarySegments,
                sharedBoundaryEdges,
                sharedVertexEdges,
                uniqueSharedEdges,
                staleSharedVertices);
            BuildConvexBoundaryVertices(
                boundarySegments,
                sharedBoundaryEdges,
                boundaryVertexDirections,
                convexBoundaryVertices);

            UploadSourceOcclusionDepthGeometry(eyePosition);
        }

        private void BuildFrameOccluderGeometry(
            MapId map,
            Box2 expandedBounds,
            EntityQuery<TransformComponent> xforms)
        {
            // This builds source-independent frame geometry:
            // - exact occluder edges, later classified into source-specific depth geometry using master's rule;
            // - flat 2D mask geometry used to apply wall bleed.
            var maxDepthFaces = _maxOccluders * PhysicsConstants.MaxPolygonVertices;
            var maxMaskVertices = _maxOccluders * PhysicsConstants.MaxPolygonVertices;
            var maxMaskIndices = _maxOccluders * (PhysicsConstants.MaxPolygonVertices - 2) * 3;
            var arrayMaskBuffer = ArrayPool<Vector2>.Shared.Rent(maxMaskVertices);
            var indexMaskBuffer = ArrayPool<ushort>.Shared.Rent(maxMaskIndices);

            var ami = 0;
            var imi = 0;
            var occluderCount = 0;
            var geometryFull = false;

            bool TryWriteMaskPolygon(int vertexOffset, int vertexCount)
            {
                // Wall bleed uses a flat 2D mask of occupied occluder area.
                // Convex occluders are serialized through the physics hull, so a simple fan is sufficient.
                if (vertexCount < 3)
                    return true;

                var indexCount = (vertexCount - 2) * 3;
                if (ami + vertexCount > arrayMaskBuffer.Length || imi + indexCount > indexMaskBuffer.Length)
                    return false;

                var amiBase = ami;
                for (var i = 0; i < vertexCount; i++)
                {
                    arrayMaskBuffer[ami++] = _occluderRenderVertices[vertexOffset + i];
                }

                for (var i = 1; i < vertexCount - 1; i++)
                {
                    indexMaskBuffer[imi++] = (ushort) amiBase;
                    indexMaskBuffer[imi++] = (ushort) (amiBase + i);
                    indexMaskBuffer[imi++] = (ushort) (amiBase + i + 1);
                }

                return true;
            }

            bool TryCacheDepthEdges(int vertexOffset, int vertexCount, byte sharedEdgeMask)
            {
                if (vertexCount < 3)
                    return true;

                var remainingFaces = maxDepthFaces - _occluderRenderEdges.Count;
                if (remainingFaces < vertexCount)
                    return false;

                var renderVertices = CollectionsMarshal.AsSpan(_occluderRenderVertices).Slice(vertexOffset, vertexCount);
                var edgeOffset = _occluderRenderEdges.Count;
                for (var i = 0; i < vertexCount; i++)
                {
                    var edge = EdgeToVector4(renderVertices[i], renderVertices[(i + 1) % vertexCount]);
                    _occluderRenderEdges.Add(edge);
                    _occluderRenderSharedEdges.Add((sharedEdgeMask & 1 << i) != 0);
                }

                _occluderRenderEntries.Add(new OccluderRenderEntry(edgeOffset, vertexCount));
                return true;
            }

            try
            {
                // Include one tile around the rendered area so shared corners on the edge of the viewport have
                // complete topology. Visible geometry is filtered back to expandedBounds below.
                var boundaryBounds = expandedBounds.Enlarged(SharedOccluderNeighbourQueryPadding);
                foreach (var (uid, comp) in _occluderSystem.GetIntersectingTrees(map, boundaryBounds))
                {
                    var treeBounds = _transformSystem.GetInvWorldMatrix(uid, xforms).TransformBox(boundaryBounds);

                    comp.Tree.QueryAabb((in ComponentTreeEntry<OccluderComponent> entry) =>
                    {
                        var (occluder, transform) = entry;
                        if (!occluder.Enabled)
                            return true;

                        var polygon = occluder.Polygon;
                        if (polygon.Length < 3)
                            return true;

                        var worldTransform = _transformSystem.GetWorldMatrix(transform, xforms);

                        // Build source-dependent corner topology from the cached client-side shared edge mask.
                        AddOccluderBoundaryEdges(
                            polygon,
                            worldTransform,
                            occluder.OccludingEdges,
                            _occluderSharedBoundaryEdges,
                            _occluderBoundarySegments);

                        if (geometryFull
                            || !worldTransform.TransformBox(occluder.LocalBounds).Intersects(expandedBounds))
                        {
                            return true;
                        }

                        if (_occluderRenderEntries.Count >= _maxOccluders
                            || _occluderRenderVertices.Count + polygon.Length > maxMaskVertices
                            || imi + (polygon.Length - 2) * 3 > indexMaskBuffer.Length)
                        {
                            geometryFull = true;
                            return true;
                        }

                        var vertexOffset = _occluderRenderVertices.Count;
                        var clockwise = SignedArea(polygon) < 0f;
                        for (var i = 0; i < polygon.Length; i++)
                        {
                            var sourceIndex = clockwise ? i : polygon.Length - 1 - i;
                            var worldVertex = Vector2.Transform(polygon[sourceIndex], worldTransform);
                            _occluderRenderVertices.Add(worldVertex);
                        }

                        if (!TryWriteMaskPolygon(vertexOffset, polygon.Length))
                        {
                            geometryFull = true;
                            return true;
                        }

                        occluderCount += 1;

                        if (!TryCacheDepthEdges(vertexOffset, polygon.Length, occluder.OccludingEdges))
                        {
                            geometryFull = true;
                            return true;
                        }

                        return true;
                    }, treeBounds);
                }

                _occlusionMaskDataLength = imi;

                BindVertexArray(_occlusionMaskVao.Handle);
                CheckGlError();

                _occlusionMaskVbo.Reallocate(arrayMaskBuffer.AsSpan(0, ami));
                _occlusionMaskEbo.Reallocate(indexMaskBuffer.AsSpan(0, imi));
            }
            finally
            {
                ArrayPool<Vector2>.Shared.Return(arrayMaskBuffer);
                ArrayPool<ushort>.Shared.Return(indexMaskBuffer);
            }

            _debugStats.Occluders += occluderCount;
        }

        private void UploadSourceOcclusionDepthGeometry(Vector2 sourcePosition)
        {
            var maxDepthFaces = _occluderRenderEdges.Count;
            var maxDepthVertices = maxDepthFaces * 4;
            var maxDepthIndices = maxDepthFaces * GetQuadBatchIndexCount();

            var arrayBuffer = ArrayPool<Vector4>.Shared.Rent(maxDepthVertices);
            // multiplied by 2 (it's a vector2 of bytes)
            var arrayVIBuffer = ArrayPool<byte>.Shared.Rent(maxDepthVertices * 2);
            var indexBuffer = ArrayPool<ushort>.Shared.Rent(maxDepthIndices);

            var ai = 0;
            var avi = 0;
            var ii = 0;
            var geometryFull = false;

            var sharedBoundaryEdges = _occluderSharedBoundaryEdges;
            var boundarySegments = _occluderBoundarySegments;
            var visibleBoundaryVertices = _occluderVisibleBoundaryVertices;
            var convexBoundaryVertices = _occluderConvexBoundaryVertices;
            var sharedVertexEdges = _occluderSharedVertexEdges;

            BuildVisibleBoundaryVertices(
                boundarySegments,
                sharedBoundaryEdges,
                sourcePosition,
                visibleBoundaryVertices);

            bool TryWriteFaceOfBuffer(Vector4 vec)
            {
                if (ai + 4 > arrayBuffer.Length || ii + GetQuadBatchIndexCount() > indexBuffer.Length)
                    return false;

                var aiBase = ai;
                for (byte vi = 0; vi < 4; vi++)
                {
                    arrayBuffer[ai++] = vec;
                    // generates the sequence:
                    // DddD
                    // HHhh
                    // deflection
                    arrayVIBuffer[avi++] = (byte)((((vi + 1) & 2) != 0) ? 0 : 255);
                    // height
                    arrayVIBuffer[avi++] = (byte)(((vi & 2) != 0) ? 0 : 255);
                }

                QuadBatchIndexWrite(indexBuffer, ref ii, (ushort)aiBase);
                return true;
            }

            try
            {
                var renderEdges = CollectionsMarshal.AsSpan(_occluderRenderEdges);
                var renderSharedEdges = CollectionsMarshal.AsSpan(_occluderRenderSharedEdges);
                foreach (var entry in _occluderRenderEntries)
                {
                    if (geometryFull || ai >= maxDepthVertices)
                        break;

                    var activeEdges = renderEdges.Slice(entry.EdgeOffset, entry.EdgeCount);
                    var activeSharedEdges = renderSharedEdges.Slice(entry.EdgeOffset, entry.EdgeCount);
                    for (var i = 0; i < activeEdges.Length; i++)
                    {
                        var edge = activeEdges[i];
                        /*
                         * Okay so essentially for occlusion you draw from edges in the viewport and project it out to the edge of the screen.
                         * In our case there are some exceptions where we don't in fact want to do that because it doesn't look good.
                         * e.g. connecting walls, but only sometimes like if not a corner, or only want to do that at specific angles.
                         * Hence you get the hell that is ShouldSuppressSharedOccluderEdge.
                         *
                         * A lot of this was implicitly handled before but now that we allow entirely arbitrary occluders
                         * this needs to be handled explicitly.
                         *
                         * If you know trig you'll be right mate.
                         */

                        var suppressSharedEdge = ShouldSuppressSharedOccluderEdge(
                            i,
                            activeEdges,
                            activeSharedEdges,
                            visibleBoundaryVertices,
                            convexBoundaryVertices,
                            sharedVertexEdges,
                            sourcePosition);

                        if (suppressSharedEdge)
                            continue;

                        if (!TryWriteFaceOfBuffer(edge))
                        {
                            geometryFull = true;
                            break;
                        }
                    }
                }

                _occlusionDataLength = ii;

                BindVertexArray(_occlusionVao.Handle);
                CheckGlError();

                _occlusionVbo.Reallocate(arrayBuffer.AsSpan(0, ai));
                _occlusionVIVbo.Reallocate(arrayVIBuffer.AsSpan(0, avi));
                _occlusionEbo.Reallocate(indexBuffer.AsSpan(0, ii));
            }
            finally
            {
                ArrayPool<Vector4>.Shared.Return(arrayBuffer);
                ArrayPool<byte>.Shared.Return(arrayVIBuffer);
                ArrayPool<ushort>.Shared.Return(indexBuffer);
            }
        }

        private void RegenLightRts(Viewport viewport)
        {
            // All of these depend on screen size so they have to be re-created if it changes.

            var lightMapSize = GetLightMapSize(viewport.Size);
            var lightMapSizeQuart = GetLightMapSize(viewport.Size, true);

            viewport.LightRenderTarget?.Dispose();
            viewport.WallMaskRenderTarget?.Dispose();
            viewport.WallBleedIntermediateRenderTarget1?.Dispose();
            viewport.WallBleedIntermediateRenderTarget2?.Dispose();
            var lightMapColorFormat = _hasGLFloatFramebuffers
                ? RenderTargetColorFormat.R11FG11FB10F
                : RenderTargetColorFormat.Rgba8;
            var lightMapSampleParameters = new TextureSampleParameters { Filter = true };

            viewport.WallMaskRenderTarget = CreateRenderTarget(viewport.Size, RenderTargetColorFormat.R8,
                name: $"{viewport.Name}-{nameof(viewport.WallMaskRenderTarget)}");

            viewport.LightRenderTarget = (RenderTexture) CreateLightRenderTarget(lightMapSize,
                $"{viewport.Name}-{nameof(viewport.LightRenderTarget)}");

            viewport.LightBlurTarget = CreateRenderTarget(lightMapSize,
                new RenderTargetFormatParameters(lightMapColorFormat),
                lightMapSampleParameters,
                $"{viewport.Name}-{nameof(viewport.LightBlurTarget)}");

            viewport.WallBleedIntermediateRenderTarget1 = CreateRenderTarget(lightMapSizeQuart,
                new RenderTargetFormatParameters(lightMapColorFormat),
                lightMapSampleParameters,
                $"{viewport.Name}-{nameof(viewport.WallBleedIntermediateRenderTarget1)}");

            viewport.WallBleedIntermediateRenderTarget2 = CreateRenderTarget(lightMapSizeQuart,
                new RenderTargetFormatParameters(lightMapColorFormat),
                lightMapSampleParameters,
                $"{viewport.Name}-{nameof(viewport.WallBleedIntermediateRenderTarget2)}");
        }

        private void RegenAllLightRts()
        {
            foreach (var viewportRef in _viewports.Values)
            {
                if (viewportRef.TryGetTarget(out var viewport))
                {
                    RegenLightRts(viewport);
                }
            }
        }

        private Vector2i GetLightMapSize(Vector2i screenSize, bool furtherDivide = false)
        {
            var scale = _lightResolutionScale;
            if (furtherDivide)
            {
                scale /= 2;
            }

            var w = (int)Math.Ceiling(screenSize.X * scale);
            var h = (int)Math.Ceiling(screenSize.Y * scale);

            return (w, h);
        }

        private void LightResolutionScaleChanged(float newValue)
        {
            _lightResolutionScale = newValue > 0.05f ? newValue : 0.05f;
            RegenAllLightRts();
        }

        private void MaxShadowcastingLightsChanged(int newValue)
        {
            _maxShadowcastingLights = newValue;
            DebugTools.Assert(_maxLights >= _maxShadowcastingLights);

            // This guard is in place because otherwise the shadow FBO is initialized before GL is initialized.
            if (!_shadowRenderTargetCanInitializeSafely)
                return;

            if (_shadowRenderTarget != null)
            {
                DeleteRenderTexture(_shadowRenderTarget.Handle);
            }

            // Shadow FBO.
            _shadowRenderTarget = CreateRenderTarget((ShadowMapSize, _maxShadowcastingLights),
                new RenderTargetFormatParameters(
                    _hasGLFloatFramebuffers ? RenderTargetColorFormat.RG32F : RenderTargetColorFormat.Rgba8, true),
                new TextureSampleParameters { WrapMode = TextureWrapMode.Repeat, Filter = true },
                nameof(_shadowRenderTarget));
        }

        private void SoftShadowsChanged(bool newValue)
        {
            _enableSoftShadows = newValue;
        }

        private void MaxOccludersChanged(int value)
        {
            _maxOccluders = Math.Max(value, 1024);
        }

        private void MaxLightsChanged(int value)
        {
            _maxLights = value;
            _lightsToRenderList = new LightRenderData[value];
            DebugTools.Assert(_maxLights >= _maxShadowcastingLights);
        }
    }
}
