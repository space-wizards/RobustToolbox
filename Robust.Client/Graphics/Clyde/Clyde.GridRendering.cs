using System;
using System.Collections.Generic;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly Dictionary<GridId, Dictionary<Vector2i, MapChunkData>> _mapChunkData =
            new();

        private int _verticesPerChunk(MapChunk chunk) => chunk.ChunkSize * chunk.ChunkSize * 4;
        private int _indicesPerChunk(MapChunk chunk) => chunk.ChunkSize * chunk.ChunkSize * GetQuadBatchIndexCount();

        private void _drawGrids(Viewport viewport, Box2Rotated worldBounds, IEye eye)
        {
            var mapId = eye.Position.MapId;
            if (!_mapManager.MapExists(mapId))
            {
                // fall back to nullspace map
                mapId = MapId.Nullspace;
            }

            SetTexture(TextureUnit.Texture0, _tileDefinitionManager.TileTextureAtlas);
            SetTexture(TextureUnit.Texture1, _lightingReady ? viewport.LightRenderTarget.Texture : _stockTextureWhite);

            var (gridProgram, _) = ActivateShaderInstance(_defaultShader.Handle);
            SetupGlobalUniformsImmediate(gridProgram, (ClydeTexture) _tileDefinitionManager.TileTextureAtlas);

            gridProgram.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
            gridProgram.SetUniformTextureMaybe(UniILightTexture, TextureUnit.Texture1);
            gridProgram.SetUniform(UniIModUV, new Vector4(0, 0, 1, 1));
            gridProgram.SetUniform(UniIModulate, Color.White);

            foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                var grid = (IMapGridInternal) mapGrid;

                if (!_mapChunkData.ContainsKey(grid.Index))
                {
                    continue;
                }

                var transform = _entityManager.GetComponent<TransformComponent>(grid.GridEntityId);
                gridProgram.SetUniform(UniIModelMatrix, transform.WorldMatrix);
                grid.GetMapChunks(worldBounds, out var enumerator);

                while (enumerator.MoveNext(out var chunk))
                {
                    if (_isChunkDirty(grid, chunk))
                    {
                        _updateChunkMesh(grid, chunk);
                    }

                    var datum = _mapChunkData[grid.Index][chunk.Indices];

                    if (datum.TileCount == 0)
                    {
                        continue;
                    }

                    BindVertexArray(datum.VAO);
                    CheckGlError();

                    _debugStats.LastGLDrawCalls += 1;
                    GL.DrawElements(GetQuadGLPrimitiveType(), datum.TileCount * GetQuadBatchIndexCount(), DrawElementsType.UnsignedShort, 0);
                    CheckGlError();
                }
            }
        }

        private void _updateChunkMesh(IMapGrid grid, MapChunk chunk)
        {
            var data = _mapChunkData[grid.Index];

            if (!data.TryGetValue(chunk.Indices, out var datum))
            {
                datum = _initChunkBuffers(grid, chunk);
            }

            Span<ushort> indexBuffer = stackalloc ushort[_indicesPerChunk(chunk)];
            Span<Vertex2D> vertexBuffer = stackalloc Vertex2D[_verticesPerChunk(chunk)];

            var i = 0;
            var cSz = grid.ChunkSize;
            var cScaled = chunk.Indices * cSz;
            for (ushort x = 0; x < cSz; x++)
            {
                for (ushort y = 0; y < cSz; y++)
                {
                    var tile = chunk.GetTile(x, y);
                    if (tile.IsEmpty)
                        continue;

                    var regionMaybe = _tileDefinitionManager.TileAtlasRegion(tile);

                    Box2 region;
                    if (regionMaybe == null || regionMaybe.Length <= tile.Variant)
                    {
                        region = _tileDefinitionManager.ErrorTileRegion;
                    }
                    else
                    {
                        region = regionMaybe[tile.Variant];
                    }

                    var gx = x + cScaled.X;
                    var gy = y + cScaled.Y;

                    var vIdx = i * 4;
                    vertexBuffer[vIdx + 0] = new Vertex2D(gx, gy, region.Left, region.Bottom);
                    vertexBuffer[vIdx + 1] = new Vertex2D(gx + 1, gy, region.Right, region.Bottom);
                    vertexBuffer[vIdx + 2] = new Vertex2D(gx + 1, gy + 1, region.Right, region.Top);
                    vertexBuffer[vIdx + 3] = new Vertex2D(gx, gy + 1, region.Left, region.Top);
                    var nIdx = i * GetQuadBatchIndexCount();
                    var tIdx = (ushort)(i * 4);
                    QuadBatchIndexWrite(indexBuffer, ref nIdx, tIdx);
                    i += 1;
                }
            }

            GL.BindVertexArray(datum.VAO);
            CheckGlError();
            datum.EBO.Use();
            datum.VBO.Use();
            datum.EBO.Reallocate(indexBuffer[..(i * GetQuadBatchIndexCount())]);
            datum.VBO.Reallocate(vertexBuffer[..(i * 4)]);
            datum.Dirty = false;
            datum.TileCount = i;
        }

        private MapChunkData _initChunkBuffers(IMapGrid grid, MapChunk chunk)
        {
            var vao = GenVertexArray();
            BindVertexArray(vao);
            CheckGlError();

            var vboSize = _verticesPerChunk(chunk) * Vertex2D.SizeOf;
            var eboSize = _indicesPerChunk(chunk) * sizeof(ushort);

            var vbo = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                vboSize, $"Grid {grid.Index} chunk {chunk.Indices} VBO");
            var ebo = new GLBuffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                eboSize, $"Grid {grid.Index} chunk {chunk.Indices} EBO");

            ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, vao, $"Grid {grid.Index} chunk {chunk.Indices} VAO");
            // Vertex Coords
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
            GL.EnableVertexAttribArray(0);
            // Texture Coords.
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            CheckGlError();

            // Assign VBO and EBO to VAO.
            // OpenGL 3.x is such a good API.
            vbo.Use();
            ebo.Use();

            var datum = new MapChunkData(vao, vbo, ebo)
            {
                Dirty = true
            };

            _mapChunkData[grid.Index].Add(chunk.Indices, datum);
            return datum;
        }

        private bool _isChunkDirty(IMapGrid grid, MapChunk chunk)
        {
            var data = _mapChunkData[grid.Index];
            return !data.TryGetValue(chunk.Indices, out var datum) || datum.Dirty;
        }

        public void _setChunkDirty(IMapGrid grid, Vector2i chunk)
        {
            var data = _mapChunkData[grid.Index];
            if (data.TryGetValue(chunk, out var datum))
            {
                datum.Dirty = true;
            }
            // Don't need to set it if we don't have an entry since lack of an entry is treated as dirty.
        }

        private void _updateOnGridModified(object? sender, GridChangedEventArgs args)
        {
            foreach (var (pos, _) in args.Modified)
            {
                var grid = args.Grid;
                var chunk = grid.GridTileToChunkIndices(pos);
                _setChunkDirty(grid, chunk);
            }
        }

        private void _updateTileMapOnUpdate(object? sender, TileChangedEventArgs args)
        {
            var grid = _mapManager.GetGrid(args.NewTile.GridIndex);
            var chunk = grid.GridTileToChunkIndices(new Vector2i(args.NewTile.X, args.NewTile.Y));
            _setChunkDirty(grid, chunk);
        }

        private void _updateOnGridCreated(MapId mapId, GridId gridId)
        {
            Logger.DebugS("grid", $"Adding {gridId} to grid renderer");
            _mapChunkData.Add(gridId, new Dictionary<Vector2i, MapChunkData>());
        }

        private void _updateOnGridRemoved(MapId mapId, GridId gridId)
        {
            Logger.DebugS("grid", $"Removing {gridId} from grid renderer");

            var data = _mapChunkData[gridId];
            foreach (var chunkDatum in data.Values)
            {
                DeleteVertexArray(chunkDatum.VAO);
                CheckGlError();
                chunkDatum.VBO.Delete();
                chunkDatum.EBO.Delete();
            }

            _mapChunkData.Remove(gridId);
        }

        private sealed class MapChunkData
        {
            public bool Dirty;
            public readonly uint VAO;
            public readonly GLBuffer VBO;
            public readonly GLBuffer EBO;
            public int TileCount;

            public MapChunkData(uint vao, GLBuffer vbo, GLBuffer ebo)
            {
                VAO = vao;
                VBO = vbo;
                EBO = ebo;
            }
        }
    }
}
