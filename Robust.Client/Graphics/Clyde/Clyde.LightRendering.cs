using System;
using System.Collections.Generic;
using System.Buffers;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using static Robust.Client.GameObjects.ClientOccluderComponent;
using OGLTextureWrapMode = OpenToolkit.Graphics.OpenGL.TextureWrapMode;
using TKStencilOp = OpenToolkit.Graphics.OpenGL4.StencilOp;

namespace Robust.Client.Graphics.Clyde
{
    // This file handles everything about light rendering.
    // That includes shadow casting and also FOV.
    // A detailed explanation of how all this works can be found on HackMD:
    // https://hackmd.io/@ss14/lighting-fov

    internal partial class Clyde
    {
        // Horizontal width, in pixels, of the shadow maps used to render regular lights.
        private const int ShadowMapSize = 512;

        // Horizontal width, in pixels, of the shadow maps used to render FOV.
        // I figured this was more accuracy sensitive than lights so resolution is significantly higher.
        private const int FovMapSize = 2048;

        // The maximum possible amount of lights in the light list.
        // In the average case, the only cost of increasing this value is memory.
        // If you are ever in a situation where this value needs to be increased, however, it will also implicitly cost some CPU time to sort the additional lights.
        private const int LightsToRenderListSize = 2048;

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

        private (PointLightComponent light, Vector2 pos, float distanceSquared)[] _lightsToRenderList = new (PointLightComponent light, Vector2 pos, float distanceSquared)[LightsToRenderListSize];

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
                new RenderTargetFormatParameters(_hasGLFloatFramebuffers ? RenderTargetColorFormat.RG32F : RenderTargetColorFormat.Rgba8, true),
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
            MaxLightsPerSceneChanged(_maxLightsPerScene);
        }

        private void LoadLightingShaders()
        {
            var depthVert = ReadEmbeddedShader("shadow-depth.vert");
            var depthFrag = ReadEmbeddedShader("shadow-depth.frag");

            (string, uint)[] attribLocations = {
                ("aPos", 0),
                ("subVertex", 1)
            };

            _fovCalculationProgram = _compileProgram(depthVert, depthFrag, attribLocations, "Shadow Depth Program");

            var debugShader = _resourceCache.GetResource<ShaderSourceResource>("/Shaders/Internal/depth-debug.swsl");
            _fovDebugShaderInstance = (ClydeShaderInstance)InstanceShader(debugShader.ClydeHandle);

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
            _lightBlurShaderHandle = LoadShaderHandle("/Shaders/Internal/light-blur.swsl");
            _mergeWallLayerShaderHandle = LoadShaderHandle("/Shaders/Internal/wall-merge.swsl");
        }

        private void DrawFov(Viewport viewport, IEye eye)
        {
            using var _ = DebugGroup(nameof(DrawFov));

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
            if (!_lightManager.Enabled)
            {
                return;
            }

            var mapId = eye.Position.MapId;

            // If this map has lighting disabled, return
            if (!_mapManager.GetMapEntity(mapId).GetComponent<IMapComponent>().LightingEnabled)
            {
                return;
            }

            var (lights, count, expandedBounds) = GetLightsToRender(mapId, worldBounds, worldAABB);
            eye.GetViewMatrix(out var eyeTransform, eye.Scale);

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
            {
                PrepareDepthDraw(RtToLoaded(_shadowRenderTarget));
                GL.CullFace(CullFaceMode.Back);
                CheckGlError();

                if (_lightManager.DrawShadows)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var (light, lightPos, _) = lights[i];

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
            GLClearColor(_lightManager.AmbientLightColor);
            GL.ClearStencil(0xFF);
            GL.StencilMask(0xFF);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit);
            CheckGlError();

            ApplyLightingFovToBuffer(viewport, eye);

            var lightShader = _loadedShaders[_enableSoftShadows ? _lightSoftShaderHandle : _lightHardShaderHandle].Program;
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

            for (var i = 0; i < count; i++)
            {
                var (component, lightPos, _) = lights[i];

                var transform = component.Owner.Transform;

                Texture? mask = null;
                var rotation = Angle.Zero;
                if (component.Mask != null)
                {
                    mask = component.Mask;
                    rotation = component.Rotation;

                    if (component.MaskAutoRotate)
                    {
                        rotation += transform.WorldRotation;
                    }
                }

                var maskTexture = mask ?? Texture.White;
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
                lightShader.SetUniformMaybe("lightIndex", component.CastShadows ? (i + 0.5f) / ShadowTexture.Height : -1);

                var offset = new Vector2(component.Radius, component.Radius);

                Matrix3 matrix;
                if (mask == null)
                {
                    matrix = Matrix3.Identity;
                }
                else
                {
                    // Only apply rotation if a mask is said, because else it doesn't matter.
                    matrix = Matrix3.CreateRotation(rotation);
                }

                (matrix.R0C2, matrix.R1C2) = lightPos;

                _drawQuad(-offset, offset, matrix, lightShader);

                _debugStats.TotalLights += 1;
            }

            ResetBlendFunc();
            GL.Disable(EnableCap.StencilTest);
            _isStencilling = false;

            CheckGlError();

            if (_cfg.GetCVar(CVars.DisplayBlurLight))
            {
                Blur(viewport, eye, viewport.LightRenderTarget, viewport.LightBlurTarget, viewport.LightRenderTarget,
                    _cfg.GetCVar(CVars.DisplayBlurLightFactor));
            }

            Blur(viewport, eye, viewport.LightRenderTarget,
                viewport.WallBleedIntermediateRenderTarget1, viewport.WallBleedIntermediateRenderTarget2,
                7e-3f, 1.1f);

            MergeWallLayer(viewport);

            BindRenderTargetFull(viewport.RenderTarget);
            GL.Viewport(0, 0, viewport.Size.X, viewport.Size.Y);
            CheckGlError();

            Array.Clear(lights, 0, count);

            _lightingReady = true;
        }

