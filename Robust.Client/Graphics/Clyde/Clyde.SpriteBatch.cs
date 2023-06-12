using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Client.Graphics.Clyde.Rhi;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using RVector2 = Robust.Shared.Maths.Vector2;
using RVector4 = Robust.Shared.Maths.Vector4;
using SVector2 = System.Numerics.Vector2;
using SVector4 = System.Numerics.Vector4;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    // Implementation of a sprite batch on the new RHI.
    // Basically, the old rendering API.

    private sealed class SpriteBatch
    {
        private const int VertexSize = 32;
        private const int UniformPassSize = 32;

        private const uint BindGroup0 = 0;
        private const uint BindGroup1 = 1;
        private const uint BindGroup2 = 2;

        private readonly Clyde _clyde;
        private readonly RhiBase _rhi;

        private readonly GpuExpansionBuffer _vertexBufferPool;

        private readonly RhiBuffer _uniformConstantsBuffer;
        private readonly GpuExpansionBuffer _uniformPassPool;

        private readonly RhiRenderPipeline _pipeline;

        private readonly RhiBindGroupLayout _group0Layout;
        private readonly RhiBindGroupLayout _group1Layout;
        private readonly RhiBindGroupLayout _group2Layout;

        // Active recording state
        private ValueList<RhiBindGroup> _tempBindGroups;
        private RhiCommandEncoder? _commandEncoder;
        private int _vertexStartIndex;
        private RhiBuffer? _currentVertexBuffer;

        private RhiRenderPassEncoder? _passEncoder;
        private Vector2i _currentPassSize;

        // Active batch state
        private int _curBatchSize = 0;

        private DirtyStateFlags _dirtyStateFlags;
        private State<ClydeHandle> _stateTexture;
        private State<(uint x, uint y, uint w, uint h)> _stateScissorRect;
        private bool _scissorSkip;

        // Other state
        private Matrix3x2 _modelTransform;

        public bool IsInRenderPass => _passEncoder != null;

        public SpriteBatch(Clyde clyde, RhiBase rhi)
        {
            _clyde = clyde;
            _rhi = rhi;

            _vertexBufferPool = new GpuExpansionBuffer(
                _rhi,
                8192 * VertexSize,
                RhiBufferUsageFlags.Vertex | RhiBufferUsageFlags.CopyDst,
                label: "_vertexBuffer"
            );

            _uniformConstantsBuffer = _rhi.CreateBuffer(new RhiBufferDescriptor(
                sizeof(float),
                RhiBufferUsageFlags.Uniform,
                Label: "_uniformConstantsBuffer"
            ));

            var uniformPoolSize = MathHelper.CeilingPowerOfTwo(
                UniformPassSize,
                (int)_rhi.DeviceLimits.MinUniformBufferOffsetAlignment
            ) * 20;

            _uniformPassPool = new GpuExpansionBuffer(
                _rhi,
                uniformPoolSize,
                RhiBufferUsageFlags.Uniform | RhiBufferUsageFlags.CopyDst,
                label: "_uniformPassBuffer");

            var res = clyde._resourceCache;
            var shaderSource = res.ContentFileReadAllText("/Shaders/Internal/default-sprite.wgsl");

            using var shader = _rhi.CreateShaderModule(new RhiShaderModuleDescriptor(
                shaderSource,
                "default-sprite.wgsl"
            ));

            _group0Layout = _rhi.CreateBindGroupLayout(new RhiBindGroupLayoutDescriptor(
                new[]
                {
                    new RhiBindGroupLayoutEntry(
                        0,
                        RhiShaderStage.Vertex | RhiShaderStage.Fragment,
                        new RhiBufferBindingLayout(MinBindingSize: 4)
                    )
                },
                "SpriteBatch bind group 0 (constants)"
            ));

            _group1Layout = _rhi.CreateBindGroupLayout(new RhiBindGroupLayoutDescriptor(
                new[]
                {
                    new RhiBindGroupLayoutEntry(
                        0,
                        RhiShaderStage.Vertex | RhiShaderStage.Fragment,
                        new RhiBufferBindingLayout(MinBindingSize: 32)
                    )
                },
                "SpriteBatch bind group 1 (view)"
            ));

            _group2Layout = _rhi.CreateBindGroupLayout(new RhiBindGroupLayoutDescriptor(
                new[]
                {
                    new RhiBindGroupLayoutEntry(
                        0,
                        RhiShaderStage.Fragment,
                        new RhiTextureBindingLayout()
                    ),
                    new RhiBindGroupLayoutEntry(
                        1,
                        RhiShaderStage.Fragment,
                        new RhiSamplerBindingLayout()
                    )
                },
                "SpriteBatch bind group 2 (draw)"
            ));

            var layout = _rhi.CreatePipelineLayout(new RhiPipelineLayoutDescriptor(
                new[] { _group0Layout, _group1Layout, _group2Layout }, "SpriteBatch pipeline layout"
            ));

            _pipeline = _rhi.CreateRenderPipeline(new RhiRenderPipelineDescriptor(
                layout,
                new RhiVertexState(
                    new RhiProgrammableStage(shader, "vs_main"),
                    new[]
                    {
                        new RhiVertexBufferLayout(32, RhiVertexStepMode.Vertex, new[]
                        {
                            new RhiVertexAttribute(RhiVertexFormat.Float32x2, 0, 0),
                            new RhiVertexAttribute(RhiVertexFormat.Float32x2, 8, 1),
                            new RhiVertexAttribute(RhiVertexFormat.Float32x4, 16, 2),
                        })
                    }
                ),
                new RhiPrimitiveState(),
                null,
                new RhiMultisampleState(),
                new RhiFragmentState(
                    new RhiProgrammableStage(shader, "fs_main"),
                    new[]
                    {
                        new RhiColorTargetState(
                            RhiTextureFormat.BGRA8UnormSrgb,
                            new RhiBlendState(
                                new RhiBlendComponent(
                                    RhiBlendOperation.Add,
                                    RhiBlendFactor.SrcAlpha,
                                    RhiBlendFactor.OneMinusSrcAlpha
                                ),
                                new RhiBlendComponent(RhiBlendOperation.Add, RhiBlendFactor.One, RhiBlendFactor.One)
                            )
                        )
                    }
                ),
                "SpriteBatch pipeline"
            ));
        }

        // TODO: this name sucks
        public Vector2i CurrentTargetSize => _currentPassSize;

        public void Start()
        {
            Clear();

            _commandEncoder = _rhi.CreateCommandEncoder(new RhiCommandEncoderDescriptor());
        }

        public void BeginPass(Vector2i size, RhiTextureView targetTexture, Color? clearColor = null)
        {
            ClearPassState();

            _currentPassSize = size;

            var projMatrix = default(Matrix3x2);
            projMatrix.M11 = 2f / size.X;
            projMatrix.M22 = -2f / size.Y;
            projMatrix.M31 = -1;
            projMatrix.M32 = 1;

            var viewMatrix = Matrix3x2.Identity;
            var projView = viewMatrix * projMatrix;

            var uniformPass = _uniformPassPool.AllocateAligned<UniformView>(
                1,
                (int) _rhi.DeviceLimits.MinUniformBufferOffsetAlignment,
                out var uniformPos
            );

            uniformPass[0] = new UniformView
            {
                ProjViewMatrix = ShaderMat3x2F.Transpose(projView),
                ScreenPixelSize = new SVector2(1f / size.X, 1f / size.Y)
            };

            var rhiClearColor = clearColor == null ? new RhiColor(0, 0, 0, 1) : Color.FromSrgb(clearColor.Value);

            _passEncoder = _commandEncoder!.BeginRenderPass(new RhiRenderPassDescriptor(
                new[]
                {
                    new RhiRenderPassColorAttachment(
                        targetTexture,
                        RhiLoadOp.Clear,
                        RhiStoreOp.Store,
                        ClearValue: rhiClearColor)
                }
            ));

            _passEncoder.SetPipeline(_pipeline);

            var constantsGroup = AllocTempBindGroup(new RhiBindGroupDescriptor(
                _group0Layout,
                new[] { new RhiBindGroupEntry(0, new RhiBufferBinding(_uniformConstantsBuffer)) }
            ));

            _passEncoder.SetBindGroup(BindGroup0, constantsGroup);

            var passGroup = AllocTempBindGroup(new RhiBindGroupDescriptor(
                _group1Layout,
                new[]
                {
                    new RhiBindGroupEntry(
                        0,
                        new RhiBufferBinding(uniformPos.Buffer, (ulong) uniformPos.ByteOffset, UniformPassSize)
                    )
                }
            ));

            _passEncoder.SetBindGroup(BindGroup1, passGroup);
        }

        public void EndPass()
        {
            FlushBatch();

            _passEncoder!.End();
            _passEncoder = null;
        }

        public void Finish()
        {
            DebugTools.Assert(_passEncoder == null, "Must end render pass before finishing the sprite batch!");

            _uniformPassPool.Flush();
            _vertexBufferPool.Flush();

            var buffer = _commandEncoder!.Finish();
            _rhi.Queue.Submit(buffer);

            _commandEncoder = null;

            Clear();
        }

        public void Draw(ClydeTexture texture, RVector2 position, Color color)
        {
            var textureHandle = texture.TextureId;

            SetTexture(textureHandle);
            ValidateBatchState();

            var vertices = AllocateVertexSpace(6);

            var width = texture.Width;
            var height = texture.Height;

            var bl = (SVector2)(position);
            var br = (SVector2)(position + (width, 0));
            var tr = (SVector2)(position + (width, height));
            var tl = (SVector2)(position + (0, height));

            var sBl = SVector2.Transform(bl, _modelTransform);
            var sBr = SVector2.Transform(br, _modelTransform);
            var sTl = SVector2.Transform(tl, _modelTransform);
            var sTr = SVector2.Transform(tr, _modelTransform);

            var asColor = Unsafe.As<Color, SVector4>(ref color);

            vertices[0] = new Vertex2D(sBl, new SVector2(0, 1), asColor);
            vertices[1] = new Vertex2D(sBr, new SVector2(1, 1), asColor);
            vertices[2] = new Vertex2D(sTr, new SVector2(1, 0), asColor);
            vertices[3] = new Vertex2D(sTr, new SVector2(1, 0), asColor);
            vertices[4] = new Vertex2D(sTl, new SVector2(0, 0), asColor);
            vertices[5] = new Vertex2D(sBl, new SVector2(0, 1), asColor);

            _vertexStartIndex += 6;
            _curBatchSize += 6;
        }

        public void Draw(
            ClydeTexture texture,
            RVector2 bl, RVector2 br, RVector2 tl, RVector2 tr,
            in Color color,
            in Box2 region)
        {
            var textureHandle = texture.TextureId;

            SetTexture(textureHandle);
            ValidateBatchState();

            var vertices = AllocateVertexSpace(6);

            var asColor = Unsafe.As<Color, SVector4>(ref Unsafe.AsRef(color));

            var sBl = SVector2.Transform((SVector2)bl, _modelTransform);
            var sBr = SVector2.Transform((SVector2)br, _modelTransform);
            var sTl = SVector2.Transform((SVector2)tl, _modelTransform);
            var sTr = SVector2.Transform((SVector2)tr, _modelTransform);

            vertices[0] = new Vertex2D(sBl, (SVector2)region.BottomLeft, asColor);
            vertices[1] = new Vertex2D(sBr, (SVector2)region.BottomRight, asColor);
            vertices[2] = new Vertex2D(sTr, (SVector2)region.TopRight, asColor);
            vertices[3] = vertices[2];
            vertices[4] = new Vertex2D(sTl, (SVector2)region.TopLeft, asColor);
            vertices[5] = vertices[0];

            _vertexStartIndex += 6;
            _curBatchSize += 6;
        }

        private Span<Vertex2D> AllocateVertexSpace(int count)
        {
            var space = _vertexBufferPool.Allocate<Vertex2D>(count, out var position);

            DebugTools.Assert(
                position.ByteOffset % VertexSize == 0,
                "Vertices should be getting allocated in exact multiples of vertex size!"
            );

            if (position.Buffer != _currentVertexBuffer)
            {
                FlushBatch();

                _passEncoder!.SetVertexBuffer(0, position.Buffer);

                _currentVertexBuffer = position.Buffer;
                _vertexStartIndex = position.ByteOffset / VertexSize;
            }

            return space;
        }

        public void SetScissor(int x, int y, int width, int height)
        {
            checked
            {
                SetScissor((uint)x, (uint)y, (uint)width, (uint)height);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetScissor(uint x, uint y, uint width, uint height)
        {
            _stateScissorRect.Set((x, y, width, height), ref _dirtyStateFlags, DirtyStateFlags.ScissorRect);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearScissor()
        {
            SetScissor(0u, 0u, (uint)_currentPassSize.X, (uint)_currentPassSize.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetTexture(ClydeHandle handle)
        {
            _stateTexture.Set(handle, ref _dirtyStateFlags, DirtyStateFlags.Texture);
        }

        private void ValidateBatchState()
        {
            if (_dirtyStateFlags == 0)
                return;

            DirtyStateFlags sureFlags = 0;
            sureFlags |= CheckSureState(ref _stateTexture, DirtyStateFlags.Texture);
            sureFlags |= CheckSureState(ref _stateScissorRect, DirtyStateFlags.ScissorRect);

            if (sureFlags == 0)
                return;

            // Something changed. Save the active batch so we can change other rendering parameters.
            FlushBatch();

            _dirtyStateFlags = 0;

            if ((sureFlags & DirtyStateFlags.Texture) != 0)
            {
                var loaded = _clyde._loadedTextures[_stateTexture.Active];

                FlushBatch();

                var group = AllocTempBindGroup(new RhiBindGroupDescriptor(
                    _group2Layout,
                    new[]
                    {
                        new RhiBindGroupEntry(0, loaded.DefaultRhiView),
                        new RhiBindGroupEntry(1, loaded.RhiSampler),
                    }
                ));

                _passEncoder!.SetBindGroup(BindGroup2, group);
            }

            if ((sureFlags & DirtyStateFlags.ScissorRect) != 0)
            {
                // wgpu doesn't support 0-size scissor rects right now.
                // We ignore draws if the scissor state says 0 width.
                // TODO: Fix this wgpu-side
                if (_stateScissorRect.Active.w != 0 && _stateScissorRect.Active.h != 0)
                {
                    _passEncoder!.SetScissorRect(
                        _stateScissorRect.Active.x,
                        _stateScissorRect.Active.y,
                        _stateScissorRect.Active.w,
                        _stateScissorRect.Active.h
                    );
                    _scissorSkip = false;
                }
                else
                {
                    _scissorSkip = true;
                }

            }

            DirtyStateFlags CheckSureState<T>(ref State<T> state, DirtyStateFlags thisFlag) where T : IEquatable<T>
            {
                // Not really sure this is worth checking again overhead-wise.
                if ((_dirtyStateFlags & thisFlag) == 0)
                    return 0;

                if (state.Active.Equals(state.New))
                    return 0;

                state.Active = state.New;
                return thisFlag;
            }
        }

        private void FlushBatch()
        {
            if (_curBatchSize == 0)
                return;

            if (!_scissorSkip)
            {
                _passEncoder!.Draw(
                    (uint)_curBatchSize,
                    1,
                    (uint)(_vertexStartIndex - _curBatchSize),
                    0
                );
            }

            _curBatchSize = 0;
        }

        public void Clear()
        {
            ClearPassState();

            _vertexStartIndex = 0;
            _currentVertexBuffer = null;

            foreach (var bindGroup in _tempBindGroups)
            {
                bindGroup.Dispose();
            }

            _tempBindGroups.Clear();
        }

        private void ClearPassState()
        {
            _curBatchSize = 0;
            _currentVertexBuffer = null;
            _vertexStartIndex = 0;

            _dirtyStateFlags = 0;
            _stateTexture = default;
            _stateScissorRect = default;

            _modelTransform = Matrix3x2.Identity;
        }

        public void SetModelTransform(in Matrix3x2 matrix)
        {
            _modelTransform = matrix;
        }

        private RhiBindGroup AllocTempBindGroup(in RhiBindGroupDescriptor descriptor)
        {
            var bindGroup = _rhi.CreateBindGroup(descriptor);
            _tempBindGroups.Add(bindGroup);
            return bindGroup;
        }

        private struct Vertex2D
        {
            public SVector2 Position;
            public SVector2 TexCoord;
            public SVector4 Color;

            public Vertex2D(SVector2 position, SVector2 texCoord, SVector4 color)
            {
                Position = position;
                TexCoord = texCoord;
                Color = color;
            }
        }

        private struct UniformView
        {
            public ShaderMat3x2F ProjViewMatrix;
            public SVector2 ScreenPixelSize;
        }

        [Flags]
        private enum DirtyStateFlags : ushort
        {
            // @formatter:off
            None        = 0,
            Texture     = 1 << 0,
            ScissorRect = 1 << 1
            // @formatter:on
        }

        private struct State<T> where T : IEquatable<T>
        {
            public T Active;
            public T New;

            public void Set(T newValue, ref DirtyStateFlags flags, DirtyStateFlags thisFlag)
            {
                if ((flags & thisFlag) != 0)
                {
                    // Property is already dirty, just write it with no further checks.
                    New = newValue;
                    return;
                }

                // Quick case: setting to same value.
                if (Active.Equals(newValue))
                    return;

                // Value modified, mark as dirty.
                New = newValue;
                flags |= thisFlag;
            }
        }
    }
}
