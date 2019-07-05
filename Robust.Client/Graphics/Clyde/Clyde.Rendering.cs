using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private RenderHandle _renderHandle;

        /// <summary>
        ///     Are we current rendering screen space or world space? Some code works differently between the two.
        /// </summary>
        private CurrentSpace _currentSpace;

        private bool _lightingReady;

        /// <summary>
        ///     The current model matrix we would use.
        ///     Necessary since certain drawing operations mess with it still.
        /// </summary>
        private Matrix3 _currentModelMatrix = Matrix3.Identity;

        // The amount of quads we can render with ushort indices, leaving open 65536 for primitive restart.
        private const ushort MaxBatchQuads = (2 << 13) - 1; // In human terms: (2**16/4)-1

        private readonly Vertex2D[] BatchVertexData = new Vertex2D[MaxBatchQuads * 4];

        // Need 5 indices per quad: 4 to draw the quad with triangle strips and another one as primitive restart.
        private readonly ushort[] BatchIndexData = new ushort[MaxBatchQuads * 5];

        private int BatchIndex;

        private int? BatchingTexture;

        // We break batching when modulate changes too.
        // This simplifies the rendering and catches most cases for now.
        // For example all the walls have a single color.
        // Yes this can be optimized later.
        private Color? BatchingModulate;

        /// <summary>
        ///     If true, re-allocate buffer objects with BufferData instead of using BufferSubData.
        /// </summary>
        private bool _reallocateBuffers;

        private ProjViewMatrices _defaultMatricesScreen;
        private ProjViewMatrices _defaultMatricesWorld;

        private ClydeHandle _currentShader;

        private float _renderTime;

        private bool _isScissoring;

        public void Render()
        {
            // Basic pre-render busywork.
            // Clear screen to black.
            ClearFramebuffer(Color.Black);

            // Update shared UBOs.
            _updateUniformConstants();
            _updateDefaultScreenMatrices();

            _setSpace(CurrentSpace.ScreenSpace);

            // Short path to render only the splash.
            if (_drawingSplash)
            {
                _drawSplash(_renderHandle);
                _flushRenderHandle(_renderHandle);
                _window.SwapBuffers();
                return;
            }

            _updateDefaultWorldMatrices();

            void RenderOverlays(OverlaySpace space)
            {
                using (DebugGroup($"Overlays: {space}"))
                {
                    foreach (var overlay in _overlayManager.AllOverlays
                        .Where(o => o.Space == space)
                        .OrderBy(o => o.ZIndex))
                    {
                        overlay.ClydeRender(_renderHandle);
                    }

                    _flushRenderHandle(_renderHandle);
                }
            }

            RenderOverlays(OverlaySpace.ScreenSpaceBelowWorld);

            _setSpace(CurrentSpace.WorldSpace);

            using (DebugGroup("Lights"))
            {
                _drawLights();

                _lightingReady = true;
            }

            using (DebugGroup("Grids"))
            {
                _drawGrids();
            }

            using (DebugGroup("Entities"))
            {
                var entityList = new List<SpriteComponent>(100);
                var map = _eyeManager.CurrentMap;

                foreach (var entity in _entityManager.GetEntities())
                {
                    if (!entity.Transform.IsMapTransform || entity.Transform.MapID != map ||
                        !entity.TryGetComponent(out SpriteComponent sprite) || !sprite.Visible)
                    {
                        continue;
                    }

                    entityList.Add(sprite);
                }

                entityList.Sort((a, b) =>
                {
                    var cmp = ((int) a.DrawDepth).CompareTo((int) b.DrawDepth);
                    if (cmp != 0)
                    {
                        return cmp;
                    }

                    return a.RenderOrder.CompareTo(b.RenderOrder);
                });

                foreach (var entity in entityList)
                {
                    entity.OpenGLRender(_renderHandle.DrawingHandleWorld);
                }

                _flushRenderHandle(_renderHandle);
            }

            RenderOverlays(OverlaySpace.WorldSpace);

            _lightingReady = false;

            _setSpace(CurrentSpace.ScreenSpace);

            RenderOverlays(OverlaySpace.ScreenSpace);

            using (DebugGroup("UI"))
            {
                _userInterfaceManager.Render(_renderHandle);
                _flushRenderHandle(_renderHandle);
            }

            // And finally, swap those buffers!
            _window.SwapBuffers();
        }

        private void _drawSplash(IRenderHandle handle)
        {
            var texture = _resourceCache.GetResource<TextureResource>("/Textures/Logo/logo.png").Texture;

            handle.DrawingHandleScreen.DrawTexture(texture, (ScreenSize - texture.Size) / 2);
        }

        /// <summary>
        ///     Updates uniform constants shared to all shaders, such as time and pixel size.
        /// </summary>
        private void _updateUniformConstants()
        {
            var constants = new UniformConstants(Vector2.One / ScreenSize, _renderTime);
            _writeBuffer(UniformConstantsUBO, constants);
        }

        private void _updateDefaultScreenMatrices()
        {
            var viewMatrixScreen = Matrix3.Identity;

            // Screen projection matrix.
            var projMatrixScreen = Matrix3.Identity;
            projMatrixScreen.R0C0 = 2f / _window.Width;
            projMatrixScreen.R1C1 = -2f / _window.Height;
            projMatrixScreen.R0C2 = -1;
            projMatrixScreen.R1C2 = 1;

            _defaultMatricesScreen = new ProjViewMatrices(projMatrixScreen, viewMatrixScreen);
        }

        private void _updateDefaultWorldMatrices()
        {
            var eye = _eyeManager.CurrentEye;

            var toScreen = _eyeManager.WorldToScreen(eye.Position.Position);
            // Round camera position to a screen pixel to avoid weird issues on odd screen sizes.
            toScreen = ((float) Math.Floor(toScreen.X), (float) Math.Floor(toScreen.Y));
            var cameraWorldAdjusted = _eyeManager.ScreenToWorld(toScreen);

            var viewMatrixWorld = Matrix3.Identity;
            viewMatrixWorld.R0C0 = 1 / eye.Zoom.X;
            viewMatrixWorld.R1C1 = 1 / eye.Zoom.Y;
            viewMatrixWorld.R0C2 = -cameraWorldAdjusted.X / eye.Zoom.X;
            viewMatrixWorld.R1C2 = -cameraWorldAdjusted.Y / eye.Zoom.Y;

            var projMatrixWorld = Matrix3.Identity;
            projMatrixWorld.R0C0 = EyeManager.PIXELSPERMETER * 2f / _window.Width;
            projMatrixWorld.R1C1 = EyeManager.PIXELSPERMETER * 2f / _window.Height;

            _defaultMatricesWorld = new ProjViewMatrices(projMatrixWorld, viewMatrixWorld);
        }

        private void _drawLights()
        {
            if (!_lightManager.Enabled)
            {
                return;
            }

            var map = _eyeManager.CurrentMap;

            void DrawLight(Vector2 pos, float range, float power, Color color)
            {
                _lightShader.SetUniform("lightCenter", pos);
                _lightShader.SetUniform("lightRange", range);
                _lightShader.SetUniform("lightPower", power);
                _lightShader.SetUniform("lightColor", color);

                var a = pos - new Vector2(range, range);
                var b = pos + new Vector2(range, range);

                var matrix = Matrix3.Identity;

                _drawQuad(a, b, ref matrix, _lightShader);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, LightRenderTarget.ObjectHandle.Handle);
            var converted = Color.FromSrgb(new Color(0.1f, 0.1f, 0.1f));
            GL.ClearColor(converted.R, converted.G, converted.B, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var (lightW, lightH) = _lightMapSize();
            GL.Viewport(0, 0, lightW, lightH);

            _lightShader.Use();

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

            foreach (var component in _componentManager.GetAllComponents<PointLightComponent>())
            {
                if (component.State != LightState.On || component.Owner.Transform.MapID != map)
                {
                    continue;
                }

                var lightPos = component.Owner.Transform.WorldMatrix.Transform(component.Offset);
                DrawLight(lightPos, component.Radius, component.Energy, component.Color);
            }

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, _window.Width, _window.Height);
        }

        private void _setProjViewMatrices(in ProjViewMatrices matrices)
        {
            ProjViewUBO.Use();
            _writeBuffer(ProjViewUBO, matrices);
        }

        private void _processCommandList(RenderCommandList list)
        {
            foreach (ref var command in list.RenderCommands)
            {
                switch (command.Type)
                {
                    case RenderCommandType.Texture:
                        _drawCommandTexture(ref command.Texture);
                        break;

                    case RenderCommandType.Line:
                        _flushBatchBuffer();
                        _drawCommandLine(ref command.Line);
                        break;

                    case RenderCommandType.ModelMatrix:
                        _currentModelMatrix = command.ModelMatrix.Matrix;
                        break;

                    case RenderCommandType.Scissor:
                        _flushBatchBuffer();
                        var oldIsScissoring = _isScissoring;
                        _isScissoring = command.Scissor.EnableScissor;
                        if (_isScissoring)
                        {
                            if (!oldIsScissoring)
                            {
                                GL.Enable(EnableCap.ScissorTest);
                            }

                            ref var s = ref command.Scissor.Scissor;
                            // Don't forget to flip it, these coordinates have bottom left as origin.
                            GL.Scissor(s.Left, _window.Height - s.Bottom, s.Width, s.Height);
                        }
                        else if (oldIsScissoring)
                        {
                            GL.Disable(EnableCap.ScissorTest);
                        }

                        break;

                    case RenderCommandType.ViewMatrix:
                        _flushBatchBuffer();
                        ProjViewMatrices matrices;
                        ref var pullProj = ref _currentSpace == CurrentSpace.ScreenSpace
                            ? ref _defaultMatricesScreen
                            : ref _defaultMatricesWorld;

                        matrices = new ProjViewMatrices(pullProj, command.ViewMatrix.Matrix);
                        _setProjViewMatrices(matrices);
                        break;

                    case RenderCommandType.UseShader:
                        _flushBatchBuffer();
                        if (command.UseShader.Handle == _currentShader.Handle)
                        {
                            break;
                        }

                        _currentShader = (ClydeHandle) command.UseShader.Handle;
                        break;

                    case RenderCommandType.SwitchSpace:
                        _flushBatchBuffer();
                        _setSpace(command.SwitchSpace.NewSpace);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void _drawCommandTexture(ref RenderCommandTexture command)
        {
            if (BatchingTexture.HasValue)
            {
                DebugTools.Assert(BatchingModulate.HasValue);
                if (BatchingTexture.Value != command.TextureId ||
                    BatchingModulate.Value != command.Modulate)
                {
                    _flushBatchBuffer();
                    BatchingTexture = command.TextureId;
                    BatchingModulate = command.Modulate;
                }
            }
            else
            {
                BatchingTexture = command.TextureId;
                BatchingModulate = command.Modulate;
            }

            var loadedTexture = _loadedTextures[BatchingTexture.Value];
            UIBox2 sr;
            if (command.HasSubRegion)
            {
                var (w, h) = loadedTexture.Size;
                var csr = command.SubRegion;
                if (_currentSpace == CurrentSpace.WorldSpace)
                {
                    sr = new UIBox2(csr.Left / w, csr.Top / h, csr.Right / w, csr.Bottom / h);
                }
                else
                {
                    sr = new UIBox2(csr.Left / w, csr.Bottom / h, csr.Right / w, csr.Top / h);
                }
            }
            else
            {
                if (_currentSpace == CurrentSpace.WorldSpace)
                {
                    sr = new UIBox2(0, 0, 1, 1);
                }
                else
                {
                    sr = new UIBox2(0, 1, 1, 0);
                }
            }

            var bl = _currentModelMatrix.Transform(command.PositionA);
            var br = _currentModelMatrix.Transform(new Vector2(command.PositionB.X, command.PositionA.Y));
            var tr = _currentModelMatrix.Transform(command.PositionB);
            var tl = _currentModelMatrix.Transform(new Vector2(command.PositionA.X, command.PositionB.Y));
            var vIdx = BatchIndex * 4;
            BatchVertexData[vIdx + 0] = new Vertex2D(bl, sr.BottomLeft);
            BatchVertexData[vIdx + 1] = new Vertex2D(br, sr.BottomRight);
            BatchVertexData[vIdx + 2] = new Vertex2D(tl, sr.TopLeft);
            BatchVertexData[vIdx + 3] = new Vertex2D(tr, sr.TopRight);
            var nIdx = BatchIndex * 5;
            var tIdx = (ushort) (BatchIndex * 4);
            BatchIndexData[nIdx + 0] = tIdx;
            BatchIndexData[nIdx + 1] = (ushort) (tIdx + 1);
            BatchIndexData[nIdx + 2] = (ushort) (tIdx + 2);
            BatchIndexData[nIdx + 3] = (ushort) (tIdx + 3);
            BatchIndexData[nIdx + 4] = ushort.MaxValue;
            BatchIndex += 1;
            if (BatchIndex >= MaxBatchQuads)
            {
                throw new NotImplementedException("Can't batch things this big yet sorry.");
            }
        }

        private void _drawCommandLine(ref RenderCommandLine renderCommandLine)
        {
            var loaded = _loadedShaders[_currentShader.Handle];
            var program = loaded.Program;

            program.Use();
            program.SetUniformMaybe(UniModUV, new Vector4(0, 0, 1, 1));
            program.SetUniformMaybe(UniModulate, renderCommandLine.Color);

            var white = _loadedTextures[((ClydeTexture) Texture.White).TextureId].OpenGLObject;
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, white.Handle);

            GL.ActiveTexture(TextureUnit.Texture1);
            if (_lightingReady && loaded.HasLighting)
            {
                var lightTexture = _loadedTextures[LightRenderTarget.Texture.TextureId].OpenGLObject;
                GL.BindTexture(TextureTarget.Texture2D, lightTexture.Handle);
            }
            else
            {
                GL.BindTexture(TextureTarget.Texture2D, white.Handle);
            }

            program.SetUniformTextureMaybe(UniMainTexture, TextureUnit.Texture0);
            program.SetUniformTextureMaybe(UniLightTexture, TextureUnit.Texture1);

            var a = renderCommandLine.PositionA;
            var b = renderCommandLine.PositionB;

            GL.BindVertexArray(LineVAO.Handle);
            var rectTransform = Matrix3.Identity;
            (rectTransform.R0C0, rectTransform.R1C1) = b - a;
            (rectTransform.R0C2, rectTransform.R1C2) = a;
            rectTransform.Multiply(ref _currentModelMatrix);
            program.SetUniformMaybe(UniModelMatrix, rectTransform);
            GL.DrawArrays(PrimitiveType.Lines, 0, 2);
        }

        private void _drawQuad(Vector2 a, Vector2 b, ref Matrix3 modelMatrix, ShaderProgram program)
        {
            GL.BindVertexArray(QuadVAO.Handle);
            var rectTransform = Matrix3.Identity;
            (rectTransform.R0C0, rectTransform.R1C1) = b - a;
            (rectTransform.R0C2, rectTransform.R1C2) = a;
            rectTransform.Multiply(ref modelMatrix);
            program.SetUniformMaybe(UniModelMatrix, rectTransform);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        /// <summary>
        ///     Flush the render handle, processing and re-pooling all the command lists.
        /// </summary>
        private void _flushRenderHandle(RenderHandle handle)
        {
            _processCommandList(handle.CommandList);
            handle.CommandList.RenderCommands.Clear();

            _flushBatchBuffer();

            // Reset renderer state.
            _currentShader = _defaultShader;
            _currentModelMatrix = Matrix3.Identity;
            _currentShader = _defaultShader;
            _disableScissor();
            _setProjViewMatrices(_defaultMatricesScreen);
        }

        private void _flushBatchBuffer()
        {
            if (!BatchingTexture.HasValue)
            {
                return;
            }

            DebugTools.Assert(BatchingTexture.HasValue);
            var loadedTexture = _loadedTextures[BatchingTexture.Value];

            GL.BindVertexArray(BatchVAO.Handle);

            _writeBuffer(BatchVBO, new Span<Vertex2D>(BatchVertexData, 0, BatchIndex * 4));
            _writeBuffer(BatchEBO, new Span<ushort>(BatchIndexData, 0, BatchIndex * 5));

            var loaded = _loadedShaders[_currentShader.Handle];
            var program = loaded.Program;

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, loadedTexture.OpenGLObject.Handle);

            GL.ActiveTexture(TextureUnit.Texture1);
            if (_lightingReady && loaded.HasLighting)
            {
                var lightTexture = _loadedTextures[LightRenderTarget.Texture.TextureId].OpenGLObject;
                GL.BindTexture(TextureTarget.Texture2D, lightTexture.Handle);
            }
            else
            {
                var white = _loadedTextures[((ClydeTexture) Texture.White).TextureId].OpenGLObject;
                GL.BindTexture(TextureTarget.Texture2D, white.Handle);
            }

            program.Use();

            program.SetUniformTextureMaybe(UniMainTexture, TextureUnit.Texture0);
            program.SetUniformTextureMaybe(UniLightTexture, TextureUnit.Texture1);

            // Model matrix becomes identity since it's built into the batch mesh.
            program.SetUniformMaybe(UniModelMatrix, Matrix3.Identity);
            // Reset ModUV to ensure it's identity and doesn't touch anything.
            program.SetUniformMaybe(UniModUV, new Vector4(0, 0, 1, 1));
            // Set modulate.
            DebugTools.Assert(BatchingModulate.HasValue);
            program.SetUniformMaybe(UniModulate, BatchingModulate.Value);
            program.SetUniformMaybe(UniTexturePixelSize, Vector2.One / loadedTexture.Size);
            // Enable primitive restart & do that draw.
            GL.Enable(EnableCap.PrimitiveRestart);
            GL.PrimitiveRestartIndex(ushort.MaxValue);
            GL.DrawElements(PrimitiveType.TriangleStrip, BatchIndex * 5, DrawElementsType.UnsignedShort, 0);
            GL.Disable(EnableCap.PrimitiveRestart);

            // Reset batch state.
            BatchIndex = 0;
            BatchingTexture = null;
            BatchingModulate = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _disableScissor()
        {
            if (_isScissoring)
            {
                GL.Disable(EnableCap.ScissorTest);
            }

            _isScissoring = false;
        }

        private void _setSpace(CurrentSpace newSpace)
        {
            _currentSpace = newSpace;

            switch (newSpace)
            {
                case CurrentSpace.ScreenSpace:
                    _setProjViewMatrices(_defaultMatricesScreen);
                    break;
                case CurrentSpace.WorldSpace:
                    _setProjViewMatrices(_defaultMatricesWorld);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newSpace), newSpace, null);
            }
        }

        private void ClearFramebuffer(Color color)
        {
            GL.ClearColor(color.ConvertOpenTK());
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }

        // Uses either glBufferData or glBufferSubData depending on _reallocateBuffers.
        private void _writeBuffer<T>(Buffer buffer, Span<T> data) where T : unmanaged
        {
            if (_reallocateBuffers)
            {
                buffer.Reallocate(data);
            }
            else
            {
                buffer.WriteSubData(data);
            }
        }

        private void _writeBuffer<T>(Buffer buffer, in T data) where T : unmanaged
        {
            if (_reallocateBuffers)
            {
                buffer.Reallocate(data);
            }
            else
            {
                buffer.WriteSubData(data);
            }
        }

        private sealed class RenderHandle : IRenderHandle
        {
            private readonly Clyde _clyde;
            public readonly RenderCommandList CommandList = new RenderCommandList();

            public DrawingHandleScreen DrawingHandleScreen { get; }
            public DrawingHandleWorld DrawingHandleWorld { get; }

            public RenderHandle(Clyde clyde)
            {
                _clyde = clyde;
                DrawingHandleScreen = new DrawingHandleScreenImpl(this);
                DrawingHandleWorld = new DrawingHandleWorldImpl(this);
            }

            private void SetModelTransform(in Matrix3 matrix)
            {
                ref var command = ref CommandList.RenderCommands.AllocAdd();
                command.Type = RenderCommandType.ModelMatrix;

                command.ModelMatrix.Matrix = matrix;
            }

            private void SetViewTransform(in Matrix3 matrix)
            {
                ref var command = ref CommandList.RenderCommands.AllocAdd();
                command.Type = RenderCommandType.ViewMatrix;

                command.ViewMatrix.Matrix = matrix;
            }

            private void DrawTexture(Texture texture, Vector2 a, Vector2 b, Color modulate, UIBox2? subRegion)
            {
                switch (texture)
                {
                    case AtlasTexture atlas:
                    {
                        texture = atlas.SourceTexture;
                        if (subRegion.HasValue)
                        {
                            var offset = atlas.SubRegion.TopLeft;
                            subRegion = new UIBox2(
                                subRegion.Value.TopLeft + offset,
                                subRegion.Value.BottomRight + offset);
                        }
                        else
                        {
                            subRegion = atlas.SubRegion;
                        }

                        break;
                    }
                }

                ref var command = ref CommandList.RenderCommands.AllocAdd();
                command.Type = RenderCommandType.Texture;

                var clydeTexture = (ClydeTexture) texture;
                command.Texture.TextureId = clydeTexture.TextureId;
                command.Texture.PositionA = a;
                command.Texture.PositionB = b;
                command.Texture.Modulate = modulate;
                if (subRegion.HasValue)
                {
                    command.Texture.SubRegion = subRegion.Value;
                    command.Texture.HasSubRegion = true;
                }
                else
                {
                    command.Texture.HasSubRegion = false;
                }
            }

            private void SetScissor(UIBox2i? scissorBox)
            {
                ref var command = ref CommandList.RenderCommands.AllocAdd();
                command.Type = RenderCommandType.Scissor;

                command.Scissor.EnableScissor = scissorBox.HasValue;
                if (scissorBox.HasValue)
                {
                    command.Scissor.Scissor = scissorBox.Value;
                }
            }

            private void DrawEntity(IEntity entity, in Vector2 position)
            {
                var sprite = entity.GetComponent<SpriteComponent>();

                // Switch rendering to world space.
                {
                    ref var commandWorldSpace = ref CommandList.RenderCommands.AllocAdd();
                    commandWorldSpace.Type = RenderCommandType.SwitchSpace;
                    commandWorldSpace.SwitchSpace.NewSpace = CurrentSpace.WorldSpace;
                }

                {
                    // Change view matrix to put entity where we need.
                    ref var commandViewMatrix = ref CommandList.RenderCommands.AllocAdd();
                    commandViewMatrix.Type = RenderCommandType.ViewMatrix;

                    var ofsX = position.X - _clyde._window.Width / 2f;
                    var ofsY = position.Y - _clyde._window.Height / 2f;
                    ref var viewMatrix = ref commandViewMatrix.ViewMatrix.Matrix;
                    viewMatrix = Matrix3.Identity;
                    viewMatrix.R0C2 = ofsX / EyeManager.PIXELSPERMETER;
                    viewMatrix.R1C2 = -ofsY / EyeManager.PIXELSPERMETER;
                }

                // Draw the entity.
                sprite.OpenGLRender(DrawingHandleWorld, false);

                // Reset to screen space
                {
                    ref var commandScreenSpace = ref CommandList.RenderCommands.AllocAdd();
                    commandScreenSpace.Type = RenderCommandType.SwitchSpace;
                    commandScreenSpace.SwitchSpace.NewSpace = CurrentSpace.ScreenSpace;
                }
            }

            private void DrawLine(Vector2 a, Vector2 b, Color color)
            {
                ref var command = ref CommandList.RenderCommands.AllocAdd();
                command.Type = RenderCommandType.Line;

                command.Line.PositionA = a;
                command.Line.PositionB = b;
                command.Line.Color = color;
            }

            private void UseShader(ShaderInstance shader)
            {
                var clydeShader = (ClydeShaderInstance) shader;

                ref var command = ref CommandList.RenderCommands.AllocAdd();
                command.Type = RenderCommandType.UseShader;

                command.UseShader.Handle = clydeShader?.ShaderHandle.Handle ?? _clyde._defaultShader.Handle;
            }



            private sealed class DrawingHandleScreenImpl : DrawingHandleScreen
            {
                private readonly RenderHandle _renderHandle;

                public DrawingHandleScreenImpl(RenderHandle renderHandle)
                {
                    _renderHandle = renderHandle;
                }

                public override void SetTransform(in Matrix3 matrix)
                {
                    _renderHandle.SetModelTransform(matrix);
                }

                public override void UseShader(ShaderInstance shader)
                {
                    _renderHandle.UseShader(shader);
                }

                public override void DrawCircle(Vector2 position, float radius, Color color)
                {
                    // TODO: Implement this.
                }

                public override void DrawLine(Vector2 from, Vector2 to, Color color)
                {
                    _renderHandle.DrawLine(from, to, color * Modulate);
                }

                public override void SetScissor(UIBox2i? scissorBox)
                {
                    _renderHandle.SetScissor(scissorBox);
                }

                public override void DrawRect(UIBox2 rect, Color color, bool filled = true)
                {
                    // TODO: Hollow rect drawing.
                    DrawTextureRect(Texture.White, rect, color * Modulate);
                }

                public override void DrawTextureRectRegion(Texture texture, UIBox2 rect, UIBox2? subRegion = null,
                    Color? modulate = null)
                {
                    var color = (modulate ?? Color.White) * Modulate;
                    _renderHandle.DrawTexture(texture, rect.TopLeft, rect.BottomRight, color,
                        subRegion);
                }

                internal override void DrawEntity(IEntity entity, Vector2 screenPosition)
                {
                    _renderHandle.DrawEntity(entity, screenPosition);
                }
            }

            private sealed class DrawingHandleWorldImpl : DrawingHandleWorld
            {
                private readonly RenderHandle _renderHandle;

                public DrawingHandleWorldImpl(RenderHandle renderHandle)
                {
                    _renderHandle = renderHandle;
                }

                public override void SetTransform(in Matrix3 matrix)
                {
                    _renderHandle.SetModelTransform(matrix);
                }

                public override void UseShader(ShaderInstance shader)
                {
                    _renderHandle.UseShader(shader);
                }

                public override void DrawCircle(Vector2 position, float radius, Color color)
                {
                    // TODO: Implement this.
                }

                public override void DrawLine(Vector2 from, Vector2 to, Color color)
                {
                    _renderHandle.DrawLine(from, to, color * Modulate);
                }

                public override void DrawRect(Box2 rect, Color color, bool filled = true)
                {
                    // TODO: Hollow rect drawing.
                    DrawTextureRect(Texture.White, rect, color * Modulate);
                }

                public override void DrawTextureRectRegion(Texture texture, Box2 rect, UIBox2? subRegion = null,
                    Color? modulate = null)
                {
                    var color = (modulate ?? Color.White) * Modulate;

                    _renderHandle.DrawTexture(texture, rect.BottomLeft, rect.TopRight, color, subRegion);
                }
            }
        }

        // Use a tagged union to store all render commands.
        // This significantly improves performance vs doing sum types via inheritance.
        // Also means I don't have to declare a pool for every command type.
        [StructLayout(LayoutKind.Explicit)]
        private struct RenderCommand
        {
            [FieldOffset(0)] public RenderCommandType Type;

            [FieldOffset(4)] public RenderCommandTexture Texture;
            [FieldOffset(4)] public RenderCommandModelMatrix ModelMatrix;
            [FieldOffset(4)] public RenderCommandViewMatrix ViewMatrix;
            [FieldOffset(4)] public RenderCommandScissor Scissor;
            [FieldOffset(4)] public RenderCommandUseShader UseShader;
            [FieldOffset(4)] public RenderCommandLine Line;
            [FieldOffset(4)] public RenderCommandSwitchSpace SwitchSpace;
        }

        private struct RenderCommandTexture
        {
            public int TextureId;
            public bool HasSubRegion;
            public UIBox2 SubRegion;

            public Vector2 PositionA;
            public Vector2 PositionB;

            public Color Modulate;
        }

        private struct RenderCommandModelMatrix
        {
            public Matrix3 Matrix;
        }

        private struct RenderCommandViewMatrix
        {
            public Matrix3 Matrix;
        }

        private struct RenderCommandScissor
        {
            public bool EnableScissor;
            public UIBox2i Scissor;
        }

        private struct RenderCommandUseShader
        {
            public int Handle;
        }

        private struct RenderCommandLine
        {
            public Vector2 PositionA;
            public Vector2 PositionB;

            public Color Color;
        }

        private struct RenderCommandSwitchSpace
        {
            public CurrentSpace NewSpace;
        }

        private enum RenderCommandType
        {
            Texture,
            Line,

            ModelMatrix,
            ViewMatrix,
            SwitchSpace,

            UseShader,
            Scissor,
        }

        private enum CurrentSpace
        {
            ScreenSpace = 0,
            WorldSpace = 1,
        }

        private struct PopDebugGroup : IDisposable
        {
            private readonly Clyde _clyde;

            public PopDebugGroup(Clyde clyde)
            {
                _clyde = clyde;
            }

            public void Dispose()
            {
                _clyde._popDebugGroupMaybe();
            }
        }

        /// <summary>
        ///     A list of rendering commands to execute in order. Pooled.
        /// </summary>
        // ReSharper disable once ClassNeverInstantiated.Local
        private class RenderCommandList
        {
            public readonly RefList<RenderCommand> RenderCommands = new RefList<RenderCommand>();
        }
    }
}
