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
using OGLTextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private const int ShadowMapSize = 512;
        private const int FovMapSize = 2048;
        private const int MaxLightsPerScene = 64;

        private ClydeShaderInstance _fovDebugShaderInstance;

        private ClydeHandle _lightShaderHandle;
        private ClydeHandle _fovShaderHandle;
        private ClydeHandle _fovLightShaderHandle;

        private Matrix4 _fovProjection;

        private RenderTarget _fovRenderTarget;
        private ClydeTexture _fovDepthTextureObject;

        private RenderTarget _shadowRenderTarget;
        private ClydeTexture _shadowTextureObject;

        private RenderTarget _wallMaskRenderTarget;
        private ClydeTexture _wallMaskTextureObject;

        private GLShaderProgram _fovCalculationProgram;

        private RenderTarget LightRenderTarget;

        private int _occlusionDataLength;
        private GLBuffer _occlusionVbo;
        private GLBuffer _occlusionEbo;
        private GLHandle _occlusionVao;

        private unsafe void InitLighting()
        {
            LoadLightingShaders();

            RegenerateLightingRenderTargets();

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

            // FOV FBO.
            _fovRenderTarget = CreateRenderTarget((FovMapSize, 1),
                new RenderTargetFormatParameters(RenderTargetColorFormat.R32F, true),
                new TextureSampleParameters {WrapMode = TextureWrapMode.Repeat}, "FOV depth render target");

            _fovDepthTextureObject = _fovRenderTarget.Texture;

            // Shadow FBO.
            _shadowRenderTarget = CreateRenderTarget((ShadowMapSize, MaxLightsPerScene),
                new RenderTargetFormatParameters(RenderTargetColorFormat.R32F, true),
                new TextureSampleParameters {WrapMode = TextureWrapMode.Repeat}, "Shadow depth render target.");

            _shadowTextureObject = _shadowRenderTarget.Texture;
        }

        private void LoadLightingShaders()
        {
            var depthVert = ReadEmbeddedShader("shadow-depth.vert");
            var depthFrag = ReadEmbeddedShader("shadow-depth.frag");

            _fovCalculationProgram = _compileProgram(depthVert, depthFrag, "Shadow Depth Program");

            var debugShader = _resourceCache.GetResource<ShaderSourceResource>("/Shaders/Internal/depth-debug.swsl");
            _fovDebugShaderInstance = (ClydeShaderInstance) InstanceShader(debugShader.ClydeHandle);

            var shaderSource = _resourceCache.GetResource<ShaderSourceResource>("/Shaders/Internal/light.swsl");
            _lightShaderHandle = shaderSource.ClydeHandle;

            shaderSource = _resourceCache.GetResource<ShaderSourceResource>("/Shaders/Internal/fov.swsl");
            _fovShaderHandle = shaderSource.ClydeHandle;

            shaderSource = _resourceCache.GetResource<ShaderSourceResource>("/Shaders/Internal/fov-lighting.swsl");
            _fovLightShaderHandle = shaderSource.ClydeHandle;
        }

        private void DrawFov(IEye eye)
        {
            var screenSizeCut = ScreenSize / EyeManager.PIXELSPERMETER;
            var maxDist = (float) Math.Max(screenSizeCut.X, screenSizeCut.Y);

            DrawOcclusionDepth(eye.Position.Position, FovMapSize, maxDist, 0, out _fovProjection);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Disable(EnableCap.DepthTest);
        }

        private void DrawOcclusionDepth(Vector2 lightPos, int width, float maxDist, int viewportY,
            out Matrix4 projMatrix)
        {
            projMatrix = default; // Gets overriden below.

            var (posX, posY) = lightPos;
            var lightMatrix = Matrix4.CreateTranslation(-posX, -posY, 0);

            _fovCalculationProgram.SetUniform("lightMatrix", lightMatrix, false);

            var baseProj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(90),
                1,
                maxDist / 1000,
                maxDist * 1.1f);

            var step = width / 4;

            for (var i = 0; i < 4; i++)
            {
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
                    projMatrix = proj;
                }

                _fovCalculationProgram.SetUniform("projectionMatrix", proj, false);
                GL.Viewport(step * i, viewportY, step, 1);

                GL.DrawElements(BeginMode.TriangleStrip, _occlusionDataLength, DrawElementsType.UnsignedShort, 0);
                _debugStats.LastGLDrawCalls += 1;
            }
        }

        private void PrepareDepthDraw(RenderTarget target)
        {
            const float arbitraryDistanceMax = 12345;

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, target.ObjectHandle.Handle);
            GL.ClearDepth(1);
            GL.ClearColor(arbitraryDistanceMax, arbitraryDistanceMax, arbitraryDistanceMax, 1);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            GL.BindVertexArray(_occlusionVao.Handle);

            _fovCalculationProgram.Use();
        }

        private void DrawLightsAndFov(Box2 worldBounds, IEye eye)
        {
            if (!_lightManager.Enabled)
            {
                return;
            }

            GenerateWallMask(eye.Position.MapId);

            UpdateOcclusionGeometry(eye.Position.MapId);

            PrepareDepthDraw(_fovRenderTarget);

            DrawFov(eye);

            var map = _eyeManager.CurrentMap;

            var lights = new List<(PointLightComponent light, Vector2 pos)>();
            var maxRadius = 123f;

            foreach (var component in _componentManager.GetAllComponents<PointLightComponent>())
            {
                if (!component.Enabled || component.Owner.Transform.MapID != map)
                {
                    continue;
                }

                var transform = component.Owner.Transform;
                var lightPos = transform.WorldMatrix.Transform(component.Offset);

                var lightBounds = Box2.CenteredAround(lightPos, Vector2.One * component.Radius * 2);

                if (!lightBounds.Intersects(worldBounds))
                {
                    continue;
                }

                lights.Add((component, lightPos));
                maxRadius = Math.Max(maxRadius, component.Radius);

                if (lights.Count == MaxLightsPerScene)
                {
                    // TODO: Allow more than 64 lights.
                    break;
                }
            }

            var shadowMatrices = new Matrix4[lights.Count];

            // Draw shadow depth.
            PrepareDepthDraw(_shadowRenderTarget);

            if (_lightManager.DrawShadows)
            {
                for (var i = 0; i < lights.Count; i++)
                {
                    var (light, lightPos) = lights[i];

                    DrawOcclusionDepth(lightPos, ShadowMapSize, light.Radius, i, out shadowMatrices[i]);
                }
            }

            GL.Disable(EnableCap.DepthTest);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, LightRenderTarget.ObjectHandle.Handle);
            ClearColor(Color.FromSrgb(AmbientLightColor));
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var (lightW, lightH) = GetLightMapSize();
            GL.Viewport(0, 0, lightW, lightH);

            var lightShader = _loadedShaders[_lightShaderHandle].Program;
            lightShader.Use();

            SetTexture(TextureUnit.Texture1, _shadowTextureObject);
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

                var lightBounds = Box2.CenteredAround(lightPos, Vector2.One * component.Radius * 2);

                if (!lightBounds.Intersects(worldBounds))
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
                lightShader.SetUniformMaybe("lightIndex", (i + 0.5f) / _shadowTextureObject.Height);
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

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, ScreenSize.X, ScreenSize.Y);

            _lightingReady = true;
        }

        private void GenerateWallMask(MapId map)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _wallMaskRenderTarget.ObjectHandle.Handle);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            foreach (var occluder in _componentManager.GetAllComponents<OccluderComponent>())
            {
                if (occluder.Owner.Transform.MapID != map)
                {
                    continue;
                }

                _renderHandle.SetModelTransform(occluder.Owner.Transform.WorldMatrix);
                _renderHandle.DrawingHandleWorld.DrawTextureRect(Texture.White, occluder.BoundingBox);
            }

            FlushRenderQueue();
        }

        private void ApplyFovToBuffer(IEye eye)
        {
            var fovShader = _loadedShaders[_fovShaderHandle].Program;
            fovShader.Use();

            SetTexture(TextureUnit.Texture0, _fovDepthTextureObject);
            SetTexture(TextureUnit.Texture1, _wallMaskTextureObject);

            fovShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
            fovShader.SetUniformMaybe("shadowMatrix", _fovProjection, false);
            fovShader.SetUniformMaybe("center", eye.Position.Position);
            fovShader.SetUniformTextureMaybe("wallMask", TextureUnit.Texture1);

            DrawBlit(fovShader);
        }

        private void ApplyLightingFovToBuffer(IEye eye)
        {
            var fovShader = _loadedShaders[_fovLightShaderHandle].Program;
            fovShader.Use();

            SetTexture(TextureUnit.Texture0, _fovDepthTextureObject);

            fovShader.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
            fovShader.SetUniformMaybe("shadowMatrix", _fovProjection, false);
            fovShader.SetUniformMaybe("center", eye.Position.Position);

            DrawBlit(fovShader);
        }

        private void DrawBlit(GLShaderProgram shader)
        {
            _drawQuad(_eyeManager.ScreenToMap((-1, -1)).Position,
                _eyeManager.ScreenToMap(ScreenSize + Vector2i.One).Position,
                Matrix3.Identity, shader);
        }

        private void UpdateOcclusionGeometry(MapId map)
        {
            const int maxOccluders = 2048;

            GL.BindVertexArray(_occlusionVao.Handle);

            var arrayBuffer = ArrayPool<Vector3>.Shared.Rent(maxOccluders * 8);
            var indexBuffer = ArrayPool<ushort>.Shared.Rent(maxOccluders * 11);

            var ai = 0;
            var ii = 0;

            try
            {
                foreach (var occluder in _componentManager.GetAllComponents<OccluderComponent>())
                {
                    if (occluder.Owner.Transform.MapID != map)
                    {
                        continue;
                    }

                    var worldTransform = occluder.Owner.Transform.WorldMatrix;

                    var box = occluder.BoundingBox;

                    var (tlX, tlY) = worldTransform.Transform(box.TopLeft);
                    var (trX, trY) = worldTransform.Transform(box.TopRight);
                    var (blX, blY) = worldTransform.Transform(box.BottomLeft);
                    var (brX, brY) = worldTransform.Transform(box.BottomRight);

                    const float polygonHeight = 500;

                    arrayBuffer[ai + 0] = new Vector3(tlX, tlY, polygonHeight);
                    arrayBuffer[ai + 1] = new Vector3(tlX, tlY, -polygonHeight);
                    arrayBuffer[ai + 2] = new Vector3(trX, trY, polygonHeight);
                    arrayBuffer[ai + 3] = new Vector3(trX, trY, -polygonHeight);
                    arrayBuffer[ai + 4] = new Vector3(brX, brY, polygonHeight);
                    arrayBuffer[ai + 5] = new Vector3(brX, brY, -polygonHeight);
                    arrayBuffer[ai + 6] = new Vector3(blX, blY, polygonHeight);
                    arrayBuffer[ai + 7] = new Vector3(blX, blY, -polygonHeight);

                    indexBuffer[ii + 0] = (ushort) (ai + 0);
                    indexBuffer[ii + 1] = (ushort) (ai + 1);
                    indexBuffer[ii + 2] = (ushort) (ai + 2);
                    indexBuffer[ii + 3] = (ushort) (ai + 3);
                    indexBuffer[ii + 4] = (ushort) (ai + 4);
                    indexBuffer[ii + 5] = (ushort) (ai + 5);
                    indexBuffer[ii + 6] = (ushort) (ai + 6);
                    indexBuffer[ii + 7] = (ushort) (ai + 7);
                    indexBuffer[ii + 8] = (ushort) (ai + 0);
                    indexBuffer[ii + 9] = (ushort) (ai + 1);
                    indexBuffer[ii + 10] = ushort.MaxValue; // Primitive restart

                    ai += 8;
                    ii += 11;
                }

                _occlusionDataLength = ii;

                _occlusionVbo.Reallocate(arrayBuffer.AsSpan(..ai));
                _occlusionEbo.Reallocate(indexBuffer.AsSpan(..ii));
            }
            finally
            {
                ArrayPool<Vector3>.Shared.Return(arrayBuffer);
                ArrayPool<ushort>.Shared.Return(indexBuffer);
            }
        }

        private void RegenerateLightingRenderTargets()
        {
            LightRenderTarget?.Delete();
            LightRenderTarget = CreateRenderTarget(GetLightMapSize(), RenderTargetColorFormat.R11FG11FB10F,
                new TextureSampleParameters {Filter = true},
                nameof(LightRenderTarget));

            _wallMaskRenderTarget?.Delete();
            _wallMaskRenderTarget = CreateRenderTarget(ScreenSize, RenderTargetColorFormat.R8,
                name: nameof(_wallMaskRenderTarget));

            _wallMaskTextureObject = _wallMaskRenderTarget.Texture;
        }

        private Vector2i GetLightMapSize()
        {
            if (!_quartResLights)
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
            if (LightRenderTarget == null)
            {
                return;
            }

            RegenerateLightingRenderTargets();
        }
    }
}
