using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;
using StencilOp = OpenToolkit.Graphics.OpenGL4.StencilOp;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private RenderHandle _renderHandle = default!;

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
        private const ushort MaxBatchQuads = (2 << 13) - 1; // In human terms: (2**16/4)-1 = 16383

        private readonly Vertex2D[] BatchVertexData = new Vertex2D[MaxBatchQuads * 4];

        // Need 5 indices per quad: 4 to draw the quad with triangle strips and another one as primitive restart.
        private readonly ushort[] BatchIndexData = new ushort[MaxBatchQuads * 5];

        private int BatchVertexIndex;
        private int BatchIndexIndex;

        // Contains information about the currently running batch.
        // So we can flush it if the next draw call is incompatible.
        private BatchMetaData? _batchMetaData;
        private LoadedTexture? _batchLoadedTexture;
        private ClydeHandle _queuedShader;

        private ProjViewMatrices _currentMatrices;

        private float _renderTime;

        private bool _isScissoring;
        private bool _isStencilling;

        private readonly RefList<RenderCommand> _queuedRenderCommands = new RefList<RenderCommand>();

        /// <summary>
        ///     Updates uniform constants shared to all shaders, such as time and pixel size.
        /// </summary>
        private void _updateUniformConstants()
        {
            var constants = new UniformConstants(Vector2.One / ScreenSize, _renderTime);
            UniformConstantsUBO.Reallocate(constants);
        }

        private ProjViewMatrices _screenMatrices()
        {
            var viewMatrixScreen = Matrix3.Identity;

            // Screen projection matrix.
            var projMatrixScreen = Matrix3.Identity;
            projMatrixScreen.R0C0 = 2f / ScreenSize.X;
            projMatrixScreen.R1C1 = -2f / ScreenSize.Y;
            projMatrixScreen.R0C2 = -1;
            projMatrixScreen.R1C2 = 1;

            return new ProjViewMatrices(projMatrixScreen, viewMatrixScreen);
        }

        private ProjViewMatrices _worldMatrices()
        {
            var eye = _eyeManager.CurrentEye;

            eye.GetViewMatrix(out var viewMatrixWorld);

            CalcWorldProjectionMatrix(out var projMatrixWorld);

            return new ProjViewMatrices(projMatrixWorld, viewMatrixWorld);
        }

        /// <inheritdoc />
        public void CalcWorldProjectionMatrix(out Matrix3 projMatrix)
        {
            var projMatrixWorld = Matrix3.Identity;
            projMatrixWorld.R0C0 = EyeManager.PixelsPerMeter * 2f / ScreenSize.X;
            projMatrixWorld.R1C1 = EyeManager.PixelsPerMeter * 2f / ScreenSize.Y;
            projMatrix = projMatrixWorld;
        }

        private void _setProjViewMatrices(in ProjViewMatrices matrices)
        {
            _currentMatrices = matrices;
            ProjViewUBO.Reallocate(matrices);
        }

        private void ProcessRenderCommands()
        {
            foreach (ref var command in _queuedRenderCommands)
            {
                switch (command.Type)
                {
                    case RenderCommandType.DrawBatch:
                        DrawCommandBatch(ref command.DrawBatch);
                        break;

                    case RenderCommandType.Scissor:
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
                            GL.Scissor(s.Left, _framebufferSize.Y - s.Bottom, s.Width, s.Height);
                        }
                        else if (oldIsScissoring)
                        {
                            GL.Disable(EnableCap.ScissorTest);
                        }

                        break;

                    case RenderCommandType.ViewMatrix:
                        var matrices = new ProjViewMatrices(_currentMatrices, command.ViewMatrix.Matrix);
                        _setProjViewMatrices(matrices);
                        break;

                    case RenderCommandType.ResetViewMatrix:
                        _setSpace(_currentSpace);
                        break;

                    case RenderCommandType.SwitchSpace:
                        _setSpace(command.SwitchSpace.NewSpace);
                        break;

                    case RenderCommandType.RenderTarget:
                        if (command.RenderTarget.RenderTarget.Value == 0)
                        {
                            // Bind window framebuffer.
                            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                            var (w, h) = ScreenSize;
                            GL.Viewport(0, 0, w, h);
                        }
                        else
                        {
                            var renderTarget = _renderTargets[command.RenderTarget.RenderTarget];

                            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget.ObjectHandle.Handle);
                            var (w, h) = renderTarget.Size;
                            GL.Viewport(0, 0, w, h);
                        }

                        break;

                    case RenderCommandType.Viewport:
                        ref var vp = ref command.Viewport.Viewport;
                        GL.Viewport(vp.Left, vp.Bottom, vp.Width, vp.Height);
                        break;

                    case RenderCommandType.Clear:
                        ref var color = ref command.Clear.Color;
                        GL.ClearColor(color.R, color.G, color.B, color.A);
                        GL.Clear(ClearBufferMask.ColorBufferBit);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void DrawCommandBatch(ref RenderCommandDrawBatch command)
        {
            var loadedTexture = _loadedTextures[command.TextureId];

            GL.BindVertexArray(BatchVAO.Handle);

            var (program, loaded) = ActivateShaderInstance(command.ShaderInstance);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, loadedTexture.OpenGLObject.Handle);

            if (_lightingReady && loaded.HasLighting)
            {
                SetTexture(TextureUnit.Texture1, _lightRenderTarget.Texture);
            }
            else
            {
                SetTexture(TextureUnit.Texture1, _stockTextureWhite);
            }

            program.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
            program.SetUniformTextureMaybe(UniILightTexture, TextureUnit.Texture1);

            // Model matrix becomes identity since it's built into the batch mesh.
            program.SetUniformMaybe(UniIModelMatrix, command.ModelMatrix);
            // Reset ModUV to ensure it's identity and doesn't touch anything.
            program.SetUniformMaybe(UniIModUV, new Vector4(0, 0, 1, 1));

            program.SetUniformMaybe(UniIModulate, command.Modulate);
            program.SetUniformMaybe(UniITexturePixelSize, Vector2.One / loadedTexture.Size);


            var primitiveType = MapPrimitiveType(command.PrimitiveType);
            if (command.Indexed)
            {
                GL.DrawElements(primitiveType, command.Count, DrawElementsType.UnsignedShort,
                    command.StartIndex * sizeof(ushort));
            }
            else
            {
                GL.DrawArrays(primitiveType, command.StartIndex, command.Count);
            }

            _debugStats.LastGLDrawCalls += 1;
        }

        private static PrimitiveType MapPrimitiveType(BatchPrimitiveType type)
        {
            return type switch
            {
                BatchPrimitiveType.TriangleList => PrimitiveType.Triangles,
                BatchPrimitiveType.TriangleFan => PrimitiveType.TriangleFan,
                BatchPrimitiveType.TriangleStrip => PrimitiveType.TriangleStrip,
                BatchPrimitiveType.LineList => PrimitiveType.Lines,
                BatchPrimitiveType.LineStrip => PrimitiveType.LineStrip,
                BatchPrimitiveType.PointList => PrimitiveType.Points,
                _ => PrimitiveType.Triangles
            };
        }

        private void _drawQuad(Vector2 a, Vector2 b, in Matrix3 modelMatrix, GLShaderProgram program)
        {
            GL.BindVertexArray(QuadVAO.Handle);
            var rectTransform = Matrix3.Identity;
            (rectTransform.R0C0, rectTransform.R1C1) = b - a;
            (rectTransform.R0C2, rectTransform.R1C2) = a;
            rectTransform.Multiply(modelMatrix);
            program.SetUniformMaybe(UniIModelMatrix, rectTransform);

            _debugStats.LastGLDrawCalls += 1;
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        /// <summary>
        ///     Flush the render handle, processing and re-pooling all the command lists.
        /// </summary>
        private void FlushRenderQueue()
        {
            // Finish any batches that may have been WiP.
            BreakBatch();

            GL.BindVertexArray(BatchVAO.Handle);

            _debugStats.LargestBatchVertices = Math.Max(BatchVertexIndex, _debugStats.LargestBatchVertices);
            _debugStats.LargestBatchIndices = Math.Max(BatchIndexIndex, _debugStats.LargestBatchIndices);

            if (BatchVertexIndex != 0)
            {
                BatchVBO.Reallocate(new Span<Vertex2D>(BatchVertexData, 0, BatchVertexIndex));
                BatchVertexIndex = 0;

                if (BatchIndexIndex != 0)
                {
                    BatchEBO.Reallocate(new Span<ushort>(BatchIndexData, 0, BatchIndexIndex));
                }

                BatchIndexIndex = 0;
            }

            ProcessRenderCommands();
            _queuedRenderCommands.Clear();

            // Reset renderer state.
            _currentModelMatrix = Matrix3.Identity;
            _queuedShader = _defaultShader.Handle;
            _disableScissor();
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
                    _setProjViewMatrices(_screenMatrices());
                    break;
                case CurrentSpace.WorldSpace:
                    _setProjViewMatrices(_worldMatrices());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newSpace), newSpace, null);
            }
        }

        private void SetSpaceFull(CurrentSpace newSpace)
        {
            _setSpace(newSpace);
        }

        private void ClearFramebuffer(Color color)
        {
            GL.ClearColor(color.ConvertOpenTK());
            GL.ClearStencil(0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit);
        }

        private (GLShaderProgram, LoadedShader) ActivateShaderInstance(ClydeHandle handle)
        {
            var instance = _shaderInstances[handle];
            var shader = _loadedShaders[instance.ShaderHandle];
            var program = shader.Program;

            program.Use();

            // Assign shader parameters to uniform since they may be dirty.
            foreach (var (name, value) in instance.Parameters)
            {
                if (!program.HasUniform(name))
                {
                    // Can happen if the GLSL compiler removes uniforms due to them being unused.
                    // Safe to just ignore them then I'd say.
                    continue;
                }

                switch (value)
                {
                    case float f:
                        program.SetUniform(name, f);
                        break;
                    case Vector2 vector2:
                        program.SetUniform(name, vector2);
                        break;
                    case Vector3 vector3:
                        program.SetUniform(name, vector3);
                        break;
                    case Vector4 vector4:
                        program.SetUniform(name, vector4);
                        break;
                    case Color color:
                        program.SetUniform(name, color);
                        break;
                    case int i:
                        program.SetUniform(name, i);
                        break;
                    case Vector2i vector2I:
                        program.SetUniform(name, vector2I);
                        break;
                    case bool b:
                        program.SetUniform(name, b ? 1 : 0);
                        break;
                    case Matrix3 matrix3:
                        program.SetUniform(name, matrix3);
                        break;
                    case Matrix4 matrix4:
                        program.SetUniform(name, matrix4);
                        break;
                    default:
                        throw new InvalidOperationException($"Unable to handle shader parameter {name}: {value}");
                }
            }

            // Handle stencil parameters.

            if (instance.Stencil.Enabled)
            {
                if (!_isStencilling)
                {
                    GL.Enable(EnableCap.StencilTest);
                    _isStencilling = true;
                }

                GL.StencilMask(instance.Stencil.WriteMask);
                GL.StencilFunc(ToGLStencilFunc(instance.Stencil.Func), instance.Stencil.Ref, instance.Stencil.ReadMask);
                GL.StencilOp(StencilOp.Keep, StencilOp.Keep, ToGLStencilOp(instance.Stencil.Op));
            }
            else if (_isStencilling)
            {
                GL.Disable(EnableCap.StencilTest);
                _isStencilling = false;
            }

            return (program, shader);
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static bool StrictColorEquality(in Color a, in Color b)
        {
            return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
        }

        private ref RenderCommand AllocRenderCommand(RenderCommandType type)
        {
            ref var command = ref _queuedRenderCommands.AllocAdd();
            command.Type = type;
            return ref command;
        }

        private void DrawSetModelTransform(in Matrix3 matrix)
        {
            _currentModelMatrix = matrix;
        }

        private void DrawSetViewTransform(in Matrix3 matrix)
        {
            ref var command = ref AllocRenderCommand(RenderCommandType.ViewMatrix);

            command.ViewMatrix.Matrix = matrix;
        }

        private void DrawTexture(ClydeHandle texture, Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr, in Color modulate, in Box2 sr)
        {
            EnsureBatchState(texture, modulate, true, BatchPrimitiveType.TriangleFan, _queuedShader);

            bl = _currentModelMatrix.Transform(bl);
            br = _currentModelMatrix.Transform(br);
            tr = _currentModelMatrix.Transform(tr);
            tl = _currentModelMatrix.Transform(tl);

            // TODO: split batch if necessary.
            var vIdx = BatchVertexIndex;
            BatchVertexData[vIdx + 0] = new Vertex2D(bl, sr.BottomLeft);
            BatchVertexData[vIdx + 1] = new Vertex2D(br, sr.BottomRight);
            BatchVertexData[vIdx + 2] = new Vertex2D(tr, sr.TopRight);
            BatchVertexData[vIdx + 3] = new Vertex2D(tl, sr.TopLeft);
            BatchVertexIndex += 4;
            var nIdx = BatchIndexIndex;
            var tIdx = (ushort) vIdx;
            BatchIndexData[nIdx + 0] = tIdx;
            BatchIndexData[nIdx + 1] = (ushort) (tIdx + 1);
            BatchIndexData[nIdx + 2] = (ushort) (tIdx + 2);
            BatchIndexData[nIdx + 3] = (ushort) (tIdx + 3);
            BatchIndexData[nIdx + 4] = ushort.MaxValue;
            BatchIndexIndex += 5;

            _debugStats.LastClydeDrawCalls += 1;
        }

        private void DrawTextureWorldSpace(ClydeHandle texture, Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr, Color modulate, UIBox2? subRegion)
        {

        }

        private void DrawTextureCommon(ClydeHandle texture, Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr, Color modulate, UIBox2? subRegion)
        {

        }

        private void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, ClydeHandle textureId,
            ReadOnlySpan<ushort> indices, ReadOnlySpan<Vertex2D> vertices, in Color color)
        {
            FinishBatch();
            _batchMetaData = null;

            vertices.CopyTo(BatchVertexData.AsSpan(BatchVertexIndex));

            // We are weaving this into the batch buffers for performance (and simplicity).
            // This means all indices have to be offset.
            for (var i = 0; i < indices.Length; i++)
            {
                var o = BatchIndexIndex + i;
                var index = indices[i];
                if (index != ushort.MaxValue) // Don't offset primitive restart.
                {
                    index = (ushort) (index + BatchVertexIndex);
                }

                BatchIndexData[o] = index;
            }

            BatchVertexIndex += vertices.Length;

            ref var command = ref AllocRenderCommand(RenderCommandType.DrawBatch);

            command.DrawBatch.Indexed = true;
            command.DrawBatch.StartIndex = BatchIndexIndex;
            command.DrawBatch.PrimitiveType = MapDrawToBatchPrimitiveType(primitiveTopology);
            command.DrawBatch.Modulate = color;
            command.DrawBatch.TextureId = textureId;
            command.DrawBatch.ShaderInstance = _queuedShader;

            command.DrawBatch.Count = indices.Length;
            command.DrawBatch.ModelMatrix = _currentModelMatrix;

            _debugStats.LastBatches += 1;
            _debugStats.LastClydeDrawCalls += 1;
            BatchIndexIndex += indices.Length;
        }

        private void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, ClydeHandle textureId,
            in ReadOnlySpan<Vertex2D> vertices, in Color color)
        {
            FinishBatch();
            _batchMetaData = null;

            vertices.CopyTo(BatchVertexData.AsSpan(BatchVertexIndex));

            ref var command = ref AllocRenderCommand(RenderCommandType.DrawBatch);

            command.DrawBatch.Indexed = false;
            command.DrawBatch.StartIndex = BatchVertexIndex;
            command.DrawBatch.PrimitiveType = MapDrawToBatchPrimitiveType(primitiveTopology);
            command.DrawBatch.Modulate = color;
            command.DrawBatch.TextureId = textureId;
            command.DrawBatch.ShaderInstance = _queuedShader;

            command.DrawBatch.Count = vertices.Length;
            command.DrawBatch.ModelMatrix = _currentModelMatrix;

            _debugStats.LastBatches += 1;
            _debugStats.LastClydeDrawCalls += 1;
            BatchVertexIndex += vertices.Length;
        }

        private BatchPrimitiveType MapDrawToBatchPrimitiveType(DrawPrimitiveTopology topology)
        {
            return topology switch
            {
                DrawPrimitiveTopology.TriangleList => BatchPrimitiveType.TriangleList,
                DrawPrimitiveTopology.TriangleFan => BatchPrimitiveType.TriangleFan,
                DrawPrimitiveTopology.TriangleStrip => BatchPrimitiveType.TriangleStrip,
                DrawPrimitiveTopology.LineList => BatchPrimitiveType.LineList,
                DrawPrimitiveTopology.LineStrip => BatchPrimitiveType.LineStrip,
                DrawPrimitiveTopology.PointList => BatchPrimitiveType.PointList,
                _ => BatchPrimitiveType.TriangleList
            };
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            EnsureBatchState(_stockTextureWhite.TextureId, color, false, BatchPrimitiveType.LineList, _queuedShader);

            a = _currentModelMatrix.Transform(a);
            b = _currentModelMatrix.Transform(b);

            // TODO: split batch if necessary.
            var vIdx = BatchVertexIndex;
            BatchVertexData[vIdx + 0] = new Vertex2D(a, Vector2.Zero);
            BatchVertexData[vIdx + 1] = new Vertex2D(b, Vector2.Zero);
            BatchVertexIndex += 2;

            _debugStats.LastClydeDrawCalls += 1;
        }

        private void DrawSetScissor(UIBox2i? scissorBox)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.Scissor);

            command.Scissor.EnableScissor = scissorBox.HasValue;
            if (scissorBox.HasValue)
            {
                command.Scissor.Scissor = scissorBox.Value;
            }
        }

        private void DrawSwitchSpace(CurrentSpace space)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.SwitchSpace);

            command.SwitchSpace.NewSpace = space;
        }

        private void DrawUseShader(ClydeHandle handle)
        {
            _queuedShader = handle;
        }

        private void DrawClear(Color color)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.Clear);

            command.Clear.Color = color;
        }

        private void DrawViewport(Box2i viewport)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.Viewport);

            command.Viewport.Viewport = viewport;
        }

        private void DrawRenderTarget(ClydeHandle handle)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.RenderTarget);

            command.RenderTarget.RenderTarget = handle;
        }

        /// <summary>
        ///     Ensures that batching metadata matches the current batch.
        ///     If not, the current batch is finished and a new one is started.
        /// </summary>
        private void EnsureBatchState(ClydeHandle textureId, in Color color, bool indexed,
            BatchPrimitiveType primitiveType, ClydeHandle shaderInstance)
        {
            if (_batchMetaData.HasValue)
            {
                var metaData = _batchMetaData.Value;
                if (metaData.TextureId == textureId &&
                    StrictColorEquality(metaData.Color, color) &&
                    indexed == metaData.Indexed &&
                    metaData.PrimitiveType == primitiveType &&
                    metaData.ShaderInstance == shaderInstance)
                {
                    // Data matches, don't have to do anything.
                    return;
                }

                // Data does not match. Finish batch...
                FinishBatch();
            }

            // ... and start another.
            _batchMetaData = new BatchMetaData(textureId, color, indexed, primitiveType,
                indexed ? BatchIndexIndex : BatchVertexIndex, shaderInstance);

            if (textureId != default)
            {
                _batchLoadedTexture = _loadedTextures[textureId];
            }
            else
            {
                _batchLoadedTexture = null;
            }
        }

        private void FinishBatch()
        {
            if (!_batchMetaData.HasValue)
            {
                return;
            }

            var metaData = _batchMetaData.Value;

            var indexed = metaData.Indexed;
            var currentIndex = indexed ? BatchIndexIndex : BatchVertexIndex;

            ref var command = ref AllocRenderCommand(RenderCommandType.DrawBatch);

            command.DrawBatch.Indexed = indexed;
            command.DrawBatch.StartIndex = metaData.StartIndex;
            command.DrawBatch.PrimitiveType = metaData.PrimitiveType;
            command.DrawBatch.Modulate = metaData.Color;
            command.DrawBatch.TextureId = metaData.TextureId;
            command.DrawBatch.ShaderInstance = metaData.ShaderInstance;

            command.DrawBatch.Count = currentIndex - metaData.StartIndex;
            command.DrawBatch.ModelMatrix = Matrix3.Identity;

            _debugStats.LastBatches += 1;
        }

        private static StencilOp ToGLStencilOp(Shaders.StencilOp op)
        {
            return op switch
            {
                Shaders.StencilOp.Keep => StencilOp.Keep,
                Shaders.StencilOp.Zero => StencilOp.Zero,
                Shaders.StencilOp.Replace => StencilOp.Replace,
                Shaders.StencilOp.IncrementClamp => StencilOp.Incr,
                Shaders.StencilOp.IncrementWrap => StencilOp.IncrWrap,
                Shaders.StencilOp.DecrementClamp => StencilOp.Decr,
                Shaders.StencilOp.DecrementWrap => StencilOp.DecrWrap,
                Shaders.StencilOp.Invert => StencilOp.Invert,
                _ => throw new NotSupportedException()
            };
        }

        private static StencilFunction ToGLStencilFunc(StencilFunc op)
        {
            return op switch
            {
                StencilFunc.Never => StencilFunction.Never,
                StencilFunc.Always => StencilFunction.Always,
                StencilFunc.Less => StencilFunction.Less,
                StencilFunc.LessOrEqual => StencilFunction.Lequal,
                StencilFunc.Greater => StencilFunction.Greater,
                StencilFunc.GreaterOrEqual => StencilFunction.Gequal,
                StencilFunc.NotEqual => StencilFunction.Notequal,
                StencilFunc.Equal => StencilFunction.Equal,
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        ///     Renderer state that cannot be changed mid-batch has been modified and a new batch will have to be started.
        /// </summary>
        private void BreakBatch()
        {
            FinishBatch();

            _batchMetaData = null;
        }

        private unsafe void TakeScreenshot(ScreenshotType type)
        {
            if (_queuedScreenshots.Count == 0 || _queuedScreenshots.All(p => p.type != type))
            {
                return;
            }

            var delegates = _queuedScreenshots.Where(p => p.type == type).ToList();

            _queuedScreenshots.RemoveAll(p => p.type == type);

            GL.CreateBuffers(1, out uint pbo);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo);
            GL.BufferData(BufferTarget.PixelPackBuffer, ScreenSize.X * ScreenSize.Y * sizeof(Rgb24), IntPtr.Zero,
                BufferUsageHint.StreamRead);
            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
            GL.ReadPixels(0, 0, ScreenSize.X, ScreenSize.Y, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            var fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

            _transferringScreenshots.Add((pbo, fence, ScreenSize, image => delegates.ForEach(p => p.callback(image))));
        }

        private unsafe void CheckTransferringScreenshots()
        {
            if (_transferringScreenshots.Count == 0)
            {
                return;
            }

            foreach (var screenshot in _transferringScreenshots.ToList())
            {
                var (pbo, fence, (width, height), callback) = screenshot;

                int status;
                GL.GetSync(fence, SyncParameterName.SyncStatus, sizeof(int), null, &status);

                if (status == (int) All.Signaled)
                {
                    GL.BindBuffer(BufferTarget.PixelPackBuffer, pbo);
                    var ptr = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);

                    var packSpan = new ReadOnlySpan<Rgb24>((void*) ptr, width * height);

                    var image = new Image<Rgb24>(width, height);
                    var imageSpan = image.GetPixelSpan();

                    FlipCopy(packSpan, imageSpan, width, height);

                    GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
                    GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
                    GL.DeleteBuffer(pbo);
                    GL.DeleteSync(fence);

                    _transferringScreenshots.Remove(screenshot);

                    // TODO: Don't do unnecessary copy here.
                    callback(image);
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct RenderCommand
        {
            // Use a tagged union to store all render commands.
            // This significantly improves performance vs doing sum types via inheritance.
            // Also means I don't have to declare a pool for every command type.
            [FieldOffset(0)] public RenderCommandType Type;

            [FieldOffset(4)] public RenderCommandDrawBatch DrawBatch;
            [FieldOffset(4)] public RenderCommandViewMatrix ViewMatrix;
            [FieldOffset(4)] public RenderCommandScissor Scissor;
            [FieldOffset(4)] public RenderCommandSwitchSpace SwitchSpace;
            [FieldOffset(4)] public RenderCommandRenderTarget RenderTarget;
            [FieldOffset(4)] public RenderCommandViewport Viewport;
            [FieldOffset(4)] public RenderCommandClear Clear;
        }

        private struct RenderCommandDrawBatch
        {
            public ClydeHandle TextureId;
            public ClydeHandle ShaderInstance;
            public Color Modulate;

            public int StartIndex;
            public int Count;
            public bool Indexed;
            public BatchPrimitiveType PrimitiveType;

            // TODO: this makes the render commands so much more large please remove.
            public Matrix3 ModelMatrix;
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

        private struct RenderCommandSwitchSpace
        {
            public CurrentSpace NewSpace;
        }

        private struct RenderCommandRenderTarget
        {
            public ClydeHandle RenderTarget;
        }

        private struct RenderCommandViewport
        {
            public Box2i Viewport;
        }

        private struct RenderCommandClear
        {
            public Color Color;
        }

        private enum RenderCommandType
        {
            DrawBatch,

            ViewMatrix,
            ResetViewMatrix,
            SwitchSpace,
            Viewport,

            Scissor,
            RenderTarget,

            Clear
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
                _clyde.PopDebugGroupMaybe();
            }
        }

        private readonly struct BatchMetaData
        {
            public readonly ClydeHandle TextureId;
            public readonly Color Color;
            public readonly bool Indexed;
            public readonly BatchPrimitiveType PrimitiveType;
            public readonly int StartIndex;
            public readonly ClydeHandle ShaderInstance;

            public BatchMetaData(ClydeHandle textureId, in Color color, bool indexed, BatchPrimitiveType primitiveType,
                int startIndex, ClydeHandle shaderInstance)
            {
                TextureId = textureId;
                Color = color;
                Indexed = indexed;
                PrimitiveType = primitiveType;
                StartIndex = startIndex;
                ShaderInstance = shaderInstance;
            }
        }

        private enum BatchPrimitiveType : byte
        {
            TriangleList,
            TriangleFan,
            TriangleStrip,
            LineList,
            LineStrip,
            PointList,
        }

        private sealed class SpriteDrawingOrderComparer : IComparer<int>
        {
            private readonly RefList<(SpriteComponent, Matrix3, Angle, float)> _drawList;

            public SpriteDrawingOrderComparer(RefList<(SpriteComponent, Matrix3, Angle, float)> drawList)
            {
                _drawList = drawList;
            }

            public int Compare(int x, int y)
            {
                var a = _drawList[x].Item1;
                var b = _drawList[y].Item1;

                var cmp = (a.DrawDepth).CompareTo(b.DrawDepth);
                if (cmp != 0)
                {
                    return cmp;
                }

                cmp = a.RenderOrder.CompareTo(b.RenderOrder);

                if (cmp != 0)
                {
                    return cmp;
                }

                cmp = _drawList[x].Item4.CompareTo(_drawList[y].Item4);

                if (cmp != 0)
                {
                    return cmp;
                }

                return a.Owner.Uid.CompareTo(b.Owner.Uid);
            }
        }
    }
}
