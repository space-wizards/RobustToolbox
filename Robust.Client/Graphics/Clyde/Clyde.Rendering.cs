using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.Utility;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using TKStencilOp = OpenToolkit.Graphics.OpenGL4.StencilOp;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        // The amount of quads we can render with ushort indices, leaving open 65536 for primitive restart.
        private const ushort MaxBatchQuads = (2 << 13) - 1; // In human terms: (2**16/4)-1 = 16383

        //
        // While rendering of most normal things (read: not grids or lighting),
        // we record stuff into a queue of rendering commands.
        // This both improves performance (I think because of CPU cache?)
        // and allows us to do batching more nicely
        // (make one fat vertex buffer of batching data and send it off quickly)
        //
        // This *basically* divides the renderer into 3 "states":
        // 1. running through high level rendering code and taking simple rendering commands,
        //    probably user code (UI, overlays, maybe more in the future).
        //    Hereafter referred to as "queue"
        // 2. actively going through queued rendering commands created during (queue)
        //    and submitting them to the GL driver.
        //    Hereafter referred to as "submit"
        // 3. running fixed special-purpose rendering code like lighting, FOV and grids.
        //    Also minor switching and transition code between stages of the renderer.
        //    Hereafter referred to as "misc"
        //
        // (queue) always has to be followed by (submit) so that the queued commands actually get executed.
        //
        // Each state obviously has some amount of... state to keep track of,
        // like transformation matrices.
        // This is complicated and I'll my best to keep it straight in the comments.
        //

        // Set to true after lighting is rendered for the current viewport.
        // Disabled again after the world rendering phase on a viewport.
        private bool _lightingReady;

        // The viewport we are currently rendering to.
        private Viewport? _currentViewport;

        // The current render target we're rendering to during queue state.
        // This gets immediately updated when switching render targets during (queue) and (misc),
        // but not during (submit).
        private LoadedRenderTarget _currentRenderTarget;

        // Current model matrix used by the (queue) state.
        // This matrix is applied to most normal geometry coming in.
        // Some is applied while the batch is being created (e.g. simple texture draw calls).
        // For DrawPrimitives OTOH the model matrix is passed along with the render command so is applied in the shader.
        private Matrix3x2 _currentMatrixModel = Matrix3x2.Identity;

        // Buffers and data for the batching system. Written into during (queue) and processed during (submit).
        private readonly Vertex2D[] BatchVertexData = new Vertex2D[MaxBatchQuads * 4];

        // Only write from InitRenderingBatchBuffers!
        private ushort[] BatchIndexData = default!;
        private int BatchVertexIndex;
        private int BatchIndexIndex;

        // Contains information about the currently running batch.
        // So we can flush it if the next draw call is incompatible.
        private BatchMetaData? _batchMetaData;

        // private LoadedTexture? _batchLoadedTexture;
        // Contains the shader instance that's currently being used by the (queue) stage for new commands.
        private ClydeHandle _queuedShader => _queuedShaderInstance.Handle;

        private ClydeShaderInstance _queuedShaderInstance = default!;

        // Current projection & view matrices that are being used ot render.
        // This gets updated to keep track during (queue) and (misc), but not during (submit).
        private Matrix3x2 _currentMatrixProj;
        private Matrix3x2 _currentMatrixView;

        // (queue) and (misc), current state of the scissor test. Null if disabled.
        private UIBox2i? _currentScissorState;

        /// <summary>
        /// Tracks enabled GL capabilities for renderer state.
        /// </summary>
        private GLCaps _glCaps = GLCaps.None;

        private bool IsStencilling
        {
            get => (_glCaps & GLCaps.Stencilling) == GLCaps.Stencilling;
            set
            {
                if (value == IsStencilling)
                    return;

                if (value)
                {
                    _glCaps |= GLCaps.Stencilling;
                    GL.Enable(EnableCap.StencilTest);
                }
                else
                {
                    _glCaps &= ~GLCaps.Stencilling;
                    GL.Disable(EnableCap.StencilTest);
                }

                CheckGlError();
            }
        }

        private bool IsBlending
        {
            get => (_glCaps & GLCaps.Blending) == GLCaps.Blending;
            set
            {
                if (value == IsBlending)
                    return;

                if (value)
                {
                    _glCaps |= GLCaps.Blending;
                    GL.Enable(EnableCap.Blend);
                }
                else
                {
                    _glCaps &= ~GLCaps.Blending;
                    GL.Disable(EnableCap.Blend);
                }

                CheckGlError();
            }
        }

        private bool IsScissoring
        {
            get => _currentScissorState != null;
        }

        private readonly RefList<RenderCommand> _queuedRenderCommands = new RefList<RenderCommand>();

        private void InitRenderingBatchBuffers()
        {
            BatchIndexData = new ushort[MaxBatchQuads * GetQuadBatchIndexCount()];
        }

        /// <summary>
        ///     Updates uniform constants shared to all shaders, such as time and pixel size.
        /// </summary>
        private void _updateUniformConstants(in Vector2i screenSize)
        {
            var constants = new UniformConstants(Vector2.One / screenSize, (float) _gameTiming.RealTime.TotalSeconds);
            UniformConstantsUBO.Reallocate(constants);
        }

        private void CalcScreenMatrices(in Vector2i screenSize, out Matrix3x2 proj, out Matrix3x2 view)
        {
            proj = Matrix3x2.Identity;
            proj.M11 = 2f / screenSize.X;
            proj.M22 = -2f / screenSize.Y;
            proj.M31 = -1;
            proj.M32 = 1;

            if (_currentRenderTarget.FlipY)
            {
                proj.M22 *= -1;
                proj.M32 *= -1;
            }

            view = Matrix3x2.Identity;
        }

        private void CalcWorldMatrices(in Vector2i screenSize, in Vector2 renderScale, IEye eye,
            out Matrix3x2 proj, out Matrix3x2 view)
        {
            eye.GetViewMatrix(out view, renderScale);

            CalcWorldProjMatrix(screenSize, out proj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CalcWorldProjMatrix(in Vector2i screenSize, out Matrix3x2 proj)
        {
            proj = Matrix3x2.Identity;
            proj.M11 = EyeManager.PixelsPerMeter * 2f / screenSize.X;
            proj.M22 = EyeManager.PixelsPerMeter * 2f / screenSize.Y;

            if (_currentRenderTarget.FlipY)
            {
                proj.M22 *= -1;
                proj.M32 *= -1;
            }
        }

        private void SetProjViewBuffer(in Matrix3x2 proj, in Matrix3x2 view)
        {
            // TODO: Fix perf here.
            // This immediately causes a glBufferData() call every time this is changed.
            // Which will be a real performance bottleneck later.
            // Because this is an UBO, these matrices should be batched as well
            // and switched out during command buffer submit by just modifying the bind points.
            var combined = new ProjViewMatrices(proj, view);
            ProjViewUBO.Reallocate(combined);
        }

        private void SetProjViewFull(in Matrix3x2 proj, in Matrix3x2 view)
        {
            _currentMatrixProj = proj;
            _currentMatrixView = view;

            SetProjViewBuffer(proj, view);
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
                        SetScissorImmediate(command.Scissor.EnableScissor, command.Scissor.Scissor);
                        break;

                    case RenderCommandType.ProjViewMatrix:
                        SetProjViewBuffer(command.ProjView.ProjMatrix, command.ProjView.ViewMatrix);
                        break;

                    case RenderCommandType.RenderTarget:
                        var rt = _renderTargets[command.RenderTarget.RenderTarget];
                        BindRenderTargetImmediate(rt);
                        GL.Viewport(0, 0, rt.Size.X, rt.Size.Y);
                        CheckGlError();
                        break;

                    case RenderCommandType.Viewport:
                        ref var vp = ref command.Viewport.Viewport;
                        GL.Viewport(vp.Left, vp.Bottom, vp.Width, vp.Height);
                        CheckGlError();
                        break;

                    case RenderCommandType.Clear:
                        ref var color = ref command.Clear.Color;
                        GL.ClearColor(color.R, color.G, color.B, color.A);
                        CheckGlError();
                        GL.ClearStencil(command.Clear.Stencil);
                        CheckGlError();
                        GL.Clear(command.Clear.Mask);
                        CheckGlError();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void DrawCommandBatch(ref RenderCommandDrawBatch command)
        {
            var loadedTexture = _loadedTextures[command.TextureId];

            BindVertexArray(BatchVAO.Handle);
            CheckGlError();

            var (program, loaded) = ActivateShaderInstance(command.ShaderInstance);
            SetupGlobalUniformsImmediate(program, loadedTexture.IsSrgb);

            GL.ActiveTexture(TextureUnit.Texture0);
            CheckGlError();
            GL.BindTexture(TextureTarget.Texture2D, loadedTexture.OpenGLObject.Handle);
            CheckGlError();

            if (_lightingReady && loaded.HasLighting)
            {
                SetTexture(TextureUnit.Texture1, _currentViewport!.LightRenderTarget.Texture);
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

            program.SetUniformMaybe(UniITexturePixelSize, Vector2.One / loadedTexture.Size);

            SetBlendFunc(loaded.BlendMode);

            var primitiveType = MapPrimitiveType(command.PrimitiveType);
            if (command.Indexed)
            {
                GL.DrawElements(primitiveType, command.Count, DrawElementsType.UnsignedShort,
                    command.StartIndex * sizeof(ushort));
                CheckGlError();
            }
            else
            {
                GL.DrawArrays(primitiveType, command.StartIndex, command.Count);
                CheckGlError();
            }

            ResetBlendFunc();
            GL.BlendEquation(BlendEquationMode.FuncAdd);

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
                BatchPrimitiveType.LineLoop => PrimitiveType.LineLoop,
                BatchPrimitiveType.PointList => PrimitiveType.Points,
                _ => PrimitiveType.Triangles
            };
        }

        private void _drawQuad(Vector2 a, Vector2 b, in Matrix3x2 modelMatrix, GLShaderProgram program)
        {
            DrawQuadWithVao(QuadVAO, a, b, modelMatrix, program);
        }

        private void DrawQuadWithVao(GLHandle vao, Vector2 a, Vector2 b, in Matrix3x2 modelMatrix,
            GLShaderProgram program)
        {
            BindVertexArray(vao.Handle);
            CheckGlError();

            var rectTransform = Matrix3x2.Identity;
            (rectTransform.M11, rectTransform.M22) = b - a;
            (rectTransform.M31, rectTransform.M32) = a;
            rectTransform = rectTransform * modelMatrix;
            program.SetUniformMaybe(UniIModelMatrix, rectTransform);

            _debugStats.LastGLDrawCalls += 1;
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            CheckGlError();
        }

        /// <summary>
        ///     Flushes the render handle, processing and re-pooling all the command lists.
        /// </summary>
        private void FlushRenderQueue()
        {
            FlushBatchQueue();

            // Reset renderer state.
            _currentMatrixModel = Matrix3x2.Identity;
            _queuedShaderInstance = _defaultShader;
            SetScissorFull(null);
        }

        private void FlushBatchQueue()
        {
            // Finish any batches that may have been WiP.
            BreakBatch();

            BindVertexArray(BatchVAO.Handle);
            CheckGlError();

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
        }

        private void SetScissorFull(UIBox2i? state)
        {
            if (state.HasValue)
            {
                SetScissorImmediate(true, state.Value);
            }
            else
            {
                SetScissorImmediate(false, default);
            }

            _currentScissorState = state;
        }

        private void SetScissorImmediate(bool enable, in UIBox2i box)
        {
            if (enable)
            {
                GL.Enable(EnableCap.ScissorTest);
            }
            else
            {
                GL.Disable(EnableCap.ScissorTest);
            }

            if (enable)
            {
                // Don't forget to flip it, these coordinates have bottom left as origin.
                // TODO: Broken when rendering to non-screen render targets.

                if (_currentRenderTarget.FlipY)
                {
                    GL.Scissor(box.Left, box.Top, box.Width, box.Height);
                }
                else
                {
                    GL.Scissor(box.Left, _currentRenderTarget.Size.Y - box.Bottom, box.Width, box.Height);
                }
                CheckGlError();
            }
        }

        // NOTE: sRGB IS IN LINEAR IF FRAMEBUFFER_SRGB IS ACTIVE.
        private void ClearFramebuffer(Color color, int stencil = 0, ClearBufferMask mask = ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit)
        {
            GL.ClearColor(color.ConvertOpenTK());
            CheckGlError();
            GL.ClearStencil(stencil);
            CheckGlError();
            GL.Clear(mask);
            CheckGlError();
        }

        private Color ConvertClearFromSrgb(Color color)
        {
            if (!_hasGLSrgb)
                return color;

            return Color.FromSrgb(color);
        }

        private (GLShaderProgram, LoadedShaderInstance) ActivateShaderInstance(ClydeHandle handle)
        {
            var instance = _shaderInstances[handle];
            var shader = _loadedShaders[instance.ShaderHandle];
            var program = shader.Program;

            program.Use();
            IsStencilling = instance.Stencil.Enabled;

            // Handle stencil parameters.
            if (instance.Stencil.Enabled)
            {
                GL.StencilMask(instance.Stencil.WriteMask);
                CheckGlError();
                GL.StencilFunc(ToGLStencilFunc(instance.Stencil.Func), instance.Stencil.Ref, instance.Stencil.ReadMask);
                CheckGlError();
                GL.StencilOp(TKStencilOp.Keep, TKStencilOp.Keep, ToGLStencilOp(instance.Stencil.Op));
                CheckGlError();
            }

            if (instance.Parameters.Count == 0)
                return (program, instance);

            if (shader.LastInstance == instance && !instance.ParametersDirty)
                return (program, instance);

            shader.LastInstance = instance;
            instance.ParametersDirty = false;

            int textureUnitVal = 0;
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
                    case float[] fArr:
                        program.SetUniform(name, fArr);
                        break;
                    case Vector2 vector2:
                        program.SetUniform(name, vector2);
                        break;
                    case Vector2[] vector2Arr:
                        program.SetUniform(name, vector2Arr);
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
                    case Color[] colorArr:
                        program.SetUniform(name, colorArr);
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
                    case bool[] bArr:
                        program.SetUniform(name, bArr);
                        break;
                    case Matrix3x2 matrix3:
                        program.SetUniform(name, matrix3);
                        break;
                    case Matrix4 matrix4:
                        program.SetUniform(name, matrix4);
                        break;
                    case ClydeTexture clydeTexture:
                        //It's important to start at Texture6 here since DrawCommandBatch uses Texture0 and Texture1 immediately after calling this
                        //function! If passing in textures as uniforms ever stops working it might be since someone made it use all the way up to Texture6 too.
                        //Might change this in the future?
                        TextureUnit cTarget = TextureUnit.Texture6 + textureUnitVal;
                        SetTexture(cTarget, clydeTexture.TextureId);
                        program.SetUniformTexture(name, cTarget);
                        textureUnitVal++;
                        break;
                    default:
                        throw new InvalidOperationException($"Unable to handle shader parameter {name}: {value}");
                }
            }

            return (program, instance);
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

        private void DrawSetModelTransform(in Matrix3x2 matrix)
        {
            _currentMatrixModel = matrix;
        }

        private Matrix3x2 DrawGetModelTransform()
        {
            return _currentMatrixModel;
        }

        private void DrawSetProjViewTransform(in Matrix3x2 proj, in Matrix3x2 view)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.ProjViewMatrix);

            command.ProjView.ProjMatrix = proj;
            command.ProjView.ViewMatrix = view;

            _currentMatrixProj = proj;
            _currentMatrixView = view;
        }

        /// <summary>
        /// Draws a texture quad to the screen.
        /// </summary>
        /// <param name="texture">Texture to draw.</param>
        /// <param name="bl">Bottom left vertex of the quad in object space.</param>
        /// <param name="br">Bottom right vertex of the quad in object space.</param>
        /// <param name="tl">Top left vertex of the quad in object space.</param>
        /// <param name="tr">Top right vertex of the quad in object space.</param>
        /// <param name="modulate">A color to multiply the texture by when shading. Non-linear.</param>
        /// <param name="texCoords">The four corners of the texture coordinates, matching the four vertices.</param>
        private void DrawTexture(ClydeHandle texture, Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr, in Color modulate,
            in Box2 texCoords)
        {
            EnsureBatchSpaceAvailable(4, GetQuadBatchIndexCount());
            EnsureBatchState(texture, true, GetQuadBatchPrimitiveType(), _queuedShader);

            bl = Vector2.Transform(bl, _currentMatrixModel);
            br = Vector2.Transform(br, _currentMatrixModel);
            tr = Vector2.Transform(tr, _currentMatrixModel);
            tl = tr + bl - br;

            // TODO: split batch if necessary.
            var vIdx = BatchVertexIndex;
            BatchVertexData[vIdx + 0] = new Vertex2D(bl, texCoords.BottomLeft, new Vector2(0, 0), modulate);
            BatchVertexData[vIdx + 1] = new Vertex2D(br, texCoords.BottomRight, new Vector2(1, 0), modulate);
            BatchVertexData[vIdx + 2] = new Vertex2D(tr, texCoords.TopRight, new Vector2(1, 1), modulate);
            BatchVertexData[vIdx + 3] = new Vertex2D(tl, texCoords.TopLeft, new Vector2(0, 1), modulate);
            BatchVertexIndex += 4;
            QuadBatchIndexWrite(BatchIndexData, ref BatchIndexIndex, (ushort) vIdx);

            _debugStats.LastClydeDrawCalls += 1;
        }

        private void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, ClydeHandle textureId,
            ReadOnlySpan<ushort> indices, ReadOnlySpan<Vertex2D> vertices)
        {
            FinishBatch();
            _batchMetaData = null;

            EnsureBatchSpaceAvailable(vertices.Length, indices.Length);

            vertices.CopyTo(BatchVertexData.AsSpan(BatchVertexIndex));

            // We are weaving this into the batch buffers for performance (and simplicity).
            // This means all indices have to be offset.
            for (var i = 0; i < indices.Length; i++)
            {
                var o = BatchIndexIndex + i;
                var index = indices[i];
                if (index != PrimitiveRestartIndex) // Don't offset primitive restart.
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
            command.DrawBatch.TextureId = textureId;
            command.DrawBatch.ShaderInstance = _queuedShader;

            command.DrawBatch.Count = indices.Length;
            command.DrawBatch.ModelMatrix = _currentMatrixModel;

            _debugStats.LastBatches += 1;
            _debugStats.LastClydeDrawCalls += 1;
            BatchIndexIndex += indices.Length;
        }

        private void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, ClydeHandle textureId,
            in ReadOnlySpan<Vertex2D> vertices)
        {
            FinishBatch();
            _batchMetaData = null;

            EnsureBatchSpaceAvailable(vertices.Length, 0);

            vertices.CopyTo(BatchVertexData.AsSpan(BatchVertexIndex));

            ref var command = ref AllocRenderCommand(RenderCommandType.DrawBatch);

            command.DrawBatch.Indexed = false;
            command.DrawBatch.StartIndex = BatchVertexIndex;
            command.DrawBatch.PrimitiveType = MapDrawToBatchPrimitiveType(primitiveTopology);
            command.DrawBatch.TextureId = textureId;
            command.DrawBatch.ShaderInstance = _queuedShader;

            command.DrawBatch.Count = vertices.Length;
            command.DrawBatch.ModelMatrix = _currentMatrixModel;

            _debugStats.LastBatches += 1;
            _debugStats.LastClydeDrawCalls += 1;
            BatchVertexIndex += vertices.Length;
        }

        private static BatchPrimitiveType MapDrawToBatchPrimitiveType(DrawPrimitiveTopology topology)
        {
            return topology switch
            {
                DrawPrimitiveTopology.TriangleList => BatchPrimitiveType.TriangleList,
                DrawPrimitiveTopology.TriangleFan => BatchPrimitiveType.TriangleFan,
                DrawPrimitiveTopology.TriangleStrip => BatchPrimitiveType.TriangleStrip,
                DrawPrimitiveTopology.LineList => BatchPrimitiveType.LineList,
                DrawPrimitiveTopology.LineStrip => BatchPrimitiveType.LineStrip,
                DrawPrimitiveTopology.LineLoop => BatchPrimitiveType.LineLoop,
                DrawPrimitiveTopology.PointList => BatchPrimitiveType.PointList,
                _ => BatchPrimitiveType.TriangleList
            };
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            EnsureBatchSpaceAvailable(2, 0);
            EnsureBatchState(_stockTextureWhite.TextureId, false, BatchPrimitiveType.LineList, _queuedShader);

            a = Vector2.Transform(a, _currentMatrixModel);
            b = Vector2.Transform(b, _currentMatrixModel);

            // TODO: split batch if necessary.
            var vIdx = BatchVertexIndex;
            BatchVertexData[vIdx + 0] = new Vertex2D(a, Vector2.Zero, color);
            BatchVertexData[vIdx + 1] = new Vertex2D(b, Vector2.Zero, color);
            BatchVertexIndex += 2;

            _debugStats.LastClydeDrawCalls += 1;
        }

        private void EnsureBatchSpaceAvailable(int vtx, int idx)
        {
            if (BatchVertexIndex + vtx >= BatchVertexData.Length || BatchIndexIndex + idx > BatchIndexData.Length)
            {
                FlushBatchQueue();
            }
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

            _currentScissorState = scissorBox;
        }

        private void DrawUseShader(ClydeShaderInstance instance)
        {
            _queuedShaderInstance = instance;
        }

        private void DrawClear(Color color, int stencil, ClearBufferMask mask)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.Clear);

            command.Clear.Color = color;
            command.Clear.Stencil = stencil;
            command.Clear.Mask = mask;
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

            _currentRenderTarget = _renderTargets[handle];
        }

        /// <summary>
        ///     Ensures that batching metadata matches the current batch.
        ///     If not, the current batch is finished and a new one is started.
        /// </summary>
        private void EnsureBatchState(ClydeHandle textureId, bool indexed,
            BatchPrimitiveType primitiveType, ClydeHandle shaderInstance)
        {
            if (_batchMetaData.HasValue)
            {
                var metaData = _batchMetaData.Value;
                if (metaData.TextureId == textureId &&
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
            _batchMetaData = new BatchMetaData(textureId, indexed, primitiveType,
                indexed ? BatchIndexIndex : BatchVertexIndex, shaderInstance);

            /*
            if (textureId != default)
            {
                _batchLoadedTexture = _loadedTextures[textureId];
            }
            else
            {
                _batchLoadedTexture = null;
            }
            */
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
            command.DrawBatch.TextureId = metaData.TextureId;
            command.DrawBatch.ShaderInstance = metaData.ShaderInstance;

            command.DrawBatch.Count = currentIndex - metaData.StartIndex;
            command.DrawBatch.ModelMatrix = Matrix3x2.Identity;

            _debugStats.LastBatches += 1;
        }

        private static TKStencilOp ToGLStencilOp(StencilOp op)
        {
            return op switch
            {
                StencilOp.Keep => TKStencilOp.Keep,
                StencilOp.Zero => TKStencilOp.Zero,
                StencilOp.Replace => TKStencilOp.Replace,
                StencilOp.IncrementClamp => TKStencilOp.Incr,
                StencilOp.IncrementWrap => TKStencilOp.IncrWrap,
                StencilOp.DecrementClamp => TKStencilOp.Decr,
                StencilOp.DecrementWrap => TKStencilOp.DecrWrap,
                StencilOp.Invert => TKStencilOp.Invert,
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

        private FullStoredRendererState PushRenderStateFull()
        {
            return new FullStoredRendererState(
                _currentMatrixProj,
                _currentMatrixView,
                _currentBoundRenderTarget,
                _currentRenderTarget,
                _queuedShaderInstance,
                _currentScissorState,
                _glCaps);
        }

        private void PopRenderStateFull(in FullStoredRendererState state)
        {
            SetProjViewFull(state.ProjMatrix, state.ViewMatrix);
            BindRenderTargetImmediate(state.BoundRenderTarget);

            _queuedShaderInstance = state.QueuedShaderInstance;
            _currentRenderTarget = state.RenderTarget;
            var (width, height) = state.BoundRenderTarget.Size;
            GL.Viewport(0, 0, width, height);

            IsStencilling = (state.GLCaps & GLCaps.Stencilling) == GLCaps.Stencilling;
            IsBlending = (state.GLCaps & GLCaps.Blending) == GLCaps.Blending;

            SetScissorFull(state.ScissorState);

            GL.ClearStencil(0xFF);
            GL.StencilMask(0xFF);
            GL.Clear(ClearBufferMask.StencilBufferBit);
        }

        private void SetViewportImmediate(Box2i box)
        {
            GL.Viewport(box.Left, box.Bottom, box.Width, box.Height);
            CheckGlError();
        }

        private void ClearRenderState()
        {
            BatchVertexIndex = 0;
            BatchIndexIndex = 0;
            _queuedRenderCommands.Clear();
            _currentViewport = null;
            _lightingReady = false;
            _currentMatrixModel = Matrix3x2.Identity;
            SetScissorFull(null);
            BindRenderTargetFull(_mainWindow!.RenderTarget);
            _batchMetaData = null;
            _queuedShaderInstance = _defaultShader;

            GL.Viewport(0, 0, _mainWindow!.FramebufferSize.X, _mainWindow!.FramebufferSize.Y);
        }

        private void SetBlendFunc(ShaderBlendMode blend)
        {
            switch (blend)
                {
                    case ShaderBlendMode.Add:
                        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.DstAlpha);
                        break;
                    case ShaderBlendMode.Subtract:
                        GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.DstAlpha, BlendingFactorSrc.Zero, BlendingFactorDest.DstAlpha);
                        GL.BlendEquation(BlendEquationMode.FuncReverseSubtract);
                        break;
                    case ShaderBlendMode.Multiply:
                        GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.OneMinusSrcAlpha);
                        break;
                    case ShaderBlendMode.None:
                        GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
                        break;
                    case ShaderBlendMode.Normal:
                        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                        break;
                }
        }

        private void ResetBlendFunc()
        {
            GL.BlendFuncSeparate(
                BlendingFactorSrc.SrcAlpha,
                BlendingFactorDest.OneMinusSrcAlpha,
                BlendingFactorSrc.One,
                BlendingFactorDest.OneMinusSrcAlpha);
        }

        private void FenceRenderTarget(RenderTargetBase rt)
        {
            if (!_hasGLFenceSync || !rt.MakeGLFence)
                return;

            if (rt.LastGLSync != 0)
                GL.DeleteSync(rt.LastGLSync);

            rt.LastGLSync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct RenderCommand
        {
            // Use a tagged union to store all render commands.
            // This significantly improves performance vs doing sum types via inheritance.
            // Also means I don't have to declare a pool for every command type.
            [FieldOffset(0)] public RenderCommandType Type;

            [FieldOffset(4)] public RenderCommandDrawBatch DrawBatch;
            [FieldOffset(4)] public RenderCommandProjViewMatrix ProjView;
            [FieldOffset(4)] public RenderCommandScissor Scissor;
            [FieldOffset(4)] public RenderCommandRenderTarget RenderTarget;
            [FieldOffset(4)] public RenderCommandViewport Viewport;
            [FieldOffset(4)] public RenderCommandClear Clear;
        }

        private struct RenderCommandDrawBatch
        {
            public ClydeHandle TextureId;
            public ClydeHandle ShaderInstance;

            public int StartIndex;
            public int Count;
            public bool Indexed;
            public BatchPrimitiveType PrimitiveType;

            // TODO: this makes the render commands so much more large please remove.
            public Matrix3x2 ModelMatrix;
        }

        private struct RenderCommandProjViewMatrix
        {
            public Matrix3x2 ProjMatrix;
            public Matrix3x2 ViewMatrix;
        }

        private struct RenderCommandScissor
        {
            public bool EnableScissor;
            public UIBox2i Scissor;
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
            public int Stencil;
            public ClearBufferMask Mask;
        }

        private enum RenderCommandType : byte
        {
            DrawBatch,

            ProjViewMatrix,

            //ResetViewMatrix,
            //SwitchSpace,
            Viewport,

            Scissor,
            RenderTarget,

            Clear
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
            public readonly bool Indexed;
            public readonly BatchPrimitiveType PrimitiveType;
            public readonly int StartIndex;
            public readonly ClydeHandle ShaderInstance;

            public BatchMetaData(ClydeHandle textureId, bool indexed, BatchPrimitiveType primitiveType,
                int startIndex, ClydeHandle shaderInstance)
            {
                TextureId = textureId;
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
            LineLoop,
            PointList,
        }

        private readonly struct FullStoredRendererState
        {
            public readonly Matrix3x2 ProjMatrix;
            public readonly Matrix3x2 ViewMatrix;
            public readonly LoadedRenderTarget BoundRenderTarget;
            public readonly LoadedRenderTarget RenderTarget;
            public readonly ClydeShaderInstance QueuedShaderInstance;

            public readonly UIBox2i? ScissorState;

            public readonly GLCaps GLCaps;

            public FullStoredRendererState(
                in Matrix3x2 projMatrix,
                in Matrix3x2 viewMatrix,
                LoadedRenderTarget boundRenderTarget,
                LoadedRenderTarget renderTarget,
                ClydeShaderInstance queuedShaderInstance,
                UIBox2i? scissorState,
                GLCaps glcaps
                )
            {
                ProjMatrix = projMatrix;
                ViewMatrix = viewMatrix;
                BoundRenderTarget = boundRenderTarget;
                RenderTarget = renderTarget;
                QueuedShaderInstance = queuedShaderInstance;

                ScissorState = scissorState;
                GLCaps = glcaps;
            }
        }

        [Flags]
        private enum GLCaps : ushort
        {
            // If you add flags here make sure to update PopRenderState!
            None = 0,

            Blending = 1 << 0,

            Stencilling = 1 << 2,
        }
    }
}
