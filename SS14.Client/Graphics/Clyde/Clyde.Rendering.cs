using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using SS14.Client.GameObjects;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.ResourceManagement;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        // ReSharper disable once IdentifierTypo
        private static readonly (uint, string) DbgGroupSSBW = (1, "Overlays: Screen Space Below World");
        private static readonly (uint, string) DbgGroupGrids = (2, "Grids");
        private static readonly (uint, string) DbgGroupEntities = (3, "Entities");
        private static readonly (uint, string) DbgGroupUI = (4, "User Interface");
        private static readonly (uint, string) DbgGroupWorldOverlay = (5, "Overlays: World");

        /// <summary>
        ///     The current model matrix we would use.
        ///     Necessary since certain drawing operations mess with it still.
        /// </summary>
        private Matrix3 _currentModelMatrix = Matrix3.Identity;

        private bool _batchModelMatricesNeedsUpdate = true;

        /// <summary>
        ///     Are we current rendering screen space or world space? Some code works differently between the two.
        /// </summary>
        private CurrentSpace _currentSpace;

        // The minimum amount of consecutive textures that have to be batch-able before we actually bother to batch them.
        // BufferSubData has a non zero overhead and a few draw calls is much better.
        private const int TextureBatchThreshold = 8;

        // The max amount of vertices in one batch. Each quad is 4 vertices so...
        private const ushort MaxBatchVertices = 16333 * 4;

        // (2**16/4)-1, so that we can use ushort indices in the index buffer
        // and leave open the primitive restart index (65535).
        private const ushort MaxBatchQuads = 16333;

        private readonly Vertex2D[] BatchVertexData = new Vertex2D[MaxBatchVertices];

        // Need 5 indices per quad: 4 to draw the quad with triangle strips and another one as primitive restart.
        private readonly ushort[] BatchIndexData = new ushort[MaxBatchQuads * 5];

        private readonly RefList<BatchCommand> BatchBuffer = new RefList<BatchCommand>();
        private readonly RefList<Matrix3> BatchModelMatrices = new RefList<Matrix3>();

        private int? BatchingTexture;

        // We break batching when modulate changes too.
        // This simplifies the rendering and catches most cases for now.
        // For example all the walls have a single color.
        // Yes this can be optimized later.
        private Color? BatchingModulate;

        private RenderHandle _renderHandle;

        /// <summary>
        ///     If true, re-allocate buffer objects with BufferData instead of using BufferSubData.
        /// </summary>
        private bool _reallocateBuffers = false;

        /// <summary>
        ///     Log when the command pools go dry and we need to re-allocate objects.
        /// </summary>
        private bool _logPoolOverdraw = false;

        private bool _isScissoring = false;

        private ProjViewMatrices _combinedDefaultMatricesWorld;
        private ProjViewMatrices _combinedDefaultMatricesScreen;

        private int _currentShader;

        private float _renderTime;

        public void Render(FrameEventArgs args)
        {
            if (GameController.Mode != GameController.DisplayMode.OpenGL)
            {
                return;
            }

            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            Vertex2DProgram.Use();

            // Update uniform constants UBO.
            _renderTime += args?.Elapsed ?? 0;
            var constants = new UniformConstants(Vector2.One / ScreenSize, _renderTime);
            UniformConstantsUBO.Use();
            if (_reallocateBuffers)
            {
                UniformConstantsUBO.Reallocate(constants);
            }
            else
            {
                UniformConstantsUBO.WriteSubData(constants);
            }

            _currentModelMatrix = Matrix3.Identity;
            _batchModelMatricesNeedsUpdate = true;

            // We hand out this handle so external code can generate command lists for us.
            _renderHandle._drawingHandles.Clear();
            _currentSpace = CurrentSpace.ScreenSpace;

            // Screen view matrix is identity. Easy huh.
            var viewMatrixScreen = Matrix3.Identity;

            // Screen projection matrix.
            var projMatrixScreen = Matrix3.Identity;
            projMatrixScreen.R0C0 = 2f / _window.Width;
            projMatrixScreen.R1C1 = -2f / _window.Height;
            projMatrixScreen.R0C2 = -1;
            projMatrixScreen.R1C2 = 1;

            _combinedDefaultMatricesScreen = new ProjViewMatrices(projMatrixScreen, viewMatrixScreen);

            _setProjViewMatrices(_combinedDefaultMatricesScreen);

            if (_drawingSplash)
            {
                _displaySplash(_renderHandle);
                _flushRenderHandle(_renderHandle);
                _window.SwapBuffers();
                return;
            }

            var eye = _eyeManager.CurrentEye;

            // World projection matrix.
            var projMatrixWorld = Matrix3.Identity;
            projMatrixWorld.R0C0 = EyeManager.PIXELSPERMETER * 2f / _window.Width;
            projMatrixWorld.R1C1 = EyeManager.PIXELSPERMETER * 2f / _window.Height;

            // World view matrix.
            var viewMatrixWorld = Matrix3.Identity;
            viewMatrixWorld.R0C0 = 1 / eye.Zoom.X;
            viewMatrixWorld.R1C1 = 1 / eye.Zoom.Y;
            viewMatrixWorld.R0C2 = -eye.Position.X / eye.Zoom.X;
            viewMatrixWorld.R1C2 = -eye.Position.Y / eye.Zoom.Y;

            _combinedDefaultMatricesWorld = new ProjViewMatrices(projMatrixWorld, viewMatrixWorld);

            _pushDebugGroupMaybe(DbgGroupSSBW);

            // Render ScreenSpaceBelowWorld overlays.
            foreach (var overlay in _overlayManager.AllOverlays
                .Where(o => o.Space == OverlaySpace.ScreenSpaceBelowWorld)
                .OrderBy(o => o.ZIndex))
            {
                overlay.OpenGLRender(_renderHandle);
            }

            _flushRenderHandle(_renderHandle);

            _popDebugGroupMaybe();

            _pushDebugGroupMaybe(DbgGroupGrids);

            _currentSpace = CurrentSpace.WorldSpace;
            // Render grids. Very hardcoded right now.
            var map = _eyeManager.CurrentMap;

            var tex = _resourceCache.GetResource<TextureResource>("/Textures/Tiles/floor_steel.png");
            var loadedTex = _loadedTextures[((OpenGLTexture) tex.Texture).OpenGLTextureId];
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, loadedTex.OpenGLObject.Handle);

            GL.BindVertexArray(BatchVAO.Handle);

            _setProjViewMatrices(_combinedDefaultMatricesWorld);

            GL.Enable(EnableCap.PrimitiveRestart);
            GL.PrimitiveRestartIndex(ushort.MaxValue);

            Vertex2DProgram.Use();
            Vertex2DProgram.SetUniform(UniModUV, new Vector4(0, 0, 1, 1));
            Vertex2DProgram.SetUniform(UniModulate, Color.White);
            foreach (var grid in _mapManager.GetMap(map).GetAllGrids())
            {
                ushort nth = 0;
                var model = Matrix3.Identity;
                model.R0C2 = grid.WorldPosition.X;
                model.R1C2 = grid.WorldPosition.Y;
                Vertex2DProgram.SetUniform(UniModelMatrix, model);

                foreach (var tileRef in grid.GetAllTiles())
                {
                    var vIdx = nth * 4;
                    BatchVertexData[vIdx + 0] = new Vertex2D(tileRef.X + 1, tileRef.Y, 1, 0, 0);
                    BatchVertexData[vIdx + 1] = new Vertex2D(tileRef.X, tileRef.Y, 0, 0, 0);
                    BatchVertexData[vIdx + 2] = new Vertex2D(tileRef.X + 1, tileRef.Y - 1f, 1, 1, 0);
                    BatchVertexData[vIdx + 3] = new Vertex2D(tileRef.X, tileRef.Y - 1f, 0, 1, 0);
                    var nIdx = nth * 5;
                    var tIdx = (ushort) (nth * 4);
                    BatchIndexData[nIdx + 0] = tIdx;
                    BatchIndexData[nIdx + 1] = (ushort) (tIdx + 1);
                    BatchIndexData[nIdx + 2] = (ushort) (tIdx + 2);
                    BatchIndexData[nIdx + 3] = (ushort) (tIdx + 3);
                    // Use primitive restart to shave off one single index per quad.
                    // Whew.
                    BatchIndexData[nIdx + 4] = ushort.MaxValue;
                    if (++nth >= MaxBatchQuads)
                    {
                        throw new NotImplementedException("Can't render grids that are that big yet sorry.");
                    }
                }

                if (nth == 0)
                {
                    continue;
                }

                BatchVBO.Use();
                BatchEBO.Use();
                var vertexData = new Span<Vertex2D>(BatchVertexData, 0, nth * 4);
                var indexData = new Span<ushort>(BatchIndexData, 0, nth * 5);
                if (_reallocateBuffers)
                {
                    BatchVBO.Reallocate(vertexData);
                    BatchEBO.Reallocate(indexData);
                }
                else
                {
                    BatchVBO.WriteSubData(vertexData);
                    BatchEBO.WriteSubData(indexData);
                }

                GL.DrawElements(PrimitiveType.TriangleStrip, nth * 5, DrawElementsType.UnsignedShort, 0);
            }

            GL.Disable(EnableCap.PrimitiveRestart);

            _popDebugGroupMaybe();

            _pushDebugGroupMaybe(DbgGroupEntities);

            // Use a SINGLE drawing handle for all entities.
            var drawingHandle = _renderHandle.CreateHandleWorld();

            var entityList = new List<SpriteComponent>(100);

            // Draw entities.
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

                return a.Owner.Uid.CompareTo(b.Owner.Uid);
            });

            foreach (var entity in entityList)
            {
                entity.OpenGLRender(drawingHandle);
            }

            _flushRenderHandle(_renderHandle);

            _popDebugGroupMaybe();

            _pushDebugGroupMaybe(DbgGroupWorldOverlay);

            // Render ScreenSpaceBelowWorld overlays.
            foreach (var overlay in _overlayManager.AllOverlays
                .Where(o => o.Space == OverlaySpace.WorldSpace)
                .OrderBy(o => o.ZIndex))
            {
                overlay.OpenGLRender(_renderHandle);
            }

            _flushRenderHandle(_renderHandle);

            _popDebugGroupMaybe();

            _pushDebugGroupMaybe(DbgGroupUI);

            _setProjViewMatrices(_combinedDefaultMatricesScreen);

            // Render UI.
            _currentSpace = CurrentSpace.ScreenSpace;
            _userInterfaceManager.Render(_renderHandle);

            _flushRenderHandle(_renderHandle);

            _popDebugGroupMaybe();

            _window.SwapBuffers();
        }

        private void _setProjViewMatrices(in ProjViewMatrices matrices)
        {
            ProjViewUBO.Use();
            if (_reallocateBuffers)
            {
                ProjViewUBO.Reallocate(matrices);
            }
            else
            {
                ProjViewUBO.WriteSubData(matrices);
            }
        }

        private void _processCommandList(RenderCommandList list)
        {
            foreach (ref var command in list.RenderCommands)
            {
                switch (command.Type)
                {
                    case RenderCommandType.Texture:
                        _drawCommandTexture(ref command, true, ref _currentModelMatrix);
                        break;
                    case RenderCommandType.Transform:
                        _currentModelMatrix = command.TransformMatrix;
                        _batchModelMatricesNeedsUpdate = true;
                        break;
                    case RenderCommandType.Scissor:
                        _flushBatchBuffer();
                        var oldIsScissoring = _isScissoring;
                        _isScissoring = command.EnableScissor;
                        if (_isScissoring)
                        {
                            if (!oldIsScissoring)
                            {
                                GL.Enable(EnableCap.ScissorTest);
                            }

                            ref var s = ref command.Scissor;
                            // Don't forget to flip it, these coordinates have bottom left as origin.
                            GL.Scissor(s.Left, _window.Height - s.Bottom, s.Width, s.Height);
                        }
                        else if (oldIsScissoring)
                        {
                            GL.Disable(EnableCap.ScissorTest);
                        }

                        break;
                    case RenderCommandType.SwitchSpace:
                        _flushBatchBuffer();
                        _currentSpace = command.NewSpace;
                        break;
                    case RenderCommandType.ChangeViewMatrix:
                        _flushBatchBuffer();
                        ProjViewMatrices matrices;
                        switch (_currentSpace)
                        {
                            case CurrentSpace.ScreenSpace:
                                matrices = new ProjViewMatrices(_combinedDefaultMatricesScreen, command.ViewMatrix);
                                break;
                            case CurrentSpace.WorldSpace:
                                matrices = new ProjViewMatrices(_combinedDefaultMatricesWorld, command.ViewMatrix);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        _setProjViewMatrices(matrices);
                        break;
                    case RenderCommandType.ResetViewMatrix:
                        _flushBatchBuffer();

                        switch (_currentSpace)
                        {
                            case CurrentSpace.ScreenSpace:
                                _setProjViewMatrices(_combinedDefaultMatricesScreen);
                                break;
                            case CurrentSpace.WorldSpace:
                                _setProjViewMatrices(_combinedDefaultMatricesWorld);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;
                    case RenderCommandType.UseShader:
                        if (command.ShaderHandle == _currentShader)
                        {
                            break;
                        }

                        _flushBatchBuffer();
                        _currentShader = command.ShaderHandle;
                        break;
                }
            }
        }

        private void _drawCommandTexture(
            ref RenderCommand renderCommandTexture,
            bool doBatching,
            ref Matrix3 modelMatrix)
        {
            if (doBatching)
            {
                if (BatchingTexture.HasValue)
                {
                    DebugTools.Assert(BatchingModulate.HasValue);
                    if (BatchingTexture.Value != renderCommandTexture.TextureId ||
                        BatchingModulate.Value != renderCommandTexture.Modulate)
                    {
                        _flushBatchBuffer();
                        BatchingTexture = renderCommandTexture.TextureId;
                        BatchingModulate = renderCommandTexture.Modulate;
                    }
                }
                else
                {
                    BatchingTexture = renderCommandTexture.TextureId;
                    BatchingModulate = renderCommandTexture.Modulate;
                }

                ref var batchCommand = ref BatchBuffer.AllocAdd();
                batchCommand.PositionA = renderCommandTexture.PositionA;
                batchCommand.PositionB = renderCommandTexture.PositionB;
                batchCommand.SubRegion = renderCommandTexture.SubRegion;
                batchCommand.HasSubRegion = renderCommandTexture.HasSubRegion;
                batchCommand.ArrayIndex = renderCommandTexture.ArrayIndex;
                if (_batchModelMatricesNeedsUpdate)
                {
                    _batchModelMatricesNeedsUpdate = false;
                    ref var model = ref BatchModelMatrices.AllocAdd();
                    model = _currentModelMatrix;
                }

                batchCommand.TransformIndex = BatchModelMatrices.Count - 1;

                return;
            }

            var loadedTexture = _loadedTextures[renderCommandTexture.TextureId];
            var loaded = _loadedShaders[_currentShader];
            var program = loaded.Program;

            program.Use();

            GL.BindVertexArray(QuadVAO.Handle);
            // Use QuadVBO to render a single quad and modify the model matrix to position it where we need it.
            if (renderCommandTexture.HasSubRegion)
            {
                // If the texture is in an atlas we have to set modifyUV in the shader,
                // so that the shader translated the quad VBO 0->1 tex coords to the sub region needed.
                var subRegion = renderCommandTexture.SubRegion;
                var (w, h) = loadedTexture.Size;
                if (_currentSpace == CurrentSpace.ScreenSpace)
                {
                    // Flip Y texture coordinates to fix screen space flipping.
                    var vec = new Vector4(
                        subRegion.Left / w,
                        subRegion.Bottom / h,
                        subRegion.Right / w,
                        subRegion.Top / h);
                    program.SetUniformMaybe(UniModUV, vec);
                }
                else
                {
                    var vec = new Vector4(
                        subRegion.Left / w,
                        subRegion.Top / h,
                        subRegion.Right / w,
                        subRegion.Bottom / h);
                    program.SetUniformMaybe(UniModUV, vec);
                }
            }
            else
            {
                if (_currentSpace == CurrentSpace.ScreenSpace)
                {
                    // Flip Y texture coordinates to fix screen space flipping.
                    program.SetUniformMaybe(UniModUV, new Vector4(0, 1, 1, 0));
                }
                else
                {
                    program.SetUniformMaybe(UniModUV, new Vector4(0, 0, 1, 1));
                }
            }

            program.SetUniformMaybe(UniModulate, renderCommandTexture.Modulate);
            program.SetUniformMaybe(UniTexturePixelSize, Vector2.One/loadedTexture.Size);

            var rectTransform = Matrix3.Identity;
            (rectTransform.R0C0, rectTransform.R1C1) = renderCommandTexture.PositionB - renderCommandTexture.PositionA;
            (rectTransform.R0C2, rectTransform.R1C2) = renderCommandTexture.PositionA;
            rectTransform.Multiply(ref modelMatrix);
            program.SetUniformMaybe(UniModelMatrix, rectTransform);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, loadedTexture.OpenGLObject.Handle);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        /// <summary>
        ///     Flush the render handle, processing and re-pooling all the command lists.
        /// </summary>
        private void _flushRenderHandle(RenderHandle handle)
        {
            foreach (var commandList in handle._commandLists)
            {
                _processCommandList(commandList);
                _returnCommandList(commandList);
                _flushBatchBuffer();
                _currentShader = _defaultShader;
                _currentModelMatrix = Matrix3.Identity;
            }

            foreach (var (drawingHandle, _) in handle._drawingHandles)
            {
                drawingHandle.Dispose();
            }

            handle._drawingHandles.Clear();
            handle._commandLists.Clear();
            _currentShader = _defaultShader;
            _disableScissor();
        }

        private void _flushBatchBuffer()
        {
            // TODO: Tons of things are still unimplemented.
            // Pretty much just look at _drawCommandTexture and see what's missing.
            if (BatchBuffer.Count < TextureBatchThreshold)
            {
                foreach (ref var batchCommand in BatchBuffer)
                {
                    var textureCommand = new RenderCommand
                    {
                        // ReSharper disable once PossibleInvalidOperationException
                        TextureId = BatchingTexture.Value,
                        PositionA = batchCommand.PositionA,
                        PositionB = batchCommand.PositionB,
                        SubRegion = batchCommand.SubRegion,
                        HasSubRegion = batchCommand.HasSubRegion,
                        // ReSharper disable once PossibleInvalidOperationException
                        Modulate = BatchingModulate.Value,
                        ArrayIndex = batchCommand.ArrayIndex,
                    };
                    ref var transform = ref BatchModelMatrices[batchCommand.TransformIndex];
                    _drawCommandTexture(ref textureCommand, false, ref transform);
                }

                BatchBuffer.Clear();
                BatchModelMatrices.Clear();
                _batchModelMatricesNeedsUpdate = true;
                BatchingTexture = null;
                BatchingModulate = null;
                return;
            }

            DebugTools.Assert(BatchingTexture.HasValue);
            var loadedTexture = _loadedTextures[BatchingTexture.Value];
            ushort quadIndex = 0;
            foreach (ref var command in BatchBuffer)
            {
                ref var transform = ref BatchModelMatrices[command.TransformIndex];
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
                    sr = new UIBox2(0, 0, 1, 1);
                }

                var arrayIndex = command.ArrayIndex - 1;
                var bl = transform.Transform(command.PositionA);
                var br = transform.Transform(new Vector2(command.PositionB.X, command.PositionA.Y));
                var tr = transform.Transform(command.PositionB);
                var tl = transform.Transform(new Vector2(command.PositionA.X, command.PositionB.Y));
                var vIdx = quadIndex * 4;
                BatchVertexData[vIdx + 0] = new Vertex2D(bl, sr.BottomLeft, arrayIndex);
                BatchVertexData[vIdx + 1] = new Vertex2D(br, sr.BottomRight, arrayIndex);
                BatchVertexData[vIdx + 2] = new Vertex2D(tl, sr.TopLeft, arrayIndex);
                BatchVertexData[vIdx + 3] = new Vertex2D(tr, sr.TopRight, arrayIndex);
                var nIdx = quadIndex * 5;
                var tIdx = (ushort) (quadIndex * 4);
                BatchIndexData[nIdx + 0] = tIdx;
                BatchIndexData[nIdx + 1] = (ushort) (tIdx + 1);
                BatchIndexData[nIdx + 2] = (ushort) (tIdx + 2);
                BatchIndexData[nIdx + 3] = (ushort) (tIdx + 3);
                BatchIndexData[nIdx + 4] = ushort.MaxValue;
                quadIndex += 1;
                if (quadIndex >= MaxBatchQuads)
                {
                    throw new NotImplementedException("Can't batch things this big yet sorry.");
                }
            }

            GL.BindVertexArray(BatchVAO.Handle);

            BatchVBO.Use();
            BatchEBO.Use();
            var vertexData = new Span<Vertex2D>(BatchVertexData, 0, quadIndex * 4);
            var indexData = new Span<ushort>(BatchIndexData, 0, quadIndex * 5);
            if (_reallocateBuffers)
            {
                BatchVBO.Reallocate(vertexData);
                BatchEBO.Reallocate(indexData);
            }
            else
            {
                BatchVBO.WriteSubData(vertexData);
                BatchEBO.WriteSubData(indexData);
            }

            var loaded = _loadedShaders[_currentShader];
            var program = loaded.Program;

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, loadedTexture.OpenGLObject.Handle);

            program.Use();

            // Model matrix becomes identity since it's built into the batch mesh.
            program.SetUniformMaybe(UniModelMatrix, Matrix3.Identity);
            // Reset ModUV to ensure it's identity and doesn't touch anything.
            program.SetUniformMaybe(UniModUV, new Vector4(0, 0, 1, 1));
            // Set modulate.
            DebugTools.Assert(BatchingModulate.HasValue);
            program.SetUniformMaybe(UniModulate, BatchingModulate.Value);
            program.SetUniformMaybe(UniTexturePixelSize, Vector2.One/loadedTexture.Size);
            // Enable primitive restart & do that draw.
            GL.Enable(EnableCap.PrimitiveRestart);
            GL.PrimitiveRestartIndex(ushort.MaxValue);
            GL.DrawElements(PrimitiveType.TriangleStrip, quadIndex * 5, DrawElementsType.UnsignedShort, 0);
            GL.Disable(EnableCap.PrimitiveRestart);

            // Reset batch buffer.
            BatchBuffer.Clear();
            BatchModelMatrices.Clear();
            _batchModelMatricesNeedsUpdate = true;
            BatchingTexture = null;
            BatchingModulate = null;

            if (_logPoolOverdraw && _poolListCreated > 0)
            {
                Logger.DebugS("ogl", "pool overdraw: l: {0}", _poolListCreated);
            }

            _poolListCreated = 0;
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

        private class RenderHandle : IRenderHandle, IDisposable
        {
            private readonly Clyde _manager;

            public readonly List<RenderCommandList> _commandLists = new List<RenderCommandList>();
            public readonly List<(DrawingHandle, RenderCommandList)> _drawingHandles =
                new List<(DrawingHandle, RenderCommandList)>();

            private bool _disposed;

            public RenderHandle(Clyde manager)
            {
                _manager = manager;
            }

            public DrawingHandleWorld CreateHandleWorld()
            {
                _assertNotDisposed();
                if (_manager._currentSpace != CurrentSpace.WorldSpace)
                {
                    throw new InvalidOperationException(
                        "Cannot create world drawing handle while not drawing in world space.");
                }

                var handle = new DrawingHandleWorld(this, _drawingHandles.Count);
                var commandList = _manager._getNewCommandList();
                _drawingHandles.Add((handle, commandList));
                _commandLists.Add(commandList);
                return handle;
            }

            public DrawingHandleScreen CreateHandleScreen()
            {
                _assertNotDisposed();
                if (_manager._currentSpace != CurrentSpace.ScreenSpace)
                {
                    throw new InvalidOperationException(
                        "Cannot create world drawing handle while not drawing in screen space.");
                }

                var handle = new DrawingHandleScreen(this, _drawingHandles.Count);
                var commandList = _manager._getNewCommandList();
                _drawingHandles.Add((handle, commandList));
                _commandLists.Add(commandList);
                return handle;
            }

            public void SetModelTransform(in Matrix3 matrix, int handleId)
            {
                _assertNotDisposed();

                var list = _drawingHandles[handleId].Item2;
                ref var command = ref list.RenderCommands.AllocAdd();
                command.Type = RenderCommandType.Transform;
                command.TransformMatrix = matrix;
            }

            public void DrawTextureRect(Texture texture, Vector2 a, Vector2 b, Color modulate, UIBox2? subRegion,
                int handleId)
            {
                _assertNotDisposed();

                var list = _drawingHandles[handleId].Item2;
                ref var command = ref list.RenderCommands.AllocAdd();
                command.Type = RenderCommandType.Texture;
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

                var openGLTexture = (OpenGLTexture) texture;
                command.TextureId = openGLTexture.OpenGLTextureId;
                command.ArrayIndex = openGLTexture.ArrayIndex;
                command.PositionA = a;
                command.PositionB = b;
                command.Modulate = modulate;
                if (subRegion.HasValue)
                {
                    command.SubRegion = subRegion.Value;
                    command.HasSubRegion = true;
                }
                else
                {
                    command.HasSubRegion = false;
                }
            }

            public void SetScissor(in UIBox2i? scissorBox, int handleId)
            {
                _assertNotDisposed();

                var list = _drawingHandles[handleId].Item2;
                ref var command = ref list.RenderCommands.AllocAdd();

                command.Type = RenderCommandType.Scissor;
                command.EnableScissor = scissorBox.HasValue;
                if (scissorBox.HasValue)
                {
                    command.Scissor = scissorBox.Value;
                }
            }

            public void DrawEntity(IEntity entity, in Vector2 position, int handleId)
            {
                _assertNotDisposed();
                DebugTools.Assert(_manager._currentSpace == CurrentSpace.ScreenSpace);

                var sprite = entity.GetComponent<SpriteComponent>();

                var list = _drawingHandles[handleId].Item2;
                // Switch rendering to world space.
                {
                    ref var commandWorldSpace = ref list.RenderCommands.AllocAdd();
                    commandWorldSpace.Type = RenderCommandType.SwitchSpace;
                    commandWorldSpace.NewSpace = CurrentSpace.WorldSpace;
                }

                {
                    // Change view matrix to put entity where we need.
                    ref var commandViewMatrix = ref list.RenderCommands.AllocAdd();
                    commandViewMatrix.Type = RenderCommandType.ChangeViewMatrix;

                    var viewMatrix = Matrix3.Identity;
                    var ofsX = position.X - _manager._window.Width / 2f;
                    var ofsY = position.Y - _manager._window.Height / 2f;
                    viewMatrix.R0C2 = ofsX / EyeManager.PIXELSPERMETER;
                    viewMatrix.R1C2 = -ofsY / EyeManager.PIXELSPERMETER;
                    commandViewMatrix.ViewMatrix = viewMatrix;
                }

                // Create drawing handle for entity.
                var drawingHandle = new DrawingHandleWorld(this, _drawingHandles.Count);
                _drawingHandles.Add((drawingHandle, list));

                // Draw the entity.
                sprite.OpenGLRender(drawingHandle, false);

                // Reset to screen space
                {
                    ref var commandScreenSpace = ref list.RenderCommands.AllocAdd();
                    commandScreenSpace.Type = RenderCommandType.SwitchSpace;
                    commandScreenSpace.NewSpace = CurrentSpace.ScreenSpace;
                }

                // Reset view matrix.
                {
                    ref var commandScreenSpace = ref list.RenderCommands.AllocAdd();
                    commandScreenSpace.Type = RenderCommandType.ResetViewMatrix;
                }
            }

            public void UseShader(Shaders.Shader shader, int handleId)
            {
                _assertNotDisposed();

                var list = _drawingHandles[handleId].Item2;
                ref var command = ref list.RenderCommands.AllocAdd();

                command.Type = RenderCommandType.UseShader;
                command.ShaderHandle = shader?.ClydeHandle ?? _manager._defaultShader;
                if (command.ShaderHandle == -1)
                {
                    command.ShaderHandle = _manager._defaultShader;
                }
            }

            public void Dispose()
            {
                _disposed = true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void _assertNotDisposed()
            {
                DebugTools.Assert(!_disposed);
            }
        }

        private enum CurrentSpace : byte
        {
            ScreenSpace = 0,
            WorldSpace = 1,
        }

        // Use a tagged union to store all render commands.
        // This significantly improves performance vs doing sum types via inheritance.
        [StructLayout(LayoutKind.Explicit)]
        private struct RenderCommand
        {
            [FieldOffset(0)] public RenderCommandType Type;

            // Texture command fields.
            [FieldOffset(4)] public int TextureId;
            [FieldOffset(8)] public Vector2 PositionA;
            [FieldOffset(16)] public Vector2 PositionB;

            [FieldOffset(24)] public UIBox2 SubRegion;

            // UIBox2 is 4 floats
            [FieldOffset(40)] public bool HasSubRegion;
            [FieldOffset(44)] public Color Modulate;
            [FieldOffset(60)] public int ArrayIndex;

            // Transform command fields.
            [FieldOffset(4)] public Matrix3 TransformMatrix;

            // Scissor command fields.
            [FieldOffset(4)] public bool EnableScissor;
            [FieldOffset(8)] public UIBox2i Scissor;

            // Switch Space command fields.
            [FieldOffset(4)] public CurrentSpace NewSpace;

            // Change view matrix command fields.
            [FieldOffset(4)] public Matrix3 ViewMatrix;

            // UseShader command fields.
            [FieldOffset(4)] public int ShaderHandle;
        }

        private struct BatchCommand
        {
            public Vector2 PositionA;
            public Vector2 PositionB;
            public UIBox2 SubRegion;
            public bool HasSubRegion;
            public int ArrayIndex;
            public int TransformIndex;
        }

        private enum RenderCommandType : int
        {
            Texture,
            Transform,
            Scissor,
            SwitchSpace,
            ChangeViewMatrix,
            ResetViewMatrix,
            UseShader,
        }

        /// <summary>
        ///     A list of rendering commands to execute in order. Pooled.
        /// </summary>
        // ReSharper disable once ClassNeverInstantiated.Local
        private class RenderCommandList
        {
            public RefList<RenderCommand> RenderCommands = new RefList<RenderCommand>();
        }
    }

    /// <summary>
    ///     Handle to generate command lists inside the OpenGL rendering system.
    /// </summary>
    internal interface IRenderHandle
    {
        DrawingHandleWorld CreateHandleWorld();
        DrawingHandleScreen CreateHandleScreen();

        void SetModelTransform(in Matrix3 matrix, int handleId);
        void DrawTextureRect(Texture texture, Vector2 a, Vector2 b, Color modulate, UIBox2? subRegion, int handleId);
        void SetScissor(in UIBox2i? scissorBox, int handleId);
        void DrawEntity(IEntity entity, in Vector2 position, int handleId);
        void UseShader(Shader shader, int handleId);
    }
}
