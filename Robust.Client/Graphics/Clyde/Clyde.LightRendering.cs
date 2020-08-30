using System;
using System.Buffers;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.ResourceManagement.ResourceTypes;
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
        private ClydeHandle _lightShaderHandle;
        private ClydeHandle _fovShaderHandle;
        private ClydeHandle _fovLightShaderHandle;
        private ClydeHandle _wallBleedBlurShaderHandle;
        private ClydeHandle _mergeWallLayerShaderHandle;

        // Projection matrix used while rendering FOV.
        // We keep this around so we can reverse the effects while overlaying actual FOV.
        private Matrix4 _fovProjection;

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

        private readonly Matrix4[] _shadowMatrices = new Matrix4[MaxLightsPerScene];

        private unsafe void InitLighting()
        {
            LoadLightingShaders();

            {
                // Occlusion VAO.
                // Only handles positions, no other vertex data necessary.
                _occlusionVao = new GLHandle(GL.GenVertexArray());
                GL.BindVertexArray(_occlusionVao.Handle);

                ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, _occlusionVao, nameof(_occlusionVao));

                _occlusionVbo = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                    nameof(_occlusionVbo));

                _occlusionEbo = new GLBuffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                    nameof(_occlusionEbo));

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(Vector3), IntPtr.Zero);
                GL.EnableVertexAttribArray(0);
            }

            {
                // Occlusion mask VAO.
                // Only handles positions, no other vertex data necessary.

                _occlusionMaskVao = new GLHandle(GL.GenVertexArray());
                GL.BindVertexArray(_occlusionMaskVao.Handle);

                ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, _occlusionMaskVao, nameof(_occlusionMaskVao));

                _occlusionMaskVbo = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                    nameof(_occlusionMaskVbo));

                _occlusionMaskEbo = new GLBuffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                    nameof(_occlusionMaskEbo));

                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(Vector2), IntPtr.Zero);
                GL.EnableVertexAttribArray(0);
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
                ("aPos", 0)
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


            _lightShaderHandle = LoadShaderHandle("/Shaders/Internal/light.swsl");
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

                DrawOcclusionDepth(eye.Position.Position, _fovRenderTarget.Size.X, maxDist, 0, out _fovProjection);

                GL.CullFace(CullFaceMode.Front);

                DrawOcclusionDepth(eye.Position.Position, _fovRenderTarget.Size.X, maxDist, 1, out _fovProjection);
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
        /// <param name="projMatrix">
        ///     Projection matrix necessary to later un-project the depth map when applying it.
        /// </param>
        private void DrawOcclusionDepth(Vector2 lightPos, int width, float maxDist, int viewportY,
            out Matrix4 projMatrix)
        {
            projMatrix = default; // Gets overriden below.

            var (posX, posY) = lightPos;
            var lightMatrix = Matrix4.CreateTranslation(-posX, -posY, 0);

            // The light is now the center of the universe.
            _fovCalculationProgram.SetUniform("shadowLightMatrix", lightMatrix, false);

            var baseProj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(90),
                1,
                maxDist / 1000,
                maxDist * 1.1f);

            var step = width / 4;

            GL.Disable(EnableCap.Blend);

            for (var i = 0; i < 4; i++)
            {
                // The occlusion geometry has to be rotated for every orientation and also corrected
                // so that the 2D coordinates make more sense in 3D.
                // These quaternions do that.
                // They're just two 90 degree rotations around (at most) 2 of the axes of rotation but uhhh.
                // I couldn't get LookRotation to work, ok?
                var orientation = i switch
                {
                    0 => new Quaternion(-0.707f, 0, 0, 0.707f),
                    1 => new Quaternion(0.5f, -0.5f, -0.5f, -0.5f),
                    2 => new Quaternion(0, 0.707f, 0.707f, 0),
                    3 => new Quaternion(-0.5f, -0.5f, -0.5f, 0.5f),
                    _ => default
                };

                var rotMatrix = Matrix4.Rotate(orientation);
                var proj = rotMatrix * baseProj;

                if (i == 0)
                {
                    // First projection matrix is necessary to undo the projection inside the application shader.
                    // So store it.
                    projMatrix = proj;
                }

                _fovCalculationProgram.SetUniform("shadowProjectionMatrix", proj, false);
                // Shift viewport around so we write to the correct quadrant of the depth map.
                GL.Viewport(step * i, viewportY, step, 1);

                GL.DrawElements(GetQuadGLPrimitiveType(), _occlusionDataLength, DrawElementsType.UnsignedShort, 0);
                _debugStats.LastGLDrawCalls += 1;
            }

            GL.Enable(EnableCap.Blend);
        }

        private void PrepareDepthDraw(LoadedRenderTarget target)
        {
            const float arbitraryDistanceMax = 1234;

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);

            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Cw);

            BindRenderTargetImmediate(target);
            GL.ClearDepth(1);
            GL.ClearColor(arbitraryDistanceMax, arbitraryDistanceMax * arbitraryDistanceMax, 0, 1);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            GL.BindVertexArray(_occlusionVao.Handle);

            _fovCalculationProgram.Use();

            SetupGlobalUniformsImmediate(_fovCalculationProgram, null);
        }

        private static void FinalizeDepthDraw()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
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

            using (DebugGroup("Draw shadow depth"))
            {
                PrepareDepthDraw(RtToLoaded(_shadowRenderTarget));
                GL.CullFace(CullFaceMode.Back);

                if (_lightManager.DrawShadows)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var (light, lightPos) = lights[i];

                        DrawOcclusionDepth(lightPos, ShadowMapSize, light.Radius, i, out _shadowMatrices[i]);
                    }
                }

                FinalizeDepthDraw();
            }

            BindRenderTargetImmediate(RtToLoaded(viewport.LightRenderTarget));
            GLClearColor(Color.FromSrgb(AmbientLightColor));
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var (lightW, lightH) = GetLightMapSize(viewport.Size);
            GL.Viewport(0, 0, lightW, lightH);

            var lightShader = _loadedShaders[_lightShaderHandle].Program;
            lightShader.Use();

            SetupGlobalUniformsImmediate(lightShader, ShadowTexture);

            SetTexture(TextureUnit.Texture1, ShadowTexture);
            lightShader.SetUniformTextureMaybe("shadowMap", TextureUnit.Texture1);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

            var lastRange = float.NaN;
            var lastPower = float.NaN;
            var lastColor = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
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

                lightShader.SetUniformMaybe("lightCenter", lightPos);
                lightShader.SetUniformMaybe("lightIndex", (i + 0.5f) / ShadowTexture.Height);
                lightShader.SetUniformMaybe("shadowMatrix", _shadowMatrices[i], false);

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

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            ApplyLightingFovToBuffer(viewport, eye);

            BlurOntoWalls(viewport, eye);

            MergeWallLayer(viewport);

            BindRenderTargetFull(viewport.RenderTarget);
            GL.Viewport(0, 0, viewport.Size.X, viewport.Size.Y);

            Array.Clear(lights, 0, count);

            _lightingReady = true;
        }

        private ((PointLightComponent light, Vector2 pos)[] lights, int count, Box2 expandedBounds)
            GetLightsToRender(MapId map, in Box2 worldBounds)
        {
            var count = 0;

            // When culling occluders later, we can't just remove any occluders outside the worldBounds.
            // As they could still affect the shadows of (large) light sources.
            // We expand the world bounds so that it encompasses the center of every light source.
            // This should make it so no culled occluder can make a difference.
            // (if the occluder is in the current lights at all, it's still not between the light and the world bounds).
            var expandedBounds = worldBounds;

            var renderingTreeSystem = _entitySystemManager.GetEntitySystem<RenderingTreeSystem>();
            var lightTree = renderingTreeSystem.GetLightTreeForMap(map);

            foreach (var component in lightTree.Query(worldBounds))
            {
                var transform = component.Owner.Transform;

                if (!component.Enabled || component.ContainerOccluded)
                {
                    continue;
                }

                var lightPos = transform.WorldMatrix.Transform(component.Offset);

                var circle = new Circle(lightPos, component.Radius);

                if (!circle.Intersects(worldBounds))
                {
                    continue;
                }

                _lightsToRenderList[count] = (component, lightPos);
                count += 1;

                expandedBounds = expandedBounds.ExtendToContain(lightPos);

                if (count == MaxLightsPerScene)
                {
                    // TODO: Allow more than MaxLightsPerScene lights.
                    break;
                }
            }

            return (_lightsToRenderList, count, expandedBounds);
        }

        private void BlurOntoWalls(Viewport viewport, IEye eye)
        {
            using var _ = DebugGroup(nameof(BlurOntoWalls));

            GL.Disable(EnableCap.Blend);
            CalcScreenMatrices(viewport.Size, out var proj, out var view);
            SetProjViewBuffer(proj, view);

            var shader = _loadedShaders[_wallBleedBlurShaderHandle].Program;
            shader.Use();

            SetupGlobalUniformsImmediate(shader, viewport.LightRenderTarget.Texture);

            shader.SetUniformMaybe("size", (Vector2) viewport.WallBleedIntermediateRenderTarget1.Size);
            shader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            var size = viewport.WallBleedIntermediateRenderTarget1.Size;
            GL.Viewport(0, 0, size.X, size.Y);

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
            // We didn't trample over the old _currentMatrices so just roll it back.
            SetProjViewBuffer(_currentMatrixProj, _currentMatrixView);
        }

        private void MergeWallLayer(Viewport viewport)
        {
            using var _ = DebugGroup(nameof(MergeWallLayer));

            BindRenderTargetFull(viewport.LightRenderTarget);

            GL.Viewport(0, 0, viewport.LightRenderTarget.Size.X, viewport.LightRenderTarget.Size.Y);
            GL.Disable(EnableCap.Blend);

            var shader = _loadedShaders[_mergeWallLayerShaderHandle].Program;
            shader.Use();

            var tex = viewport.WallBleedIntermediateRenderTarget2.Texture;

            SetupGlobalUniformsImmediate(shader, tex);

            SetTexture(TextureUnit.Texture0, tex);

            shader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            GL.BindVertexArray(_occlusionMaskVao.Handle);

            GL.DrawElements(GetQuadGLPrimitiveType(), _occlusionMaskDataLength, DrawElementsType.UnsignedShort,
                IntPtr.Zero);

            GL.Enable(EnableCap.Blend);
        }

        private void ApplyFovToBuffer(Viewport viewport, IEye eye)
        {
            // Applies FOV to the final framebuffer.

            var fovShader = _loadedShaders[_fovShaderHandle].Program;
            fovShader.Use();

            SetupGlobalUniformsImmediate(fovShader, FovTexture);

            SetTexture(TextureUnit.Texture0, FovTexture);

            fovShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
            fovShader.SetUniformMaybe("shadowMatrix", _fovProjection, false);
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
            }
            else
            {
                // OpenGL why do you torture me so.
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            }

            fovShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
            fovShader.SetUniformMaybe("shadowMatrix", _fovProjection, false);
            fovShader.SetUniformMaybe("center", eye.Position.Position);

            DrawBlit(viewport, fovShader);

            if (_hasGLSamplerObjects)
            {
                GL.BindSampler(0, 0);
            }
            else
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
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
            const float polygonHeight = 500;

            using var _ = DebugGroup(nameof(UpdateOcclusionGeometry));

            var arrayBuffer = ArrayPool<Vector3>.Shared.Rent(maxOccluders * 8);
            var indexBuffer = ArrayPool<ushort>.Shared.Rent(maxOccluders * GetQuadBatchIndexCount() * 4);

            var arrayMaskBuffer = ArrayPool<Vector2>.Shared.Rent(maxOccluders * 4);
            var indexMaskBuffer = ArrayPool<ushort>.Shared.Rent(maxOccluders * GetQuadBatchIndexCount());

            try
            {
                var renderingTreeSystem = _entitySystemManager.GetEntitySystem<RenderingTreeSystem>();
                var occluderTree = renderingTreeSystem.GetOccluderTreeForMap(map);

                var ai = 0;
                var ami = 0;
                var ii = 0;
                var imi = 0;

                foreach (var occluder in occluderTree.Query(expandedBounds))
                {
                    var transform = occluder.Owner.Transform;
                    if (!occluder.Enabled)
                    {
                        continue;
                    }

                    var worldTransform = transform.WorldMatrix;
                    var box = occluder.BoundingBox;

                    // So uh, angle 0 = east... Apparently...
                    // We account for that here so I don't go insane.
                    var (tlX, tlY) = worldTransform.Transform(box.BottomLeft);
                    var (trX, trY) = worldTransform.Transform(box.TopLeft);
                    var (brX, brY) = worldTransform.Transform(box.TopRight);
                    var (blX, blY) = worldTransform.Transform(box.BottomRight);

                    // Vertices used as main occlusion geometry.
                    // We always send all of these (see below) to keep code complexity down.
                    ushort vTLH = (ushort) (ai + 0);
                    arrayBuffer[ai + 0] = new Vector3(tlX, tlY, polygonHeight);
                    ushort vTLL = (ushort) (ai + 1);
                    arrayBuffer[ai + 1] = new Vector3(tlX, tlY, -polygonHeight);
                    ushort vTRH = (ushort) (ai + 2);
                    arrayBuffer[ai + 2] = new Vector3(trX, trY, polygonHeight);
                    ushort vTRL = (ushort) (ai + 3);
                    arrayBuffer[ai + 3] = new Vector3(trX, trY, -polygonHeight);
                    ushort vBRH = (ushort) (ai + 4);
                    arrayBuffer[ai + 4] = new Vector3(brX, brY, polygonHeight);
                    ushort vBRL = (ushort) (ai + 5);
                    arrayBuffer[ai + 5] = new Vector3(brX, brY, -polygonHeight);
                    ushort vBLH = (ushort) (ai + 6);
                    arrayBuffer[ai + 6] = new Vector3(blX, blY, polygonHeight);
                    ushort vBLL = (ushort) (ai + 7);
                    arrayBuffer[ai + 7] = new Vector3(blX, blY, -polygonHeight);

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
                    // Note that faces are drawn with their 'normals' facing in the described direction in 3D space.
                    // That is, they're clockwise "as viewed from the outside".
                    // (When changing things to QuadBatchIndexWrite,
                    // I described the behaviour correctly in this comment and then failed to implement it - 20kdc)

                    // North face (TL/TR)
                    if (!no || !tlV && !trV)
                    {
                        QuadBatchIndexWrite(indexBuffer, ref ii, vTRH, vTLH, vTLL, vTRL);
                    }

                    // East face (TR/BR)
                    if (!eo || !brV && !trV)
                    {
                        QuadBatchIndexWrite(indexBuffer, ref ii, vBRH, vTRH, vTRL, vBRL);
                    }

                    // South face (BR/BL)
                    if (!so || !brV && !blV)
                    {
                        QuadBatchIndexWrite(indexBuffer, ref ii, vBLH, vBRH, vBRL, vBLL);
                    }

                    // West face (BL/TL)
                    if (!wo || !blV && !tlV)
                    {
                        QuadBatchIndexWrite(indexBuffer, ref ii, vTLH, vBLH, vBLL, vTLL);
                    }

                    // Generate mask geometry.
                    arrayMaskBuffer[ami + 0] = new Vector2(tlX, tlY);
                    arrayMaskBuffer[ami + 1] = new Vector2(trX, trY);
                    arrayMaskBuffer[ami + 2] = new Vector2(brX, brY);
                    arrayMaskBuffer[ami + 3] = new Vector2(blX, blY);

                    // Generate mask indices.
                    QuadBatchIndexWrite(indexMaskBuffer, ref imi, (ushort) ami);

                    ai += 8;
                    ami += 4;
                }

                _occlusionDataLength = ii;
                _occlusionMaskDataLength = imi;

                // Upload geometry to OpenGL.
                GL.BindVertexArray(_occlusionVao.Handle);

                _occlusionVbo.Reallocate(arrayBuffer.AsSpan(..ai));
                _occlusionEbo.Reallocate(indexBuffer.AsSpan(..ii));

                GL.BindVertexArray(_occlusionMaskVao.Handle);

                _occlusionMaskVbo.Reallocate(arrayMaskBuffer.AsSpan(..ami));
                _occlusionMaskEbo.Reallocate(indexMaskBuffer.AsSpan(..imi));
            }
            finally
            {
                ArrayPool<Vector3>.Shared.Return(arrayBuffer);
                ArrayPool<Vector2>.Shared.Return(arrayMaskBuffer);
                ArrayPool<ushort>.Shared.Return(indexBuffer);
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

        private Vector2i GetLightMapSize(Vector2i screenSize, bool? overrideSetting = null)
        {
            var setting = overrideSetting ?? _quartResLights;
            if (!setting)
            {
                return screenSize;
            }

            var w = (int) Math.Ceiling(screenSize.X / 2f);
            var h = (int) Math.Ceiling(screenSize.Y / 2f);

            return (w, h);
        }

        protected override void HighResLightsChanged(bool newValue)
        {
            _quartResLights = !newValue;
            RegenAllLightRts();
        }
    }
}