        private ((PointLightComponent light, Vector2 pos, float distanceSquared)[] lights, int count, Box2 expandedBounds)
            GetLightsToRender(MapId map, in Box2Rotated worldBounds, in Box2 worldAABB)
        {
            var renderingTreeSystem = _entitySystemManager.GetEntitySystem<RenderingTreeSystem>();
            var enlargedBounds = worldAABB.Enlarged(renderingTreeSystem.MaxLightRadius);

            // Use worldbounds for this one as we only care if the light intersects our actual bounds
            var state = (this, worldAABB, count: 0);

            foreach (var comp in renderingTreeSystem.GetRenderTrees(map, enlargedBounds))
            {
                var bounds = comp.Owner.Transform.InvWorldMatrix.TransformBox(worldBounds);

                comp.LightTree.QueryAabb(ref state, (ref (Clyde clyde, Box2 worldAABB, int count) state, in PointLightComponent light) =>
                {
                    if (state.count >= LightsToRenderListSize)
                    {
                        // There are too many lights to fit in the static memory.
                        return false;
                    }

                    var transform = light.Owner.Transform;

                    if (float.IsNaN(transform.LocalPosition.X) || float.IsNaN(transform.LocalPosition.Y)) return true;

                    var lightPos = transform.WorldMatrix.Transform(light.Offset);

                    var circle = new Circle(lightPos, light.Radius);

                    // If the light doesn't touch anywhere the camera can see, it doesn't matter.
                    if (!circle.Intersects(state.worldAABB))
                    {
                        return true;
                    }

                    float distanceSquared = (state.worldAABB.Center - lightPos).LengthSquared;
                    state.clyde._lightsToRenderList[state.count++] = (light, lightPos, distanceSquared);

                    return true;
                }, bounds);
            }

            if (state.count > _maxLightsPerScene)
            {
                // There are too many lights to fit in the scene.
                // This check must occur before occluder expansion, or else bad things happen.
                // Sort lights by distance.
                Array.Sort(_lightsToRenderList, 0, state.count, Comparer<(PointLightComponent light, Vector2 pos, float distanceSquared)>.Create((x, y) =>
                {
                    return x.distanceSquared.CompareTo(y.distanceSquared);
                }));
                // Then effectively delete the furthest lights.
                state.count = _maxLightsPerScene;
            }

            // When culling occluders later, we can't just remove any occluders outside the worldBounds.
            // As they could still affect the shadows of (large) light sources.
            // We expand the world bounds so that it encompasses the center of every light source.
            // This should make it so no culled occluder can make a difference.
            // (if the occluder is in the current lights at all, it's still not between the light and the world bounds).
            var expandedBounds = worldAABB;

            for (var i = 0; i < state.count; i++)
            {
                var (_, lightPos, _) = _lightsToRenderList[i];
                expandedBounds = expandedBounds.ExtendToContain(lightPos);
            }

            return (_lightsToRenderList, state.count, expandedBounds);
        }

