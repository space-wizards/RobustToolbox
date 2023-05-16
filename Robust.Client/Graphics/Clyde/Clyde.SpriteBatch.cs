using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Client.Graphics.Clyde.Rhi;
using Robust.Shared.Collections;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Vector2 = Robust.Shared.Maths.Vector2;
using Vector4 = Robust.Shared.Maths.Vector4;

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

        private int _curBatchSize = 0;
        private int _vertexIdx = 0;

        private RhiCommandEncoder? _commandEncoder;
        private RhiRenderPassEncoder? _passEncoder;

        private ClydeHandle _currentTexture;

        private ValueList<RhiBindGroup> _tempBindGroups;

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
                24, RhiBufferUsageFlags.Uniform, MappedAtCreation: true, Label: "_uniformPassBuffer"
            ));

            var mapped = _uniformPassBuffer.GetMappedRange(0, 24);
            mapped.Write(Matrix3x2.Identity, 0);
            _uniformPassBuffer.Unmap();

            var res = IoCManager.Resolve<IResourceManager>();
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
                        new RhiBufferBindingLayout(MinBindingSize: 24)
                    )
                },
                "SpriteBatch bind group 1 (pass)"
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
                                new RhiBlendComponent()
                            )
                        )
                    }
                ),
                "SpriteBatch pipeline"
            ));
        }

        public void Start(RhiTextureView targetTexture)
        {
            _commandEncoder = _rhi.CreateCommandEncoder(new RhiCommandEncoderDescriptor());
            _passEncoder = _commandEncoder.BeginRenderPass(new RhiRenderPassDescriptor(
                new[] { new RhiRenderPassColorAttachment(targetTexture, RhiLoadOp.Clear, RhiStoreOp.Store) }
            ));

            _passEncoder.SetPipeline(_pipeline);
            _passEncoder.SetVertexBuffer(0, _vertexBuffer);

            var constantsGroup = AllocTempBindGroup(new RhiBindGroupDescriptor(
                _group0Layout,
                new []{new RhiBindGroupEntry(0, new RhiBufferBinding(_uniformConstantsBuffer))}
            ));

            _passEncoder.SetBindGroup(0, constantsGroup);

            var passGroup = AllocTempBindGroup(new RhiBindGroupDescriptor(
                _group1Layout,
                new []{new RhiBindGroupEntry(0, new RhiBufferBinding(_uniformPassBuffer))}
            ));

            _passEncoder.SetBindGroup(1, passGroup);
        }

        public void Finish()
        {
            FlushBatch();

            _rhi.Queue.WriteBuffer<Vertex2D>(_vertexBuffer, 0, _vertexBufferData.AsSpan(0, _vertexIdx));

            _passEncoder!.End();
            var buffer = _commandEncoder!.Finish(new RhiCommandBufferDescriptor());
            _rhi.Queue.Submit(buffer);

            foreach (var bindGroup in _tempBindGroups)
            {
                bindGroup.Dispose();
            }

            _tempBindGroups.Clear();
            _vertexIdx = 0;
        }

        public void Draw(ClydeTexture texture, Vector2 position, Color color)
        {
            var textureHandle = texture.TextureId;
            if (textureHandle != _currentTexture)
            {
                var loaded = _clyde._loadedTextures[textureHandle];

                FlushBatch();

                var group = AllocTempBindGroup(new RhiBindGroupDescriptor(
                    _group2Layout,
                    new []
                    {
                        new RhiBindGroupEntry(0, loaded.DefaultRhiView),
                        new RhiBindGroupEntry(1, loaded.RhiSampler),
                    }
                ));

                _passEncoder!.SetBindGroup(2, group);

                _currentTexture = textureHandle;
            }

            var size = 0.1f;

            var bl = position;
            var br = position + (size, 0);
            var tr = position + (size, size);
            var tl = position + (0, size);

            var asColor = Unsafe.As<Color, Vector4>(ref color);

            _vertexBufferData[_vertexIdx + 0] = new Vertex2D(bl, (0, 0), asColor);
            _vertexBufferData[_vertexIdx + 1] = new Vertex2D(br, (1, 0), asColor);
            _vertexBufferData[_vertexIdx + 2] = new Vertex2D(tr, (1, 1), asColor);
            _vertexBufferData[_vertexIdx + 3] = new Vertex2D(tr, (1, 1), asColor);
            _vertexBufferData[_vertexIdx + 4] = new Vertex2D(tl, (0, 1), asColor);
            _vertexBufferData[_vertexIdx + 5] = new Vertex2D(bl, (0, 0), asColor);

            _vertexIdx += 6;
            _curBatchSize += 6;
        }

        private void FlushBatch()
        {
            if (_curBatchSize == 0)
                return;

            _passEncoder!.Draw((uint)_curBatchSize, 1, (uint)(_vertexIdx - _curBatchSize), 0);
            _curBatchSize = 0;
            _currentTexture = default;
        }

        private RhiBindGroup AllocTempBindGroup(in RhiBindGroupDescriptor descriptor)
        {
            var bindGroup = _rhi.CreateBindGroup(descriptor);
            _tempBindGroups.Add(bindGroup);
            return bindGroup;
        }

        private struct Vertex2D
        {
            public Vector2 Position;
            public Vector2 TexCoord;
            public Vector4 Color;

            public Vertex2D(Vector2 position, Vector2 texCoord, Vector4 color)
            {
                Position = position;
                TexCoord = texCoord;
                Color = color;
            }
        }
    }
}
