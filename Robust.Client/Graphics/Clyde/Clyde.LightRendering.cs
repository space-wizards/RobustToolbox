using System;
using System.Buffers;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using static Robust.Client.GameObjects.ClientOccluderComponent;
using OGLTextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;

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
        private const int MaxLightsPerScene = 64;

        private ClydeShaderInstance _fovDebugShaderInstance;

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

        // Various render targets used in the light rendering process.

        // Lighting is drawn into this. This then gets sampled later while rendering world-space stuff.
        private RenderTarget _lightRenderTarget;

        // For depth calculation for FOV.
        private RenderTarget _fovRenderTarget;

        // For depth calculation of lighting shadows.
        private RenderTarget _shadowRenderTarget;

        // Unused, to be removed.
        private RenderTarget _wallMaskRenderTarget;

        // Two render targets used to apply gaussian blur to the _lightRenderTarget so it bleeds "into" walls.
        // We need two of them because efficient blur works in two stages and also we're doing multiple iterations.
        private RenderTarget _wallBleedIntermediateRenderTarget1;
        private RenderTarget _wallBleedIntermediateRenderTarget2;

        // Proxies to textures of some of the above render targets.
        private ClydeTexture FovTexture => _fovRenderTarget.Texture;

        private ClydeTexture ShadowTexture => _shadowRenderTarget.Texture;

        // Sampler used to sample the FovTexture with linear filtering, used in the lighting FOV pass
        // (it uses VSM unlike final FOV).
        private GLHandle _fovFilterSampler;

        // Shader program used to calculate depth for shadows/FOV.
        // Sadly not .swsl since it has a different vertex format and such.
        private GLShaderProgram _fovCalculationProgram;

        // Occlusion geometry used to render shadows and FOV.

        // Amount of indices in _occlusionEbo, so how much we have to draw when drawing _occlusionVao.
        private int _occlusionDataLength;

        // Actual GL objects used for rendering.
        private GLBuffer _occlusionVbo;
        private GLBuffer _occlusionEbo;
        private GLHandle _occlusionVao;


        // Occlusion mask geometry that represents the area with occluders.
        // This is used to merge _wallBleedIntermediateRenderTarget2 onto _lightRenderTarget after wall bleed is done.

        // Amount of indices in _occlusionMaskEbo, so how much we have to draw when drawing _occlusionMaskVao.
        private int _occlusionMaskDataLength;

        // Actual GL objects used for rendering.
        private GLBuffer _occlusionMaskVbo;
        private GLBuffer _occlusionMaskEbo;
        private GLHandle _occlusionMaskVao;

        private unsafe void InitLighting()
        {
            LoadLightingShaders();

            RegenerateLightingRenderTargets();

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
                new RenderTargetFormatParameters(RenderTargetColorFormat.RG32F, true),
                new TextureSampleParameters {WrapMode = TextureWrapMode.Repeat},
                nameof(_fovRenderTarget));

            _fovFilterSampler = new GLHandle(GL.GenSampler());
            GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureMagFilter, (int) All.Linear);
            GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureMinFilter, (int) All.Linear);
            GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureWrapS, (int) All.Repeat);
            GL.SamplerParameter(_fovFilterSampler.Handle, SamplerParameterName.TextureWrapT, (int) All.Repeat);

            // Shadow FBO.
            _shadowRenderTarget = CreateRenderTarget((ShadowMapSize, MaxLightsPerScene),
                new RenderTargetFormatParameters(RenderTargetColorFormat.RG32F, true),
                new TextureSampleParameters {WrapMode = TextureWrapMode.Repeat, Filter = true},
                nameof(_shadowRenderTarget));
        }

        private void LoadLightingShaders()
        {
            var depthVert = ReadEmbeddedShader("shadow-depth.vert");
            var depthFrag = ReadEmbeddedShader("shadow-depth.frag");

            _fovCalculationProgram = _compileProgram(depthVert, depthFrag, "Shadow Depth Program");

            var debugShader = _resourceCache.GetResource<ShaderSourceResource>("/Shaders/Internal/depth-debug.swsl");
            _fovDebugShaderInstance = (ClydeShaderInstance) InstanceShader(debugShader.ClydeHandle);

            ClydeHandle LoadShaderHandle(string path)
            {
                var shaderSource = _resourceCache.GetResource<ShaderSourceResource>(path);
                return shaderSource.ClydeHandle;
            }

            _lightShaderHandle = LoadShaderHandle("/Shaders/Internal/light.swsl");
            _fovShaderHandle = LoadShaderHandle("/Shaders/Internal/fov.swsl");
            _fovLightShaderHandle = LoadShaderHandle("/Shaders/Internal/fov-lighting.swsl");
            _wallBleedBlurShaderHandle = LoadShaderHandle("/Shaders/Internal/wall-bleed-blur.swsl");
            _mergeWallLayerShaderHandle = LoadShaderHandle("/Shaders/Internal/wall-merge.swsl");
        }

        private void DrawFov(IEye eye)
        {
            using var _ = DebugGroup(nameof(DrawFov));

            PrepareDepthDraw(_fovRenderTarget);

            if (eye.DrawFov)
            {
                // Calculate maximum distance for the projection based on screen size.
                var screenSizeCut = ScreenSize / EyeManager.PIXELSPERMETER;
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
            _fovCalculationProgram.SetUniform("lightMatrix", lightMatrix, false);

            var baseProj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(90),
                1,
                maxDist / 1000,
                maxDist * 1.1f);

            var step = width / 4;

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

                _fovCalculationProgram.SetUniform("projectionMatrix", proj, false);
                // Shift viewport around so we write to the correct quadrant of the depth map.
                GL.Viewport(step * i, viewportY, step, 1);

                GL.DrawElements(BeginMode.TriangleStrip, _occlusionDataLength, DrawElementsType.UnsignedShort, 0);
                _debugStats.LastGLDrawCalls += 1;
            }
        }

        private void PrepareDepthDraw(RenderTarget target)
        {
            const float arbitraryDistanceMax = 1234;

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);

            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Cw);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, target.ObjectHandle.Handle);
            GL.ClearDepth(1);
            GL.ClearColor(arbitraryDistanceMax, arbitraryDistanceMax * arbitraryDistanceMax, 0, 1);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            GL.BindVertexArray(_occlusionVao.Handle);

            _fovCalculationProgram.Use();
        }

        private static void FinalizeDepthDraw()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
        }

        private void DrawLightsAndFov(Box2 worldBounds, IEye eye)
        {
            if (!_lightManager.Enabled)
            {
                return;
            }

            var map = eye.Position.MapId;

            var (lights, expandedBounds) = GetLightsToRender(map, worldBounds);

            UpdateOcclusionGeometry(map, expandedBounds, eye.Position.Position);

            DrawFov(eye);

            var shadowMatrices = new Matrix4[lights.Count];

            using (DebugGroup("Draw shadow depth"))
            {
                PrepareDepthDraw(_shadowRenderTarget);
                GL.CullFace(CullFaceMode.Back);

                if (_lightManager.DrawShadows)
                {
                    for (var i = 0; i < lights.Count; i++)
                    {
                        var (light, lightPos) = lights[i];

                        DrawOcclusionDepth(lightPos, ShadowMapSize, light.Radius, i, out shadowMatrices[i]);
                    }
                }

                FinalizeDepthDraw();
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _lightRenderTarget.ObjectHandle.Handle);
            GLClearColor(Color.FromSrgb(AmbientLightColor));
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var (lightW, lightH) = GetLightMapSize();
            GL.Viewport(0, 0, lightW, lightH);

            var lightShader = _loadedShaders[_lightShaderHandle].Program;
            lightShader.Use();

            SetTexture(TextureUnit.Texture1, ShadowTexture);
            lightShader.SetUniformTextureMaybe("shadowMap", TextureUnit.Texture1);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

            var lastRange = float.NaN;
            var lastPower = float.NaN;
            var lastColor = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
            Texture lastMask = null;

            for (var i = 0; i < lights.Count; i++)
            {
                var (component, lightPos) = lights[i];
                var transform = component.Owner.Transform;

                var circle = new Circle(lightPos, component.Radius);

                if (!circle.Intersects(worldBounds))
                {
                    continue;
                }

                Texture mask = null;
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

                if (!FloatMath.CloseTo(lastRange, component.Radius))
                {
                    lastRange = component.Radius;
                    lightShader.SetUniformMaybe("lightRange", lastRange);
                }

                if (!FloatMath.CloseTo(lastPower, component.Energy))
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
                lightShader.SetUniformMaybe("shadowMatrix", shadowMatrices[i], false);

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
            }

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            ApplyLightingFovToBuffer(eye);

            BlurOntoWalls(eye);

            MergeWallLayer();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, ScreenSize.X, ScreenSize.Y);

            _lightingReady = true;
        }

        private (List<(PointLightComponent light, Vector2 pos)> lights, Box2 expandedBounds)
            GetLightsToRender(MapId map, in Box2 worldBounds)
        {
            var lights = new List<(PointLightComponent light, Vector2 pos)>(64);
            // When culling occluders later, we can't just remove any occluders outside the worldBounds.
            // As they could still affect the shadows of (large) light sources.
            // We expand the world bounds so that it encompasses the center of every light source.
            // This should make it so no culled occluder can make a difference.
            // (if the occluder is in the current lights at all, it's still not between the light and the world bounds).
            var expandedBounds = worldBounds;

            foreach (var component in _componentManager.GetAllComponents<PointLightComponent>())
            {
                var transform = component.Owner.Transform;

                if (!component.Enabled || transform.MapID != map)
                {
                    continue;
                }

                var lightPos = transform.WorldMatrix.Transform(component.Offset);

                var lightBounds = Box2.CenteredAround(lightPos, Vector2.One * component.Radius * 2);

                if (!lightBounds.Intersects(worldBounds))
                {
                    continue;
                }

                lights.Add((component, lightPos));

                expandedBounds = expandedBounds.ExtendToContain(lightPos);

                if (lights.Count == MaxLightsPerScene)
                {
                    // TODO: Allow more than 64 lights.
                    break;
                }
            }

            return (lights, expandedBounds);
        }

        private void BlurOntoWalls(IEye eye)
        {
            using var _ = DebugGroup(nameof(BlurOntoWalls));

            GL.Disable(EnableCap.Blend);
            _setSpace(CurrentSpace.ScreenSpace);

            var shader = _loadedShaders[_wallBleedBlurShaderHandle].Program;
            shader.Use();

            shader.SetUniformMaybe("size", (Vector2) _wallBleedIntermediateRenderTarget1.Size);
            shader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            var size = _wallBleedIntermediateRenderTarget1.Size;
            GL.Viewport(0, 0, size.X, size.Y);

            // Initially we're pulling from the light render target.
            // So we set it out of the loop so
            // _wallBleedIntermediateRenderTarget2 gets bound at the end of the loop body.
            SetTexture(TextureUnit.Texture0, _lightRenderTarget.Texture);

            // Have to scale the blurring radius based on viewport size and camera zoom.
            const float refCameraHeight = 14;
            var cameraSize = eye.Zoom.Y * ScreenSize.Y / EyeManager.PIXELSPERMETER;
            // 7e-3f is just a magic factor that makes it look ok.
            var factor = 7e-3f * (refCameraHeight / cameraSize);

            // Multi-iteration gaussian blur.
            for (var i = 3; i > 0; i--)
            {
                var scale = (i + 1) * factor;
                // Set factor.
                shader.SetUniformMaybe("radius", scale);

                _wallBleedIntermediateRenderTarget1.Bind();

                // Blur horizontally to _wallBleedIntermediateRenderTarget1.
                shader.SetUniformMaybe("direction", Vector2.UnitX);
                _drawQuad(Vector2.Zero, ScreenSize, Matrix3.Identity, shader);

                SetTexture(TextureUnit.Texture0, _wallBleedIntermediateRenderTarget1.Texture);
                _wallBleedIntermediateRenderTarget2.Bind();

                // Blur vertically to _wallBleedIntermediateRenderTarget2.
                shader.SetUniformMaybe("direction", Vector2.UnitY);
                _drawQuad(Vector2.Zero, ScreenSize, Matrix3.Identity, shader);

                SetTexture(TextureUnit.Texture0, _wallBleedIntermediateRenderTarget2.Texture);
            }

            GL.Enable(EnableCap.Blend);
            _setSpace(CurrentSpace.WorldSpace);
        }

        private void MergeWallLayer()
        {
            using var _ = DebugGroup(nameof(MergeWallLayer));

            _lightRenderTarget.Bind();

            GL.Viewport(0, 0, _lightRenderTarget.Size.X, _lightRenderTarget.Size.Y);
            GL.Disable(EnableCap.Blend);

            var shader = _loadedShaders[_mergeWallLayerShaderHandle].Program;
            shader.Use();

            var tex = _wallBleedIntermediateRenderTarget2.Texture;
            SetTexture(TextureUnit.Texture0, tex);

            shader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);

            GL.BindVertexArray(_occlusionMaskVao.Handle);

            GL.DrawElements(PrimitiveType.TriangleFan, _occlusionMaskDataLength, DrawElementsType.UnsignedShort,
                IntPtr.Zero);

            GL.Enable(EnableCap.Blend);
        }

        private void ApplyFovToBuffer(IEye eye)
        {
            // Applies FOV to the final framebuffer.

            var fovShader = _loadedShaders[_fovShaderHandle].Program;
            fovShader.Use();

            SetTexture(TextureUnit.Texture0, FovTexture);

            fovShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
            fovShader.SetUniformMaybe("shadowMatrix", _fovProjection, false);
            fovShader.SetUniformMaybe("center", eye.Position.Position);

            DrawBlit(fovShader);
        }

        private void ApplyLightingFovToBuffer(IEye eye)
        {
            // Applies FOV to the lighting framebuffer.

            var fovShader = _loadedShaders[_fovLightShaderHandle].Program;
            fovShader.Use();

            SetTexture(TextureUnit.Texture0, FovTexture);

            // Have to bind sampler to use linear filtering on the shadow map here.
            // VSM wants it.
            GL.BindSampler(0, _fovFilterSampler.Handle);

            fovShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
            fovShader.SetUniformMaybe("shadowMatrix", _fovProjection, false);
            fovShader.SetUniformMaybe("center", eye.Position.Position);

            DrawBlit(fovShader);

            GL.BindSampler(0, 0);
        }

        private void DrawBlit(GLShaderProgram shader)
        {
            _drawQuad(_eyeManager.ScreenToMap((-1, -1)).Position,
                _eyeManager.ScreenToMap(ScreenSize + Vector2i.One).Position,
                Matrix3.Identity, shader);
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

            // TODO: More accurate bounds check.
            // Yeah just enlarge it a wee bit to accomodate for large occluders maybe. Oh well.
            expandedBounds.Enlarged(2);

            var arrayBuffer = ArrayPool<Vector3>.Shared.Rent(maxOccluders * 8);
            var indexBuffer = ArrayPool<ushort>.Shared.Rent(maxOccluders * 20);

            var arrayMaskBuffer = ArrayPool<Vector2>.Shared.Rent(maxOccluders * 4);
            var indexMaskBuffer = ArrayPool<ushort>.Shared.Rent(maxOccluders * 5);

            try
            {
                var ai = 0;
                var ami = 0;
                var ii = 0;
                var imi = 0;

                foreach (var occluder in _componentManager.GetAllComponents<ClientOccluderComponent>())
                {
                    var transform = occluder.Owner.Transform;
                    if (!occluder.Enabled || transform.MapID != map)
                    {
                        continue;
                    }

                    var worldTransform = transform.WorldMatrix;
                    var box = occluder.BoundingBox;

                    var centerPos = (worldTransform.R0C2, worldTransform.R1C2);

                    if (!expandedBounds.Contains(centerPos))
                    {
                        continue;
                    }

                    // So uh, angle 0 = east... Apparently...
                    // We account for that here so I don't go insane.
                    var (tlX, tlY) = worldTransform.Transform(box.BottomLeft);
                    var (trX, trY) = worldTransform.Transform(box.TopLeft);
                    var (brX, brY) = worldTransform.Transform(box.TopRight);
                    var (blX, blY) = worldTransform.Transform(box.BottomRight);

                    // Vertices used as main occlusion geometry.
                    // We always send all of these (see below) to keep code complexity down.
                    arrayBuffer[ai + 0] = new Vector3(tlX, tlY, polygonHeight);
                    arrayBuffer[ai + 1] = new Vector3(tlX, tlY, -polygonHeight);
                    arrayBuffer[ai + 2] = new Vector3(trX, trY, polygonHeight);
                    arrayBuffer[ai + 3] = new Vector3(trX, trY, -polygonHeight);
                    arrayBuffer[ai + 4] = new Vector3(brX, brY, polygonHeight);
                    arrayBuffer[ai + 5] = new Vector3(brX, brY, -polygonHeight);
                    arrayBuffer[ai + 6] = new Vector3(blX, blY, polygonHeight);
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
                    var no = occluder.Occluding.HasFlag(OccluderDir.North);
                    var so = occluder.Occluding.HasFlag(OccluderDir.South);
                    var eo = occluder.Occluding.HasFlag(OccluderDir.East);
                    var wo = occluder.Occluding.HasFlag(OccluderDir.West);

                    // Do visibility tests for occluders (described above).
                    var tlV = dTlX > 0 && !wo || dTlY < 0 && !no;
                    var trV = dTrX < 0 && !eo || dTrY < 0 && !no;
                    var blV = dBlX > 0 && !wo || dBlY > 0 && !so;
                    var brV = dBrX < 0 && !eo || dBrY > 0 && !so;

                    // Handle faces, rules described above.

                    // North face.
                    if (!no || !tlV && !trV)
                    {
                        indexBuffer[ii + 0] = (ushort) (ai + 0);
                        indexBuffer[ii + 1] = (ushort) (ai + 1);
                        indexBuffer[ii + 2] = (ushort) (ai + 2);
                        indexBuffer[ii + 3] = (ushort) (ai + 3);
                        indexBuffer[ii + 4] = ushort.MaxValue; // Primitive restart
                        ii += 5;
                    }

                    // East face.
                    if (!eo || !brV && !trV)
                    {
                        indexBuffer[ii + 0] = (ushort) (ai + 2);
                        indexBuffer[ii + 1] = (ushort) (ai + 3);
                        indexBuffer[ii + 2] = (ushort) (ai + 4);
                        indexBuffer[ii + 3] = (ushort) (ai + 5);
                        indexBuffer[ii + 4] = ushort.MaxValue; // Primitive restart
                        ii += 5;
                    }

                    // South face.
                    if (!so || !brV && !blV)
                    {
                        indexBuffer[ii + 0] = (ushort) (ai + 4);
                        indexBuffer[ii + 1] = (ushort) (ai + 5);
                        indexBuffer[ii + 2] = (ushort) (ai + 6);
                        indexBuffer[ii + 3] = (ushort) (ai + 7);
                        indexBuffer[ii + 4] = ushort.MaxValue; // Primitive restart
                        ii += 5;
                    }

                    // West face.
                    if (!wo || !blV && !tlV)
                    {
                        indexBuffer[ii + 0] = (ushort) (ai + 6);
                        indexBuffer[ii + 1] = (ushort) (ai + 7);
                        indexBuffer[ii + 2] = (ushort) (ai + 0);
                        indexBuffer[ii + 3] = (ushort) (ai + 1);
                        indexBuffer[ii + 4] = ushort.MaxValue; // Primitive restart
                        ii += 5;
                    }

                    if (_quartResLights)
                    {
                        // On low-res lights we bias the occlusion mask inwards.
                        // This avoids wall lighting going onto the tile next to them at certain tile alignments.
                        // It's inwards to avoid seeing disconnected shadows on wall edges.
                        const float bias = 0.5f / EyeManager.PIXELSPERMETER;

                        if (!no)
                        {
                            tlY -= bias;
                            trY -= bias;
                        }

                        if (!eo)
                        {
                            trX -= bias;
                            brX -= bias;
                        }

                        if (!so)
                        {
                            blY += bias;
                            brY += bias;
                        }

                        if (!wo)
                        {
                            blX += bias;
                            tlX += bias;
                        }
                    }

                    // Generate mask geometry.
                    arrayMaskBuffer[ami + 0] = new Vector2(tlX, tlY);
                    arrayMaskBuffer[ami + 1] = new Vector2(trX, trY);
                    arrayMaskBuffer[ami + 2] = new Vector2(brX, brY);
                    arrayMaskBuffer[ami + 3] = new Vector2(blX, blY);

                    // Generate mask indices.
                    indexMaskBuffer[imi + 0] = (ushort) (ami + 0);
                    indexMaskBuffer[imi + 1] = (ushort) (ami + 1);
                    indexMaskBuffer[imi + 2] = (ushort) (ami + 2);
                    indexMaskBuffer[imi + 3] = (ushort) (ami + 3);
                    indexMaskBuffer[imi + 4] = ushort.MaxValue; // Primitive restart

                    ai += 8;
                    ami += 4;
                    imi += 5;
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

        private void RegenerateLightingRenderTargets()
        {
            // All of these depend on screen size so they have to be re-created if it changes.

            var lightMapSize = GetLightMapSize();
            var lightMapSizeQuart = GetLightMapSize(true);
            const RenderTargetColorFormat lightMapColorFormat = RenderTargetColorFormat.R11FG11FB10F;
            var lightMapSampleParameters = new TextureSampleParameters {Filter = true};

            _lightRenderTarget?.Delete();
            _wallMaskRenderTarget?.Delete();
            _wallBleedIntermediateRenderTarget1?.Delete();
            _wallBleedIntermediateRenderTarget2?.Delete();

            _wallMaskRenderTarget = CreateRenderTarget(ScreenSize, RenderTargetColorFormat.R8,
                name: nameof(_wallMaskRenderTarget));

            _lightRenderTarget = CreateRenderTarget(lightMapSize, lightMapColorFormat,
                lightMapSampleParameters,
                nameof(_lightRenderTarget));

            _wallBleedIntermediateRenderTarget1 = CreateRenderTarget(lightMapSizeQuart, lightMapColorFormat,
                lightMapSampleParameters,
                nameof(_wallBleedIntermediateRenderTarget1));

            _wallBleedIntermediateRenderTarget2 = CreateRenderTarget(lightMapSizeQuart, lightMapColorFormat,
                lightMapSampleParameters,
                nameof(_wallBleedIntermediateRenderTarget2));
        }

        private Vector2i GetLightMapSize(bool? overrideSetting = null)
        {
            var setting = overrideSetting ?? _quartResLights;
            if (!setting)
            {
                return (ScreenSize.X, ScreenSize.Y);
            }

            var w = (int) Math.Ceiling(ScreenSize.X / 2f);
            var h = (int) Math.Ceiling(ScreenSize.Y / 2f);

            return (w, h);
        }

        protected override void HighResLightsChanged(bool newValue)
        {
            _quartResLights = !newValue;
            if (_lightRenderTarget == null)
            {
                return;
            }

            RegenerateLightingRenderTargets();
        }
    }
}