        private void Blur(Viewport viewport, IEye eye,
            RenderTexture main, RenderTexture first, RenderTexture secondary,
            float blurFactor, float finalFactor=1.0f)
        {
            using var _ = DebugGroup(nameof(Blur));

            GL.Disable(EnableCap.Blend);
            CheckGlError();
            CalcScreenMatrices(viewport.Size, out var proj, out var view);
            SetProjViewBuffer(proj, view);

            var shader = _loadedShaders[_lightBlurShaderHandle].Program;
            shader.Use();

            SetupGlobalUniformsImmediate(shader, first.Texture);

            var size = first.Size;
            shader.SetUniformMaybe("size", (Vector2)size);
            shader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            GL.Viewport(0, 0, size.X, size.Y);
            CheckGlError();

            SetTexture(TextureUnit.Texture0, main.Texture);

            // Have to scale the blurring radius based on viewport size and camera zoom.
            const float refCameraHeight = 14;
            var cameraSize = eye.Zoom.Y * viewport.Size.Y * (1 / viewport.RenderScale.Y) / EyeManager.PixelsPerMeter;

            var factor = blurFactor * (refCameraHeight / cameraSize);

            // Multi-iteration gaussian blur.
            for (var i = 3; i > 0; i--)
            {
                var scale = (i + 1) * factor;
                // Set factor.
                shader.SetUniformMaybe("radius", scale);
                shader.SetUniformMaybe("final", finalFactor);

                BindRenderTargetFull(first);

                // Blur horizontally to _wallBleedIntermediateRenderTarget1.
                shader.SetUniformMaybe("direction", Vector2.UnitX);
                _drawQuad(Vector2.Zero, viewport.Size, Matrix3.Identity, shader);

                SetTexture(TextureUnit.Texture0, first.Texture);

                BindRenderTargetFull(secondary);

                // Blur vertically to _wallBleedIntermediateRenderTarget2.
                shader.SetUniformMaybe("direction", Vector2.UnitY);
                _drawQuad(Vector2.Zero, viewport.Size, Matrix3.Identity, shader);

                SetTexture(TextureUnit.Texture0, secondary.Texture);
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
            bool blur = _cfg.GetCVar(CVars.DisplayBlurFov);

            // GLES2 noobs don't have texture swizzling, sorry!
            if (_openGLVersion is RendererOpenGLVersion.GLES2) blur = false;

            // If we're blurring FOV, we need to render to a separate target first
            // and then draw that to the final framebuffer.
            if (blur)
            {
                BindRenderTargetFull(viewport.FovBlurTarget1);
                GLClearColor(default); // .Red
                GL.Clear(ClearBufferMask.ColorBufferBit);
                CheckGlError();
                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, (int) All.Red);
            }

            var fovShader = _loadedShaders[_fovShaderHandle].Program;
            fovShader.Use();

            SetupGlobalUniformsImmediate(fovShader, FovTexture);

            SetTexture(TextureUnit.Texture0, FovTexture);

            fovShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            GL.StencilMask(0xFF);
            CheckGlError();
            GL.StencilFunc(StencilFunction.Always, 0, 0);
            CheckGlError();
            GL.StencilOp(TKStencilOp.Keep, TKStencilOp.Keep, TKStencilOp.Replace);
            CheckGlError();

            FovSetTransformAndBlit(viewport, eye.Position.Position, fovShader);

            if (blur)
            {
                Blur(viewport, eye, viewport.FovBlurTarget1, viewport.FovBlurTarget2, viewport.FovBlurTarget1,
                    _cfg.GetCVar(CVars.DisplayBlurFovFactor));

                // GL.BlendFunc(BlendingFactor.Zero, BlendingFactor.OneMinusSrcAlpha);

                BindRenderTargetFull(viewport.RenderTarget);
                GL.Viewport(0, 0, viewport.RenderTarget.Size.X, viewport.RenderTarget.Size.Y);
                SetTexture(TextureUnit.Texture0, viewport.FovBlurTarget1.Texture);

                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

                // ResetBlendFunc();
                // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, (int) All.Alpha);
            }

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

            var clipToDiff = new Matrix3(ref uX, ref uY, ref uZero);

            fovShader.SetUniformMaybe("clipToDiff", clipToDiff);
            _drawQuad(Vector2.Zero, Vector2.One, Matrix3.Identity, fovShader);
        }

