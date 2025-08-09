using System;
using System.Collections.Generic;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Numerics;
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
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using static Robust.Shared.GameObjects.OccluderComponent;
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

        private (PointLightComponent light, Vector2 pos, float distanceSquared, Angle rot)[] _lightsToRenderList = default!;

        private LightCapacityComparer _lightCap = new();
        private ShadowCapacityComparer _shadowCap = new ShadowCapacityComparer();

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
                GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, sizeof(Vector4d), IntPtr.Zero);
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
        /// </param>
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

            eye.GetViewMatrixNoOffset(out var eyeTransform, eye.Scale);

            UpdateOcclusionGeometry(mapId, expandedBounds, eyeTransform);

            DrawFov(viewport, eye);

            if (!_lightManager.DrawLighting)
            {
                BindRenderTargetFull(viewport.RenderTarget);
                GL.Viewport(0, 0, viewport.Size.X, viewport.Size.Y);
                CheckGlError();
    
                // If we don't draw lighting, we still need to apply the FOV to the buffer.
                IsStencilling = true;
                ApplyFovToBuffer(viewport, eye);
                IsStencilling = false;
                
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
                        var (light, lightPos, _, _) = _lightsToRenderList[i];

                        if (!light.CastShadows) continue;

                        DrawOcclusionDepth(lightPos, ShadowMapSize, light.Radius, i);
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
            Texture? lastMask = null;

            using (_prof.Group("Draw Lights"))
            {
                for (var i = 0; i < count; i++)
                {
                    var (component, lightPos, _, rot) = _lightsToRenderList[i];

                    Texture? mask = null;
                    var rotation = Angle.Zero;
                    if (component.Mask != null)
                    {
                        mask = component.Mask;
                        rotation = component.Rotation;

                        if (component.MaskAutoRotate)
                        {
                            rotation += rot;
                        }
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

                    lightShader.SetUniformMaybe("lightCenter", lightPos);
                    lightShader.SetUniformMaybe("lightIndex",
                        component.CastShadows ? (i + 0.5f) / ShadowTexture.Height : -1);

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
            int count,
            int shadowCastingCount,
            EntityQuery<TransformComponent> xforms,
            Box2 worldAABB) state,
            in ComponentTreeEntry<PointLightComponent> value)
        {
            ref var count = ref state.count;
            ref var shadowCount = ref state.shadowCastingCount;

            // If there are too many lights, exit the query
            if (count >= state.clyde._maxLights)
                return false;

            var (light, transform) = value;
            var (lightPos, rot) = state.clyde._transformSystem.GetWorldPositionRotation(transform, state.xforms);
            lightPos += rot.RotateVec(light.Offset);
            var circle = new Circle(lightPos, light.Radius);

            // If the light doesn't touch anywhere the camera can see, it doesn't matter.
            // The tree query is not fully accurate because the viewport may be rotated relative to a grid.
            if (!circle.Intersects(state.worldAABB))
                return true;

            // If the light is a shadow casting light, keep a separate track of that
            if (light.CastShadows)
                shadowCount++;

            var distanceSquared = (state.worldAABB.Center - lightPos).LengthSquared();
            state.clyde._lightsToRenderList[count++] = (light, lightPos, distanceSquared, rot);

            return true;
        }

        private sealed class LightCapacityComparer : IComparer<(PointLightComponent light, Vector2 pos, float distanceSquared, Angle rot)>
        {
            public int Compare(
                (PointLightComponent light, Vector2 pos, float distanceSquared, Angle rot) x,
                (PointLightComponent light, Vector2 pos, float distanceSquared, Angle rot) y)
            {
                if (x.light.CastShadows && !y.light.CastShadows) return 1;
                if (!x.light.CastShadows && y.light.CastShadows) return -1;
                return 0;
            }
        }

        private sealed class ShadowCapacityComparer : IComparer<(PointLightComponent light, Vector2 pos, float distanceSquared, Angle rot)>
        {
            public int Compare(
                (PointLightComponent light, Vector2 pos, float distanceSquared, Angle rot) x,
                (PointLightComponent light, Vector2 pos, float distanceSquared, Angle rot) y)
            {
                return x.distanceSquared.CompareTo(y.distanceSquared);
            }
        }

        private (int count, Box2 expandedBounds) GetLightsToRender(
            MapId map,
            in Box2Rotated worldBounds,
            in Box2 worldAABB)
        {
            // Use worldbounds for this one as we only care if the light intersects our actual bounds
            var xforms = _entityManager.GetEntityQuery<TransformComponent>();
            var state = (this, count: 0, shadowCastingCount: 0, xforms, worldAABB);
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
                expandedBounds = expandedBounds.ExtendToContain(_lightsToRenderList[i].pos);
            }

            _debugStats.TotalLights += state.count;
            _debugStats.ShadowLights += Math.Min(state.shadowCastingCount, _maxShadowcastingLights);

            return (state.count, expandedBounds);
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

            GL.DrawElements(GetQuadGLPrimitiveType(), _occlusionMaskDataLength, DrawElementsType.UnsignedShort,
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

        private readonly struct OccluderData
        {
            public readonly OccluderComponent Occluder;
            public readonly Vector2 TopLeft, TopRight, BottomRight, BottomLeft;
            
            public OccluderData(OccluderComponent occluder, Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl)
            {
                Occluder = occluder;
                TopLeft = tl;
                TopRight = tr;
                BottomRight = br;
                BottomLeft = bl;
            }
        }

        private void UpdateOcclusionGeometry(MapId map, Box2 expandedBounds, Matrix3x2 eyeTransform)
        {
            using var _ = _prof.Group("UpdateOcclusionGeometry");
            using var _p = DebugGroup(nameof(UpdateOcclusionGeometry));

            var arrayBuffer = ArrayPool<Vector4d>.Shared.Rent(_maxOccluders * 16);
            var arrayVIBuffer = ArrayPool<byte>.Shared.Rent(_maxOccluders * 32);
            var indexBuffer = ArrayPool<ushort>.Shared.Rent(_maxOccluders * GetQuadBatchIndexCount() * 4);
            var arrayMaskBuffer = ArrayPool<Vector2>.Shared.Rent(_maxOccluders * 4);
            var indexMaskBuffer = ArrayPool<ushort>.Shared.Rent(_maxOccluders * GetQuadBatchIndexCount());

            var ai = 0;
            var avi = 0;
            var ami = 0;
            var ii = 0;
            var imi = 0;
            var amiMax = _maxOccluders * 4;
            var xforms = _entityManager.GetEntityQuery<TransformComponent>();

            try
            {
                var occluders = CollectOccluders(map, expandedBounds, amiMax, xforms);
                
                for (int i = 0; i < occluders.Count; i++)
                {
                    var data = occluders[i];
                    var neighbors = DetectNeighbors(data, occluders, i);
                    
                    GenerateOccluderGeometry(
                        data, neighbors, eyeTransform,
                        arrayBuffer, arrayVIBuffer, indexBuffer, arrayMaskBuffer, indexMaskBuffer,
                        ref ai, ref avi, ref ami, ref ii, ref imi);
                }

                _occlusionDataLength = ii;
                _occlusionMaskDataLength = imi;

                UploadOcclusionData(arrayBuffer, ai, arrayVIBuffer, avi, indexBuffer, ii, 
                                   arrayMaskBuffer, ami, indexMaskBuffer, imi);
            }
            finally
            {
                ArrayPool<Vector4d>.Shared.Return(arrayBuffer);
                ArrayPool<byte>.Shared.Return(arrayVIBuffer);
                ArrayPool<ushort>.Shared.Return(indexBuffer);
                ArrayPool<Vector2>.Shared.Return(arrayMaskBuffer);
                ArrayPool<ushort>.Shared.Return(indexMaskBuffer);
            }

            _debugStats.Occluders += ami / 4;
        }

        private List<OccluderData> CollectOccluders(MapId map, Box2 expandedBounds, int maxCount, 
                                                    EntityQuery<TransformComponent> xforms)
        {
            var occluders = new List<OccluderData>();
            var count = 0;

            foreach (var (uid, comp) in _occluderSystem.GetIntersectingTrees(map, expandedBounds))
            {
                if (count >= maxCount) break;

                var treeBounds = _transformSystem.GetInvWorldMatrix(uid).TransformBox(expandedBounds);
                
                comp.Tree.QueryAabb((in ComponentTreeEntry<OccluderComponent> entry) =>
                {
                    if (count >= maxCount) return false;
                    
                    var (occluder, transform) = entry;
                    if (!occluder.Enabled) return true;

                    var worldTransform = _transformSystem.GetWorldMatrix(transform, xforms);
                    var box = occluder.BoundingBox;
                    
                    var tl = Vector2.Transform(box.TopLeft, worldTransform);
                    var tr = Vector2.Transform(box.TopRight, worldTransform);
                    var br = Vector2.Transform(box.BottomRight, worldTransform);
                    var bl = tl + br - tr;
                    
                    occluders.Add(new OccluderData(occluder, tl, tr, br, bl));
                    count += 4;
                    
                    return true;
                }, treeBounds);
            }
            
            return occluders;
        }

        private OccluderDir DetectNeighbors(OccluderData current, List<OccluderData> all, int currentIndex)
        {
            const float tolerance = 0.01f;
            var neighbors = OccluderDir.None;
            
            for (int i = 0; i < all.Count; i++)
            {
                if (i == currentIndex) continue;
                
                var other = all[i];
                
                if (EdgesMatch(current.TopLeft, current.TopRight, other.BottomLeft, other.BottomRight, tolerance))
                    neighbors |= OccluderDir.North;
                    
                if (EdgesMatch(current.BottomLeft, current.BottomRight, other.TopLeft, other.TopRight, tolerance))
                    neighbors |= OccluderDir.South;
                    
                if (EdgesMatch(current.TopRight, current.BottomRight, other.TopLeft, other.BottomLeft, tolerance))
                    neighbors |= OccluderDir.East;
                    
                if (EdgesMatch(current.TopLeft, current.BottomLeft, other.TopRight, other.BottomRight, tolerance))
                    neighbors |= OccluderDir.West;
            }
            
            return neighbors;
        }

        private static bool EdgesMatch(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, float tolerance)
        {
            var vertical = Math.Abs(a1.X - a2.X) < tolerance;
            
            if (vertical)
            {
                return Math.Abs(a1.X - b1.X) < tolerance && 
                       Math.Max(a1.Y, b1.Y) < Math.Min(a2.Y, b2.Y);
            }
            
            return Math.Abs(a1.Y - b1.Y) < tolerance && 
                   Math.Max(a1.X, b1.X) < Math.Min(a2.X, b2.X);
        }

        private void GenerateOccluderGeometry(
            OccluderData data, OccluderDir neighbors, Matrix3x2 eyeTransform,
            Vector4d[] arrayBuffer, byte[] arrayVIBuffer, ushort[] indexBuffer,
            Vector2[] arrayMaskBuffer, ushort[] indexMaskBuffer,
            ref int ai, ref int avi, ref int ami, ref int ii, ref int imi)
        {
            var faces = new[]
            {
                new Vector4d(data.TopLeft.X, data.TopLeft.Y, data.TopRight.X, data.TopRight.Y),
                new Vector4d(data.TopRight.X, data.TopRight.Y, data.BottomRight.X, data.BottomRight.Y),
                new Vector4d(data.BottomRight.X, data.BottomRight.Y, data.BottomLeft.X, data.BottomLeft.Y),
                new Vector4d(data.BottomLeft.X, data.BottomLeft.Y, data.TopLeft.X, data.TopLeft.Y)
            };

            var deltas = new[]
            {
                Vector2.Transform(data.TopLeft, eyeTransform),
                Vector2.Transform(data.TopRight, eyeTransform),
                Vector2.Transform(data.BottomLeft, eyeTransform),
                Vector2.Transform(data.BottomRight, eyeTransform)
            };
            deltas[3] = deltas[2] + deltas[1] - deltas[0];

            var hasNeighbor = new[]
            {
                (neighbors & OccluderDir.North) != 0,
                (neighbors & OccluderDir.East) != 0,
                (neighbors & OccluderDir.South) != 0,
                (neighbors & OccluderDir.West) != 0
            };

            // FOV logic: faces are only generated if no neighbor or both corners aren't visible
            var faceVisibility = new[]
            {
                !hasNeighbor[0] && CheckFaceEyeVis(deltas[0], deltas[1]),
                !hasNeighbor[1] && CheckFaceEyeVis(deltas[1], deltas[3]),
                !hasNeighbor[2] && CheckFaceEyeVis(deltas[3], deltas[2]),
                !hasNeighbor[3] && CheckFaceEyeVis(deltas[2], deltas[0])
            };

            var cornerVisibility = new[]
            {
                faceVisibility[0] || faceVisibility[3], // TL
                faceVisibility[0] || faceVisibility[1], // TR
                faceVisibility[2] || faceVisibility[3], // BL
                faceVisibility[2] || faceVisibility[1]  // BR
            };

            var shouldGenerateFace = new[]
            {
                !hasNeighbor[0] || (!cornerVisibility[0] && !cornerVisibility[1]),
                !hasNeighbor[1] || (!cornerVisibility[1] && !cornerVisibility[3]),
                !hasNeighbor[2] || (!cornerVisibility[3] && !cornerVisibility[2]),
                !hasNeighbor[3] || (!cornerVisibility[2] && !cornerVisibility[0])
            };

            for (int dir = 0; dir < 4; dir++)
            {
                if (shouldGenerateFace[dir])
                {
                    WriteFaceBuffer(faces[dir], arrayBuffer, arrayVIBuffer, indexBuffer, ref ai, ref avi, ref ii);
                }
            }

            // Generate mask geometry
            arrayMaskBuffer[ami] = new Vector2(data.TopLeft.X, data.TopLeft.Y);
            arrayMaskBuffer[ami + 1] = new Vector2(data.TopRight.X, data.TopRight.Y);
            arrayMaskBuffer[ami + 2] = new Vector2(data.BottomRight.X, data.BottomRight.Y);
            arrayMaskBuffer[ami + 3] = new Vector2(data.BottomLeft.X, data.BottomLeft.Y);
            
            QuadBatchIndexWrite(indexMaskBuffer, ref imi, (ushort)ami);
            ami += 4;

            static bool CheckFaceEyeVis(Vector2 a, Vector2 b) => a.X * b.Y > a.Y * b.X;
        }

        private void WriteFaceBuffer(Vector4d face, Vector4d[] arrayBuffer, byte[] arrayVIBuffer, 
                                     ushort[] indexBuffer, ref int ai, ref int avi, ref int ii)
        {
            var aiBase = ai;
            for (byte vi = 0; vi < 4; vi++)
            {
                arrayBuffer[ai++] = face;
                arrayVIBuffer[avi++] = (byte)((((vi + 1) & 2) != 0) ? 0 : 255);
                arrayVIBuffer[avi++] = (byte)(((vi & 2) != 0) ? 0 : 255);
            }
            QuadBatchIndexWrite(indexBuffer, ref ii, (ushort)aiBase);
        }

        private void UploadOcclusionData(Vector4d[] arrayBuffer, int ai, byte[] arrayVIBuffer, int avi,
                                         ushort[] indexBuffer, int ii, Vector2[] arrayMaskBuffer, int ami,
                                         ushort[] indexMaskBuffer, int imi)
        {
            BindVertexArray(_occlusionVao.Handle);
            CheckGlError();

            _occlusionVbo.Reallocate(arrayBuffer.AsSpan(0, ai));
            _occlusionVIVbo.Reallocate(arrayVIBuffer.AsSpan(0, avi));
            _occlusionEbo.Reallocate(indexBuffer.AsSpan(0, ii));

            BindVertexArray(_occlusionMaskVao.Handle);
            CheckGlError();

            _occlusionMaskVbo.Reallocate(arrayMaskBuffer.AsSpan(0, ami));
            _occlusionMaskEbo.Reallocate(indexMaskBuffer.AsSpan(0, imi));
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
            _lightsToRenderList = new (PointLightComponent, Vector2, float , Angle)[value];
            DebugTools.Assert(_maxLights >= _maxShadowcastingLights);
        }
    }
}
