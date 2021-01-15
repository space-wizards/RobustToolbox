using System;
using System.Buffers;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using static Robust.Client.GameObjects.ClientOccluderComponent;
using OGLTextureWrapMode = OpenToolkit.Graphics.OpenGL.TextureWrapMode;

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
        private const int MaxLightsPerScene = 128;

        private ClydeShaderInstance _fovDebugShaderInstance = default!;

        // Various shaders used in the light rendering process.
        // We keep ClydeHandles into the _loadedShaders dict so they can be reloaded.
        // They're all .swsl now.
        private ClydeHandle _lightSoftShaderHandle;
        private ClydeHandle _lightHardShaderHandle;
        private ClydeHandle _fovShaderHandle;
        private ClydeHandle _fovLightShaderHandle;
        private ClydeHandle _wallBleedBlurShaderHandle;
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

        // Proxies to textures of the above render targets.
        private ClydeTexture FovTexture => _fovRenderTarget.Texture;
        private ClydeTexture ShadowTexture => _shadowRenderTarget.Texture;

        private readonly (PointLightComponent light, Vector2 pos)[] _lightsToRenderList
            = new (PointLightComponent light, Vector2 pos)[MaxLightsPerScene];

        private unsafe void InitLighting()
        {
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
                new TextureSampleParameters {WrapMode = TextureWrapMode.Repeat},
                nameof(_fovRenderTarget));

            if (_hasGLSamplerObjects)
            {
                _fovFilterSampler = new GLHandle(GL.GenSampler());
                GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureMagFilter, (int) All.Linear);
                GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureMinFilter, (int) All.Linear);
                GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureWrapS, (int) All.Repeat);
                GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureWrapT, (int) All.Repeat);
                CheckGlError();
            }

            // Shadow FBO.
            _shadowRenderTarget = CreateRenderTarget((ShadowMapSize, MaxLightsPerScene),
                new RenderTargetFormatParameters(_hasGLFloatFramebuffers ? RenderTargetColorFormat.RG32F : RenderTargetColorFormat.Rgba8, true),
                new TextureSampleParameters {WrapMode = TextureWrapMode.Repeat, Filter = true},
                nameof(_shadowRenderTarget));
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
            _fovDebugShaderInstance = (ClydeShaderInstance) InstanceShader(debugShader.ClydeHandle);

            ClydeHandle LoadShaderHandle(string path)
            {
                try
                {
                    var shaderSource = _resourceCache.GetResource<ShaderSourceResource>(path);
                    return shaderSource.ClydeHandle;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Can't load shader {path}\n{ex.GetType().Name}: {ex.Message}");
                    return default;
                }
            }


            _lightSoftShaderHandle = LoadShaderHandle("/Shaders/Internal/light-soft.swsl");
            _lightHardShaderHandle = LoadShaderHandle("/Shaders/Internal/light-hard.swsl");
            _fovShaderHandle = LoadShaderHandle("/Shaders/Internal/fov.swsl");
            _fovLightShaderHandle = LoadShaderHandle("/Shaders/Internal/fov-lighting.swsl");
            _wallBleedBlurShaderHandle = LoadShaderHandle("/Shaders/Internal/wall-bleed-blur.swsl");
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
                var maxDist = (float) Math.Max(screenSizeCut.X, screenSizeCut.Y);

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

        private void DrawLightsAndFov(Viewport viewport, Box2 worldBounds, IEye eye)
        {
            if (!_lightManager.Enabled)
            {
                return;
            }

            var map = eye.Position.MapId;

            var (lights, count, expandedBounds) = GetLightsToRender(map, worldBounds);

            UpdateOcclusionGeometry(map, expandedBounds, eye.Position.Position);

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
                        var (light, lightPos) = lights[i];

                        DrawOcclusionDepth(lightPos, ShadowMapSize, light.Radius, i);
                    }
                }

                FinalizeDepthDraw();
            }

            BindRenderTargetImmediate(RtToLoaded(viewport.LightRenderTarget));
            CheckGlError();
            GLClearColor(_lightManager.AmbientLightColor);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            CheckGlError();

            var (lightW, lightH) = GetLightMapSize(viewport.Size);
            GL.Viewport(0, 0, lightW, lightH);
            CheckGlError();

            var lightShader = _loadedShaders[_enableSoftShadows ? _lightSoftShaderHandle : _lightHardShaderHandle].Program;
            lightShader.Use();

            SetupGlobalUniformsImmediate(lightShader, ShadowTexture);

            SetTexture(TextureUnit.Texture1, ShadowTexture);
            lightShader.SetUniformTextureMaybe("shadowMap", TextureUnit.Texture1);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            CheckGlError();

            var lastRange = float.NaN;
            var lastPower = float.NaN;
            var lastColor = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
            var lastSoftness = float.NaN;
            Texture? lastMask = null;

            for (var i = 0; i < count; i++)
            {
                var (component, lightPos) = lights[i];

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

                if (!MathHelper.CloseTo(lastRange, component.Radius))
                {
                    lastRange = component.Radius;
                    lightShader.SetUniformMaybe("lightRange", lastRange);
                }

                if (!MathHelper.CloseTo(lastPower, component.Energy))
                {
                    lastPower = component.Energy;
                    lightShader.SetUniformMaybe("lightPower", lastPower);
                }

                if (lastColor != component.Color)
                {
                    lastColor = component.Color;
                    lightShader.SetUniformMaybe("lightColor", lastColor);
                }

                if (_enableSoftShadows && !MathHelper.CloseTo(lastSoftness, component.Softness))
                {
                    lastSoftness = component.Softness;
                    lightShader.SetUniformMaybe("lightSoftness", lastSoftness);
                }

                lightShader.SetUniformMaybe("lightCenter", lightPos);
                lightShader.SetUniformMaybe("lightIndex", (i + 0.5f) / ShadowTexture.Height);

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

            CheckGlError();

            ApplyLightingFovToBuffer(viewport, eye);

            BlurOntoWalls(viewport, eye);

            MergeWallLayer(viewport);

            BindRenderTargetFull(viewport.RenderTarget);
            GL.Viewport(0, 0, viewport.Size.X, viewport.Size.Y);
            CheckGlError();

            Array.Clear(lights, 0, count);

            _lightingReady = true;
        }

        private ((PointLightComponent light, Vector2 pos)[] lights, int count, Box2 expandedBounds)
            GetLightsToRender(MapId map, in Box2 worldBounds)
        {
            // When culling occluders later, we can't just remove any occluders outside the worldBounds.
            // As they could still affect the shadows of (large) light sources.
            // We expand the world bounds so that it encompasses the center of every light source.
            // This should make it so no culled occluder can make a difference.
            // (if the occluder is in the current lights at all, it's still not between the light and the world bounds).
            var expandedBounds = worldBounds;

            var renderingTreeSystem = _entitySystemManager.GetEntitySystem<RenderingTreeSystem>();
            var lightTree = renderingTreeSystem.GetLightTreeForMap(map);

            var state = (this, expandedBounds, count: 0);

            lightTree.QueryAabb(ref state, (ref (Clyde clyde, Box2 expandedBounds, int count) state, in PointLightComponent light) =>
            {
                var transform = light.Owner.Transform;

                if (!light.Enabled || light.ContainerOccluded)
                {
                    return true;
                }

                var lightPos = transform.WorldMatrix.Transform(light.Offset);

                var circle = new Circle(lightPos, light.Radius);

                if (!circle.Intersects(state.expandedBounds))
                {
                    return true;
                }

                state.clyde._lightsToRenderList[state.count] = (light, lightPos);
                state.count += 1;

                state.expandedBounds = state.expandedBounds.ExtendToContain(lightPos);

                if (state.count == MaxLightsPerScene)
                {
                    // TODO: Allow more than MaxLightsPerScene lights.
                    return false;
                }

                return true;
            }, expandedBounds);

            return (_lightsToRenderList, state.count, state.expandedBounds);
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

            shader.SetUniformMaybe("size", (Vector2) viewport.WallBleedIntermediateRenderTarget1.Size);
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
            var cameraSize = eye.Zoom.Y * viewport.Size.Y / EyeManager.PixelsPerMeter;
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
                _drawQuad(Vector2.Zero, viewport.Size, Matrix3.Identity, shader);

                SetTexture(TextureUnit.Texture0, viewport.WallBleedIntermediateRenderTarget1.Texture);
                BindRenderTargetFull(viewport.WallBleedIntermediateRenderTarget2);

                // Blur vertically to _wallBleedIntermediateRenderTarget2.
                shader.SetUniformMaybe("direction", Vector2.UnitY);
                _drawQuad(Vector2.Zero, viewport.Size, Matrix3.Identity, shader);

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
            // Applies FOV to the final framebuffer.

            var fovShader = _loadedShaders[_fovShaderHandle].Program;
            fovShader.Use();

            SetupGlobalUniformsImmediate(fovShader, FovTexture);

            SetTexture(TextureUnit.Texture0, FovTexture);

            fovShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
            fovShader.SetUniformMaybe("center", eye.Position.Position);

            DrawBlit(viewport, fovShader);
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
            fovShader.SetUniformMaybe("center", eye.Position.Position);

            DrawBlit(viewport, fovShader);

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

        private void DrawBlit(Viewport vp, GLShaderProgram shader)
        {
            var a = ScreenToMap((-1, -1), vp);
            var b = ScreenToMap(vp.Size + Vector2i.One, vp);

            _drawQuad(a, b, Matrix3.Identity, shader);
        }

        private void UpdateOcclusionGeometry(MapId map, Box2 expandedBounds, Vector2 eyePosition)
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

                foreach (var gridId in _mapManager.FindGridIdsIntersecting(map, expandedBounds, true))
                {
                    if (!occluderSystem.TryGetOccluderTreeForGrid(map, gridId, out var occluderTree)) continue;

                    Box2 gridBounds;

                    if (gridId == GridId.Invalid)
                    {
                        gridBounds = expandedBounds;
                    }
                    else
                    {
                        // TODO: Ideally this would clamp to the outer border of what we can see
                        var grid = _mapManager.GetGrid(gridId);
                        gridBounds = expandedBounds.Translated(-grid.WorldPosition);
                    }

                    occluderTree.QueryAabb((in OccluderComponent sOccluder) =>
                    {
                        var occluder = (ClientOccluderComponent) sOccluder;
                        var transform = occluder.Owner.Transform;
                        if (!occluder.Enabled)
                        {
                            return true;
                        }

                        var worldTransform = transform.WorldMatrix;
                        var box = occluder.BoundingBox;

                        // So uh, angle 0 = east... Apparently...
                        // We account for that here so I don't go insane.
                        var (tlX, tlY) = worldTransform.Transform(box.BottomLeft);
                        var (trX, trY) = worldTransform.Transform(box.TopLeft);
                        var (brX, brY) = worldTransform.Transform(box.TopRight);
                        var (blX, blY) = worldTransform.Transform(box.BottomRight);

                        // Faces.
                        var faceN = new Vector4(tlX, tlY, trX, trY);
                        var faceE = new Vector4(trX, trY, brX, brY);
                        var faceS = new Vector4(brX, brY, blX, blY);
                        var faceW = new Vector4(blX, blY, tlX, tlY);

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
                        var (dTlX, dTlY) = (tlX, tlY) - eyePosition;
                        var (dTrX, dTrY) = (trX, trY) - eyePosition;
                        var (dBlX, dBlY) = (blX, blY) - eyePosition;
                        var (dBrX, dBrY) = (brX, brY) - eyePosition;

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
                                arrayVIBuffer[avi++] = (byte) ((((vi + 1) & 2) != 0) ? 0 : 255);
                                // height
                                arrayVIBuffer[avi++] = (byte) (((vi & 2) != 0) ? 0 : 255);
                            }
                            QuadBatchIndexWrite(indexBuffer, ref ii, (ushort) aiBase);
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
                        arrayMaskBuffer[ami + 0] = new Vector2(tlX, tlY);
                        arrayMaskBuffer[ami + 1] = new Vector2(trX, trY);
                        arrayMaskBuffer[ami + 2] = new Vector2(brX, brY);
                        arrayMaskBuffer[ami + 3] = new Vector2(blX, blY);

                        // Generate mask indices.
                        QuadBatchIndexWrite(indexMaskBuffer, ref imi, (ushort) ami);

                        ami += 4;

                        return true;
                    }, gridBounds);
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
            var lightMapSampleParameters = new TextureSampleParameters {Filter = true};

            viewport.LightRenderTarget?.Dispose();
            viewport.WallMaskRenderTarget?.Dispose();
            viewport.WallBleedIntermediateRenderTarget1?.Dispose();
            viewport.WallBleedIntermediateRenderTarget2?.Dispose();

            viewport.WallMaskRenderTarget = CreateRenderTarget(viewport.Size, RenderTargetColorFormat.R8,
                name: $"{viewport.Name}-{nameof(viewport.WallMaskRenderTarget)}");

            viewport.LightRenderTarget = CreateRenderTarget(lightMapSize, lightMapColorFormat,
                lightMapSampleParameters,
                $"{viewport.Name}-{nameof(viewport.LightRenderTarget)}");

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
            var divider = (float) _lightmapDivider;
            if (furtherDivide)
            {
                divider *= 2;
            }

            var w = (int) Math.Ceiling(screenSize.X / divider);
            var h = (int) Math.Ceiling(screenSize.Y / divider);

            return (w, h);
        }

        protected override void LightmapDividerChanged(int newValue)
        {
            _lightmapDivider = newValue;
            RegenAllLightRts();
        }

        protected override void SoftShadowsChanged(bool newValue)
        {
            _enableSoftShadows = newValue;
        }
    }
}
