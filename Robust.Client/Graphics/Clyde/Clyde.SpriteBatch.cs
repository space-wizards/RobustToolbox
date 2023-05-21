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
        private readonly Clyde _clyde;
        private readonly RhiBase _rhi;

        private readonly RhiBuffer _vertexBuffer;
        private readonly Vertex2D[] _vertexBufferData;

        private readonly RhiBuffer _uniformConstantsBuffer;
        private readonly RhiBuffer _uniformPassBuffer;

        private readonly RhiRenderPipeline _pipeline;

        private readonly RhiBindGroupLayout _group0Layout;
        private readonly RhiBindGroupLayout _group1Layout;
        private readonly RhiBindGroupLayout _group2Layout;

        // Active recording state
        private ValueList<RhiBindGroup> _tempBindGroups;
        private RhiCommandEncoder? _commandEncoder;
        private RhiRenderPassEncoder? _passEncoder;

        // Active batch state
        private int _curBatchSize = 0;
        private int _vertexIdx = 0;
        private ClydeHandle _currentTexture;

        // Other state
        private Matrix3x2 _modelTransform;

        public bool IsInRenderPass => _passEncoder != null;

        public SpriteBatch(Clyde clyde, RhiBase rhi)
        {
            _clyde = clyde;
            _rhi = rhi;

            var vertices = 8192;

            _vertexBuffer = _rhi.CreateBuffer(new RhiBufferDescriptor(
                (ulong)(vertices * 32),
                RhiBufferUsageFlags.Vertex | RhiBufferUsageFlags.CopyDst,
                Label: "_vertexBuffer"
            ));

            _vertexBufferData = new Vertex2D[vertices];

            _uniformConstantsBuffer = _rhi.CreateBuffer(new RhiBufferDescriptor(
                sizeof(float),
                RhiBufferUsageFlags.Uniform,
                Label: "_uniformConstantsBuffer"
            ));

            _uniformPassBuffer = _rhi.CreateBuffer(new RhiBufferDescriptor(
                32, RhiBufferUsageFlags.Uniform | RhiBufferUsageFlags.CopyDst, MappedAtCreation: true,
                Label: "_uniformPassBuffer"
            ));

            var mapped = _uniformPassBuffer.GetMappedRange(0, 24);
            mapped.Write(Matrix3x2.Identity, 0);
            _uniformPassBuffer.Unmap();

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

        public void Start()
        {
            Clear();

            _commandEncoder = _rhi.CreateCommandEncoder(new RhiCommandEncoderDescriptor());
        }

        public void BeginPass(Vector2i size, RhiTextureView targetTexture, Color? clearColor = null)
        {
            var projMatrix = default(Matrix3x2);
            projMatrix.M11 = 2f / size.X;
            projMatrix.M22 = -2f / size.Y;
            projMatrix.M31 = -1;
            projMatrix.M32 = 1;

            var viewMatrix = Matrix3x2.Identity;
            var projView = viewMatrix * projMatrix;

            var data = new UniformView
            {
                ProjViewMatrix = ShaderMat3x2F.Transpose(projView),
                ScreenPixelSize = new SVector2(1f / size.X, 1f / size.Y)
            };

            _rhi.Queue.WriteBuffer(_uniformPassBuffer, 0, new ReadOnlySpan<UniformView>(in data));

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
            _passEncoder.SetVertexBuffer(0, _vertexBuffer);

            var constantsGroup = AllocTempBindGroup(new RhiBindGroupDescriptor(
                _group0Layout,
                new[] { new RhiBindGroupEntry(0, new RhiBufferBinding(_uniformConstantsBuffer)) }
            ));

            _passEncoder.SetBindGroup(0, constantsGroup);

            var passGroup = AllocTempBindGroup(new RhiBindGroupDescriptor(
                _group1Layout,
                new[] { new RhiBindGroupEntry(0, new RhiBufferBinding(_uniformPassBuffer)) }
            ));

            _passEncoder.SetBindGroup(1, passGroup);
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

            _rhi.Queue.WriteBuffer<Vertex2D>(_vertexBuffer, 0, _vertexBufferData.AsSpan(0, _vertexIdx));

            var buffer = _commandEncoder!.Finish();
            _rhi.Queue.Submit(buffer);

            _commandEncoder = null;

            Clear();
        }

        public void Draw(ClydeTexture texture, RVector2 position, Color color)
        {
            var textureHandle = texture.TextureId;
            if (textureHandle != _currentTexture)
            {
                var loaded = _clyde._loadedTextures[textureHandle];

                FlushBatch();

                var group = AllocTempBindGroup(new RhiBindGroupDescriptor(
                    _group2Layout,
                    new[]
                    {
                        new RhiBindGroupEntry(0, loaded.DefaultRhiView),
                        new RhiBindGroupEntry(1, loaded.RhiSampler),
                    }
                ));

                _passEncoder!.SetBindGroup(2, group);

                _currentTexture = textureHandle;
            }

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

            _vertexBufferData[_vertexIdx + 0] = new Vertex2D(sBl, new SVector2(0, 1), asColor);
            _vertexBufferData[_vertexIdx + 1] = new Vertex2D(sBr, new SVector2(1, 1), asColor);
            _vertexBufferData[_vertexIdx + 2] = new Vertex2D(sTr, new SVector2(1, 0), asColor);
            _vertexBufferData[_vertexIdx + 3] = new Vertex2D(sTr, new SVector2(1, 0), asColor);
            _vertexBufferData[_vertexIdx + 4] = new Vertex2D(sTl, new SVector2(0, 0), asColor);
            _vertexBufferData[_vertexIdx + 5] = new Vertex2D(sBl, new SVector2(0, 1), asColor);

            _vertexIdx += 6;
            _curBatchSize += 6;
        }

        public void Draw(
            ClydeTexture texture,
            RVector2 bl, RVector2 br, RVector2 tl, RVector2 tr,
            in Color color,
            in Box2 region)
        {
            var textureHandle = texture.TextureId;
            if (textureHandle != _currentTexture)
            {
                var loaded = _clyde._loadedTextures[textureHandle];

                FlushBatch();

                var group = AllocTempBindGroup(new RhiBindGroupDescriptor(
                    _group2Layout,
                    new[]
                    {
                        new RhiBindGroupEntry(0, loaded.DefaultRhiView),
                        new RhiBindGroupEntry(1, loaded.RhiSampler),
                    }
                ));

                _passEncoder!.SetBindGroup(2, group);

                _currentTexture = textureHandle;
            }

            var asColor = Unsafe.As<Color, SVector4>(ref Unsafe.AsRef(color));

            var sBl = SVector2.Transform((SVector2) bl, _modelTransform);
            var sBr = SVector2.Transform((SVector2) br, _modelTransform);
            var sTl = SVector2.Transform((SVector2) tl, _modelTransform);
            var sTr = SVector2.Transform((SVector2) tr, _modelTransform);

            _vertexBufferData[_vertexIdx + 0] = new Vertex2D(sBl, (SVector2) region.BottomLeft, asColor);
            _vertexBufferData[_vertexIdx + 1] = new Vertex2D(sBr, (SVector2) region.BottomRight, asColor);
            _vertexBufferData[_vertexIdx + 2] = new Vertex2D(sTr, (SVector2) region.TopRight, asColor);
            _vertexBufferData[_vertexIdx + 3] = _vertexBufferData[_vertexIdx + 2];
            _vertexBufferData[_vertexIdx + 4] = new Vertex2D(sTl, (SVector2) region.TopLeft, asColor);
            _vertexBufferData[_vertexIdx + 5] = _vertexBufferData[_vertexIdx + 0];

            _vertexIdx += 6;
            _curBatchSize += 6;
        }

        private void FlushBatch()
        {
            if (_curBatchSize == 0)
                return;

            _passEncoder!.Draw((uint)_curBatchSize, 1, (uint)(_vertexIdx - _curBatchSize), 0);
            _curBatchSize = 0;
        }

        public void Clear()
        {
            _vertexIdx = 0;
            _currentTexture = default;
            _curBatchSize = 0;

            foreach (var bindGroup in _tempBindGroups)
            {
                bindGroup.Dispose();
            }

            _tempBindGroups.Clear();
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
    }
}