        private void UpdateOcclusionGeometry(MapId map, Box2 expandedBounds, Matrix3 eyeTransform)
        {
            // This method generates two sets of occlusion geometry:
            // 3D geometry used during depth projection.
            // 2D mask geometry used to apply wall bleed.

            // TODO: This code probably does not work correctly with rotated camera.
            // TODO: Yes this function throws and index exception if you reach maxOccluders.

            const int maxOccluders = 2048;

            using var _ = DebugGroup(nameof(UpdateOcclusionGeometry));

            // 16 = 4 vertices * 4 directions
            var arrayBuffer = ArrayPool<Vector4>.Shared.Rent(maxOccluders * 4 * 4);
            // multiplied by 2 (it's a vector2 of bytes)
            var arrayVIBuffer = ArrayPool<byte>.Shared.Rent(maxOccluders * 2 * 4 * 4);
            var indexBuffer = ArrayPool<ushort>.Shared.Rent(maxOccluders * GetQuadBatchIndexCount() * 4);

            var arrayMaskBuffer = ArrayPool<Vector2>.Shared.Rent(maxOccluders * 4);
            var indexMaskBuffer = ArrayPool<ushort>.Shared.Rent(maxOccluders * GetQuadBatchIndexCount());

            try
            {
                var occluderSystem = _entitySystemManager.GetEntitySystem<OccluderSystem>();

                var ai = 0;
                var avi = 0;
                var ami = 0;
                var ii = 0;
                var imi = 0;

                foreach (var comp in occluderSystem.GetOccluderTrees(map, expandedBounds))
                {
                    var treeBounds = comp.Owner.Transform.InvWorldMatrix.TransformBox(expandedBounds);

                    comp.Tree.QueryAabb((in OccluderComponent sOccluder) =>
                    {
                        var occluder = (ClientOccluderComponent)sOccluder;
                        var transform = occluder.Owner.Transform;
                        if (!occluder.Enabled)
                        {
                            return true;
                        }

                        var worldTransform = transform.WorldMatrix;
                        var box = occluder.BoundingBox;

                        var tl = worldTransform.Transform(box.TopLeft);
                        var tr = worldTransform.Transform(box.TopRight);
                        var br = worldTransform.Transform(box.BottomRight);
                        var bl = worldTransform.Transform(box.BottomLeft);

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
                        var (dTlX, dTlY) = eyeTransform.Transform(tl);
                        var (dTrX, dTrY) = eyeTransform.Transform(tr);
                        var (dBlX, dBlY) = eyeTransform.Transform(bl);
                        var (dBrX, dBrY) = eyeTransform.Transform(br);

                        // Get which neighbors are occluding.
                        var no = (occluder.Occluding & OccluderDir.North) != 0;
                        var so = (occluder.Occluding & OccluderDir.South) != 0;
                        var eo = (occluder.Occluding & OccluderDir.East) != 0;
                        var wo = (occluder.Occluding & OccluderDir.West) != 0;

                        // Do visibility tests for occluders (described above).
                        var tlV = dTlX > 0 && !wo || dTlY < 0 && !no;
                        var trV = dTrX < 0 && !eo || dTrY < 0 && !no;
                        var blV = dBlX > 0 && !wo || dBlY > 0 && !so;
                        var brV = dBrX < 0 && !eo || dBrY > 0 && !so;

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
        }

        private void RegenLightRts(Viewport viewport)
        {
            // All of these depend on screen size so they have to be re-created if it changes.

            var lightMapSize = GetLightMapSize(viewport.Size);
            var lightMapSizeQuart = GetLightMapSize(viewport.Size, true);
            var lightMapColorFormat = _hasGLFloatFramebuffers ? RenderTargetColorFormat.R11FG11FB10F : RenderTargetColorFormat.Rgba8;
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

            viewport.FovBlurTarget1 = CreateRenderTarget(lightMapSize,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true), // R8
                lightMapSampleParameters,
                $"{viewport.Name}-{nameof(viewport.FovBlurTarget1)}");

            viewport.FovBlurTarget2 = CreateRenderTarget(lightMapSize,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true), // R8
                lightMapSampleParameters,
                $"{viewport.Name}-{nameof(viewport.FovBlurTarget2)}");

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
            var divider = (float)_lightmapDivider;
            if (furtherDivide)
            {
                divider *= 2;
            }

            var w = (int)Math.Ceiling(screenSize.X / divider);
            var h = (int)Math.Ceiling(screenSize.Y / divider);

            return (w, h);
        }

        private void LightmapDividerChanged(int newValue)
        {
            _lightmapDivider = newValue;
            RegenAllLightRts();
        }

        private void MaxLightsPerSceneChanged(int newValue)
        {
            _maxLightsPerScene = newValue;

            // This guard is in place because otherwise the shadow FBO is initialized before GL is initialized.
            if (!_shadowRenderTargetCanInitializeSafely)
                return;

            if (_shadowRenderTarget != null)
            {
                DeleteRenderTexture(_shadowRenderTarget.Handle);
            }
            // Shadow FBO.
            _shadowRenderTarget = CreateRenderTarget((ShadowMapSize, _maxLightsPerScene),
                new RenderTargetFormatParameters(_hasGLFloatFramebuffers ? RenderTargetColorFormat.RG32F : RenderTargetColorFormat.Rgba8, true),
                new TextureSampleParameters { WrapMode = TextureWrapMode.Repeat, Filter = true },
                nameof(_shadowRenderTarget));
        }

        private void SoftShadowsChanged(bool newValue)
        {
            _enableSoftShadows = newValue;
        }
    }
}
