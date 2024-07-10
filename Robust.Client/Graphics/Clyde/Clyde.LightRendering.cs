using System;
using System.Collections.Generic;
using System.Buffers;
using System.Numerics;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using OGLTextureWrapMode = OpenToolkit.Graphics.OpenGL.TextureWrapMode;
using TKStencilOp = OpenToolkit.Graphics.OpenGL4.StencilOp;
using Robust.Shared.Physics;
using Robust.Client.ComponentTrees;
using Robust.Shared.Graphics;
using static Robust.Shared.GameObjects.OccluderComponent;
using Robust.Shared.Utility;
using TextureWrapMode = Robust.Shared.Graphics.TextureWrapMode;
using Vector4 = Robust.Shared.Maths.Vector4;

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

        private unsafe void InitLighting()
        {


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

                Logger.Warning($"Can't load shader {path}\n");
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

            GL.Disable(EnableCap.Blend);
            CheckGlError();

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

            GL.Enable(EnableCap.Blend);
            CheckGlError();
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
            var mapUid = _mapManager.GetMapEntityId(mapId);
            if (!_entityManager.TryGetComponent<MapComponent>(mapUid, out var map) || !map.LightingEnabled)
            {
                return;
            }

            (PointLightComponent light, Vector2 pos, float distanceSquared, Angle rot)[] lights;
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

            GL.Enable(EnableCap.StencilTest);
            _isStencilling = true;

            var (lightW, lightH) = GetLightMapSize(viewport.Size);
            GL.Viewport(0, 0, lightW, lightH);
            CheckGlError();

            BindRenderTargetImmediate(RtToLoaded(viewport.LightRenderTarget));
            CheckGlError();
            GLClearColor(_entityManager.GetComponentOrNull<MapLightComponent>(mapUid)?.AmbientLightColor ?? MapLightComponent.DefaultColor);
            GL.ClearStencil(0xFF);
            GL.StencilMask(0xFF);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit);
            CheckGlError();

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
            GL.Disable(EnableCap.StencilTest);
            _isStencilling = false;

            CheckGlError();

            if (_cfg.GetCVar(CVars.LightBlur))
                BlurLights(viewport, eye);

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

            Array.Clear(_lightsToRenderList, 0, count);

            _lightingReady = true;
        }

        private static bool LightQuery(ref (
            Clyde clyde,
            int count,
            int shadowCastingCount,
            TransformSystem xformSystem,
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
            var (lightPos, rot) = state.xformSystem.GetWorldPositionRotation(transform, state.xforms);
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
            var lightTreeSys = _entitySystemManager.GetEntitySystem<LightTreeSystem>();
            var xformSystem = _entitySystemManager.GetEntitySystem<TransformSystem>();

            // Use worldbounds for this one as we only care if the light intersects our actual bounds
            var xforms = _entityManager.GetEntityQuery<TransformComponent>();
            var state = (this, count: 0, shadowCastingCount: 0, xformSystem, xforms, worldAABB);

            foreach (var (uid, comp) in lightTreeSys.GetIntersectingTrees(map, worldAABB))
            {
                var bounds = xformSystem.GetInvWorldMatrix(uid, xforms).TransformBox(worldBounds);
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

        private void BlurLights(Viewport viewport, IEye eye)
        {
            using var _ = DebugGroup(nameof(BlurLights));

            GL.Disable(EnableCap.Blend);
            CheckGlError();
            CalcScreenMatrices(viewport.Size, out var proj, out var view);
            SetProjViewBuffer(proj, view);

            var shader = _loadedShaders[_lightBlurShaderHandle].Program;
            shader.Use();

            SetupGlobalUniformsImmediate(shader, viewport.LightRenderTarget.Texture);

            var size = viewport.LightRenderTarget.Size;
            shader.SetUniformMaybe("size", (Vector2)size);
            shader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            GL.Viewport(0, 0, size.X, size.Y);
            CheckGlError();

            // Initially we're pulling from the light render target.
            // So we set it out of the loop so
            // _wallBleedIntermediateRenderTarget2 gets bound at the end of the loop body.
            SetTexture(TextureUnit.Texture0, viewport.LightRenderTarget.Texture);

            // Have to scale the blurring radius based on viewport size and camera zoom.
            const float refCameraHeight = 14;
            var facBase = _cfg.GetCVar(CVars.LightBlurFactor);
            var cameraSize = eye.Zoom.Y * viewport.Size.Y * (1 / viewport.RenderScale.Y) / EyeManager.PixelsPerMeter;
            // 7e-3f is just a magic factor that makes it look ok.
            var factor = facBase * (refCameraHeight / cameraSize);

            // Multi-iteration gaussian blur.
            for (var i = 3; i > 0; i--)
            {
                var scale = (i + 1) * factor;
                // Set factor.
                shader.SetUniformMaybe("radius", scale);

                BindRenderTargetFull(viewport.LightBlurTarget);

                // Blur horizontally to _wallBleedIntermediateRenderTarget1.
                shader.SetUniformMaybe("direction", Vector2.UnitX);
                _drawQuad(Vector2.Zero, viewport.Size, Matrix3x2.Identity, shader);

                SetTexture(TextureUnit.Texture0, viewport.LightBlurTarget.Texture);

                BindRenderTargetFull(viewport.LightRenderTarget);

                // Blur vertically to _wallBleedIntermediateRenderTarget2.
                shader.SetUniformMaybe("direction", Vector2.UnitY);
                _drawQuad(Vector2.Zero, viewport.Size, Matrix3x2.Identity, shader);

                SetTexture(TextureUnit.Texture0, viewport.LightRenderTarget.Texture);
            }

            GL.Enable(EnableCap.Blend);
            CheckGlError();
            // We didn't trample over the old _currentMatrices so just roll it back.
            SetProjViewBuffer(_currentMatrixProj, _currentMatrixView);
        }

        private void BlurOntoWalls(Viewport viewport, IEye eye)
        {
            using var _ = DebugGroup(nameof(BlurOntoWalls));

            GL.Disable(EnableCap.Blend);
            CheckGlError();
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

            GL.Enable(EnableCap.Blend);
            CheckGlError();
            // We didn't trample over the old _currentMatrices so just roll it back.
            SetProjViewBuffer(_currentMatrixProj, _currentMatrixView);
        }

        private void MergeWallLayer(Viewport viewport)
        {
            using var _ = DebugGroup(nameof(MergeWallLayer));

            BindRenderTargetFull(viewport.LightRenderTarget);

            GL.Viewport(0, 0, viewport.LightRenderTarget.Size.X, viewport.LightRenderTarget.Size.Y);
            CheckGlError();
            GL.Disable(EnableCap.Blend);
            CheckGlError();

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

            GL.Enable(EnableCap.Blend);
            CheckGlError();
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
            GL.Disable(EnableCap.StencilTest);
            _isStencilling = false;
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

        private void UpdateOcclusionGeometry(MapId map, Box2 expandedBounds, Matrix3x2 eyeTransform)
        {
            using var _ = _prof.Group("UpdateOcclusionGeometry");
            using var _p = DebugGroup(nameof(UpdateOcclusionGeometry));

            // This method generates two sets of occlusion geometry:
            // 3D geometry used during depth projection.
            // 2D mask geometry used to apply wall bleed.

            // 16 = 4 vertices * 4 directions
            var arrayBuffer = ArrayPool<Vector4>.Shared.Rent(_maxOccluders * 4 * 4);
            // multiplied by 2 (it's a vector2 of bytes)
            var arrayVIBuffer = ArrayPool<byte>.Shared.Rent(_maxOccluders * 2 * 4 * 4);
            var indexBuffer = ArrayPool<ushort>.Shared.Rent(_maxOccluders * GetQuadBatchIndexCount() * 4);

            var arrayMaskBuffer = ArrayPool<Vector2>.Shared.Rent(_maxOccluders * 4);
            var indexMaskBuffer = ArrayPool<ushort>.Shared.Rent(_maxOccluders * GetQuadBatchIndexCount());

            // I love mysterious variable names, it keeps you on your toes.
            var ai = 0;
            var avi = 0;
            var ami = 0;
            var ii = 0;
            var imi = 0;
            var amiMax = _maxOccluders * 4;

            var occluderSystem = _entitySystemManager.GetEntitySystem<OccluderSystem>();
            var xformSystem = _entitySystemManager.GetEntitySystem<TransformSystem>();
            var xforms = _entityManager.GetEntityQuery<TransformComponent>();

            try
            {
                foreach (var (uid, comp) in occluderSystem.GetIntersectingTrees(map, expandedBounds))
                {
                    if (ami >= amiMax)
                        break;

                    var treeBounds = xforms.GetComponent(uid).InvWorldMatrix.TransformBox(expandedBounds);

                    comp.Tree.QueryAabb((in ComponentTreeEntry<OccluderComponent> entry) =>
                    {
                        var (occluder, transform) = entry;
                        if (!occluder.Enabled)
                        {
                            return true;
                        }

                        if (ami >= amiMax)
                            return false;

                        var worldTransform = xformSystem.GetWorldMatrix(transform, xforms);
                        var box = occluder.BoundingBox;

                        var tl = Vector2.Transform(box.TopLeft, worldTransform);
                        var tr = Vector2.Transform(box.TopRight, worldTransform);
                        var br = Vector2.Transform(box.BottomRight, worldTransform);
                        var bl = tl + br - tr;

                        // Faces.
                        var faceN = new Vector4(tl.X, tl.Y, tr.X, tr.Y);
                        var faceE = new Vector4(tr.X, tr.Y, br.X, br.Y);
                        var faceS = new Vector4(br.X, br.Y, bl.X, bl.Y);
                        var faceW = new Vector4(bl.X, bl.Y, tl.X, tl.Y);

                        //
                        // Buckle up.
                        // For the front-face culled final FOV to work, we obviously cannot have faces inside a series
                        // of walls that are perpendicular to you.
                        // This next code does that by only writing render indices for faces that should be rendered.
                        //

                        //
                        // Keep in mind, a face only blocks light from *leaving* from the back.
                        // It does not block light entering.
                        //
                        // So first rule: a face always exists if there's no neighboring occluder in that direction.
                        // Can't have holes after all.
                        // Second rule: otherwise, if either vertex of the face is "visible" from the camera,
                        // we don't draw the face.
                        // This visibility check is significantly more simple and resourceful than you might think.
                        // A corner becomes "occluded" if it's not visible from either cardinal direction it's on.
                        // So a the top right corner is occluded if there's something blocking visibility
                        // on the top AND right.
                        // This "occluded in direction" check has two parts: whether this is a neighboring occluder (duh)
                        // And whether the is in that direction of the corner.
                        // (so a corner on the back of a wall is occluded because the camera is position on the other side).
                        //
                        // You'll notice that in some cases like corner walls, ALL corners are marked "occluded".
                        // This is fine! The occlusion only blocks incoming light,
                        // and the neighboring walls DO treat those corners as visible.
                        // Yes, you cannot share the handling of overlapping corners of two aligned neighboring occluders.
                        // They still have different potential behavior, keeps the code simple(ish).
                        //

                        // Calculate delta positions from camera.
                        var dTl = Vector2.Transform(tl, eyeTransform);
                        var dTr = Vector2.Transform(tr, eyeTransform);
                        var dBl = Vector2.Transform(bl, eyeTransform);
                        var dBr = dBl + dTr - dTl;

                        // Get which neighbors are occluding.
                        var no = (occluder.Occluding & OccluderDir.North) != 0;
                        var so = (occluder.Occluding & OccluderDir.South) != 0;
                        var eo = (occluder.Occluding & OccluderDir.East) != 0;
                        var wo = (occluder.Occluding & OccluderDir.West) != 0;

                        // Do visibility tests for occluders (described above).
                        static bool CheckFaceEyeVis(Vector2 a, Vector2 b)
                        {
                            // determine which side of the plane the face is on
                            // the plane is at the origin of this coordinate system, which is also the eye
                            // the normal of the plane is that of the face
                            // therefore, if the dot <= 0, the face is facing the camera
                            // I don't like this, but rotated occluders started happening

                            // var normal =  (b - a).Rotated90DegreesAnticlockwiseWorld;
                            // Vector2.Dot(normal, a) <= 0;
                            // equivalent to:
                            return a.X * b.Y > a.Y * b.X;
                        }

                        var nV = ((!no) && CheckFaceEyeVis(dTl, dTr));
                        var sV = ((!so) && CheckFaceEyeVis(dBr, dBl));
                        var eV = ((!eo) && CheckFaceEyeVis(dTr, dBr));
                        var wV = ((!wo) && CheckFaceEyeVis(dBl, dTl));
                        var tlV = nV || wV;
                        var trV = nV || eV;
                        var blV = sV || wV;
                        var brV = sV || eV;

                        // Handle faces, rules described above.
                        // Note that "from above" it should be clockwise.
                        // Further handling is in the shadow depth vertex shader.
                        // (I have broken this so many times. - 20kdc)

                        void WriteFaceOfBuffer(Vector4 vec)
                        {
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
                        }

                        // North face (TL/TR)
                        if (!no || !tlV && !trV)
                        {
                            WriteFaceOfBuffer(faceN);
                        }

                        // East face (TR/BR)
                        if (!eo || !brV && !trV)
                        {
                            WriteFaceOfBuffer(faceE);
                        }

                        // South face (BR/BL)
                        if (!so || !brV && !blV)
                        {
                            WriteFaceOfBuffer(faceS);
                        }

                        // West face (BL/TL)
                        if (!wo || !blV && !tlV)
                        {
                            WriteFaceOfBuffer(faceW);
                        }

                        // Generate mask geometry.
                        arrayMaskBuffer[ami + 0] = new Vector2(tl.X, tl.Y);
                        arrayMaskBuffer[ami + 1] = new Vector2(tr.X, tr.Y);
                        arrayMaskBuffer[ami + 2] = new Vector2(br.X, br.Y);
                        arrayMaskBuffer[ami + 3] = new Vector2(bl.X, bl.Y);

                        // Generate mask indices.
                        QuadBatchIndexWrite(indexMaskBuffer, ref imi, (ushort)ami);

                        ami += 4;

                        return true;
                    }, treeBounds);
                }

                _occlusionDataLength = ii;
                _occlusionMaskDataLength = imi;

                // Upload geometry to OpenGL.
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
            finally
            {
                ArrayPool<Vector4>.Shared.Return(arrayBuffer);
                ArrayPool<byte>.Shared.Return(arrayVIBuffer);
                ArrayPool<ushort>.Shared.Return(indexBuffer);
                ArrayPool<Vector2>.Shared.Return(arrayMaskBuffer);
                ArrayPool<ushort>.Shared.Return(indexMaskBuffer);
            }

            _debugStats.Occluders += ami / 4;
        }

        private void RegenLightRts(Viewport viewport)
        {
            // All of these depend on screen size so they have to be re-created if it changes.

            var lightMapSize = GetLightMapSize(viewport.Size);
            var lightMapSizeQuart = GetLightMapSize(viewport.Size, true);
            var lightMapColorFormat = _hasGLFloatFramebuffers
                ? RenderTargetColorFormat.R11FG11FB10F
                : RenderTargetColorFormat.Rgba8;
            var lightMapSampleParameters = new TextureSampleParameters { Filter = true };

            viewport.LightRenderTarget?.Dispose();
            viewport.WallMaskRenderTarget?.Dispose();
            viewport.WallBleedIntermediateRenderTarget1?.Dispose();
            viewport.WallBleedIntermediateRenderTarget2?.Dispose();

            viewport.WallMaskRenderTarget = CreateRenderTarget(viewport.Size, RenderTargetColorFormat.R8,
                name: $"{viewport.Name}-{nameof(viewport.WallMaskRenderTarget)}");

            viewport.LightRenderTarget = CreateRenderTarget(lightMapSize,
                new RenderTargetFormatParameters(lightMapColorFormat, hasDepthStencil: true),
                lightMapSampleParameters,
                $"{viewport.Name}-{nameof(viewport.LightRenderTarget)}");

            viewport.LightBlurTarget = CreateRenderTarget(lightMapSize,
                new RenderTargetFormatParameters(lightMapColorFormat),
                lightMapSampleParameters,
                $"{viewport.Name}-{nameof(viewport.LightBlurTarget)}");

            viewport.WallBleedIntermediateRenderTarget1 = CreateRenderTarget(lightMapSizeQuart, lightMapColorFormat,
                lightMapSampleParameters,
                $"{viewport.Name}-{nameof(viewport.WallBleedIntermediateRenderTarget1)}");

            viewport.WallBleedIntermediateRenderTarget2 = CreateRenderTarget(lightMapSizeQuart, lightMapColorFormat,
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
