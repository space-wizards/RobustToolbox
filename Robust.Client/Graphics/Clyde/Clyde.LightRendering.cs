using System;
using System.Buffers;
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

        private ClydeShaderInstance _fovDebugShaderInstance;

        private RenderTarget _fovRenderTarget;
        private GLShaderProgram _fovCalculationProgram;

        private RenderTarget LightRenderTarget;
        private ClydeTexture _fovDepthTextureObject;

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
            _fovRenderTarget = CreateRenderTarget((ShadowMapSize, 1),
                new RenderTargetFormatParameters(RenderTargetColorFormat.R32F, true),
                new TextureSampleParameters {WrapMode = TextureWrapMode.Repeat}, "FOV depth render target");

            _fovDepthTextureObject = _fovRenderTarget.Texture;
        }

        private void LoadLightingShaders()
        {
            var depthVert = ReadEmbeddedShader("shadow-depth.vert");
            var depthFrag = ReadEmbeddedShader("shadow-depth.frag");

            _fovCalculationProgram = _compileProgram(depthVert, depthFrag, "Shadow Depth Program");

            var debugShader = _resourceCache.GetResource<ShaderSourceResource>("/Shaders/Internal/depth-debug.swsl");
            _fovDebugShaderInstance = (ClydeShaderInstance) InstanceShader(debugShader.ClydeHandle);
        }

        private void DrawFov(Box2 worldBounds, IEye eye)
        {
            var screenSizeCut = ScreenSize / EyeManager.PIXELSPERMETER;
            var maxDist = (float) Math.Max(screenSizeCut.X, screenSizeCut.Y);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fovRenderTarget.ObjectHandle.Handle);
            GL.ClearDepth(1);
            GL.ClearColor(maxDist, maxDist, maxDist, 1);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            GL.BindVertexArray(_occlusionVao.Handle);

            _fovCalculationProgram.Use();

            var lightMatrix = Matrix4.CreateTranslation(-eye.Position.X, -eye.Position.Y, 0);
            _fovCalculationProgram.SetUniform("lightMatrix", lightMatrix, false);

            var baseProj =
                Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1, maxDist / 1000,
                    maxDist * 1.1f);

            const int step = ShadowMapSize / 4;

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

                _fovCalculationProgram.SetUniform("projectionMatrix", proj, false);
                GL.Viewport(step * i, 0, step, 1);

                GL.DrawElements(BeginMode.TriangleStrip, _occlusionDataLength, DrawElementsType.UnsignedShort, 0);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Disable(EnableCap.DepthTest);
        }

        private void DrawLightsAndFov(Box2 worldBounds, IEye eye)
        {
            if (!_lightManager.Enabled)
            {
                return;
            }

            UpdateOcclusionGeometry(eye.Position.MapId);

            DrawFov(worldBounds, eye);

            var map = _eyeManager.CurrentMap;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, LightRenderTarget.ObjectHandle.Handle);
            var converted = Color.FromSrgb(new Color(0.1f, 0.1f, 0.1f));
            GL.ClearColor(converted.R, converted.G, converted.B, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var (lightW, lightH) = GetLightMapSize();
            GL.Viewport(0, 0, lightW, lightH);

            _lightShader.Use();

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

            var lastRange = float.NaN;
            var lastPower = float.NaN;
            var lastColor = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
            Texture lastMask = null;

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
                    var maskHandle = _loadedTextures[((ClydeTexture) maskTexture).TextureId].OpenGLObject;
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, maskHandle.Handle);
                    lastMask = maskTexture;
                    _lightShader.SetUniformTexture("lightMask", TextureUnit.Texture0);
                }

                if (!FloatMath.CloseTo(lastRange, component.Radius))
                {
                    lastRange = component.Radius;
                    _lightShader.SetUniform("lightRange", lastRange);
                }

                if (!FloatMath.CloseTo(lastPower, component.Energy))
                {
                    lastPower = component.Energy;
                    _lightShader.SetUniform("lightPower", lastPower);
                }

                if (lastColor != component.Color)
                {
                    lastColor = component.Color;
                    _lightShader.SetUniform("lightColor", lastColor);
                }

                _lightShader.SetUniform("lightCenter", lightPos);

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

                _drawQuad(-offset, offset, ref matrix, _lightShader);
            }

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, ScreenSize.X, ScreenSize.Y);

            _lightingReady = true;
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
                name: nameof(LightRenderTarget));
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
