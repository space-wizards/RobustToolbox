using System;
using System.Buffers;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private readonly Dictionary<GridId, Dictionary<MapIndices, MapChunkData>> _mapChunkData =
            new Dictionary<GridId, Dictionary<MapIndices, MapChunkData>>();

        private int _verticesPerChunk(IMapChunk chunk) => chunk.ChunkSize * chunk.ChunkSize * 4;
        private int _indicesPerChunk(IMapChunk chunk) => chunk.ChunkSize * chunk.ChunkSize * 5;

        private void _drawGrids(Box2 worldBounds)
        {
            var mapId = _eyeManager.CurrentMap;
            if (!_mapManager.MapExists(mapId))
            {
                // fall back to the default eye's map
                _eyeManager.CurrentEye = null;
                mapId = _eyeManager.CurrentMap;
            }

            SetTexture(TextureUnit.Texture0, _tileDefinitionManager.TileTextureAtlas);
            SetTexture(TextureUnit.Texture1, _lightingReady ? _lightRenderTarget.Texture : _stockTextureWhite);

            var (gridProgram, _) = ActivateShaderInstance(_defaultShader.Handle);

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

                var model = Matrix3.Identity;
                model.R0C2 = grid.WorldPosition.X;
                model.R1C2 = grid.WorldPosition.Y;
                gridProgram.SetUniform(UniIModelMatrix, model);

                foreach (var (_, chunk) in grid.GetMapChunks())
                {
                    // Calc world bounds for chunk.
                    if (!chunk.CalcWorldBounds().Intersects(worldBounds))
                    {
                        continue;
                    }

                    if (_isChunkDirty(grid, chunk))
                    {
                        _updateChunkMesh(grid, chunk);
                    }

                    var datum = _mapChunkData[grid.Index][chunk.Indices];

                    if (datum.TileCount == 0)
                    {
                        continue;
                    }

                    GL.BindVertexArray(datum.VAO);

                    _debugStats.LastGLDrawCalls += 1;
                    GL.DrawElements(BeginMode.TriangleStrip, datum.TileCount * 5, DrawElementsType.UnsignedShort, 0);
                }
            }
        }

        private void _updateChunkMesh(IMapGrid grid, IMapChunk chunk)
        {
            var data = _mapChunkData[grid.Index];

            if (!data.TryGetValue(chunk.Indices, out var datum))
            {
                datum = _initChunkBuffers(grid, chunk);
            }

            var vertexPool = ArrayPool<Vertex2D>.Shared;
            var indexPool = ArrayPool<ushort>.Shared;

            var vertexBuffer = vertexPool.Rent(_verticesPerChunk(chunk));
            var indexBuffer = indexPool.Rent(_indicesPerChunk(chunk));

            try
            {
                var i = 0;
                foreach (var tile in chunk)
                {
                    var regionMaybe = _tileDefinitionManager.TileAtlasRegion(tile.Tile);
                    if (!regionMaybe.HasValue)
                    {
                        continue;
                    }

                    var region = regionMaybe.Value;

                    var vIdx = i * 4;
                    vertexBuffer[vIdx + 0] = new Vertex2D(tile.X + 1, tile.Y + 1, region.Right, region.Top);
                    vertexBuffer[vIdx + 1] = new Vertex2D(tile.X, tile.Y + 1, region.Left, region.Top);
                    vertexBuffer[vIdx + 2] = new Vertex2D(tile.X + 1, tile.Y, region.Right, region.Bottom);
                    vertexBuffer[vIdx + 3] = new Vertex2D(tile.X, tile.Y, region.Left, region.Bottom);
                    var nIdx = i * 5;
                    var tIdx = (ushort) (i * 4);
                    indexBuffer[nIdx + 0] = tIdx;
                    indexBuffer[nIdx + 1] = (ushort) (tIdx + 1);
                    indexBuffer[nIdx + 2] = (ushort) (tIdx + 2);
                    indexBuffer[nIdx + 3] = (ushort) (tIdx + 3);
                    indexBuffer[nIdx + 4] = ushort.MaxValue;
                    i += 1;
                }

                GL.BindVertexArray(datum.VAO);
                datum.EBO.Use();
                datum.VBO.Use();
                datum.EBO.Reallocate(new Span<ushort>(indexBuffer, 0, i * 5));
                datum.VBO.Reallocate(new Span<Vertex2D>(vertexBuffer, 0, i * 4));
                datum.Dirty = false;
                datum.TileCount = i;
            }
            finally
            {
                vertexPool.Return(vertexBuffer);
                indexPool.Return(indexBuffer);
            }
        }

        private MapChunkData _initChunkBuffers(IMapGrid grid, IMapChunk chunk)
        {
            var vao = (uint)GL.GenVertexArray();
            GL.BindVertexArray(vao);

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

            // Assign VBO and EBO to VAO.
            // OpenGL 3.x is such a good API.
            vbo.Use();
            ebo.Use();

            var datum = new MapChunkData
            {
                Dirty = true,
                EBO = ebo,
                VAO = vao,
                VBO = vbo
            };

            _mapChunkData[grid.Index].Add(chunk.Indices, datum);
            return datum;
        }

        private bool _isChunkDirty(IMapGrid grid, IMapChunk chunk)
        {
            var data = _mapChunkData[grid.Index];
            return !data.TryGetValue(chunk.Indices, out var datum) || datum.Dirty;
        }

        public void _setChunkDirty(IMapGrid grid, MapIndices chunk)
        {
            var data = _mapChunkData[grid.Index];
            if (data.TryGetValue(chunk, out var datum))
            {
                datum.Dirty = true;
            }
            // Don't need to set it if we don't have an entry since lack of an entry is treated as dirty.
        }

        private void _updateOnGridModified(object sender, GridChangedEventArgs args)
        {
            foreach (var (pos, _) in args.Modified)
            {
                var grid = args.Grid;
                var chunk = grid.GridTileToChunkIndices(pos);
                _setChunkDirty(grid, chunk);
            }
        }

        private void _updateTileMapOnUpdate(object sender, TileChangedEventArgs args)
        {
            var grid = _mapManager.GetGrid(args.NewTile.GridIndex);
            var chunk = grid.GridTileToChunkIndices(new MapIndices(args.NewTile.X, args.NewTile.Y));
            _setChunkDirty(grid, chunk);
        }

        private void _updateOnGridCreated(GridId gridId)
        {
            _mapChunkData.Add(gridId, new Dictionary<MapIndices, MapChunkData>());
        }

        private void _updateOnGridRemoved(GridId gridId)
        {
            var data = _mapChunkData[gridId];
            foreach (var chunkDatum in data.Values)
            {
                GL.DeleteVertexArray(chunkDatum.VAO);
                chunkDatum.VBO.Delete();
                chunkDatum.EBO.Delete();
            }

            _mapChunkData.Remove(gridId);
        }

        private class MapChunkData
        {
            public bool Dirty;
            public uint VAO;
            public GLBuffer VBO;
            public GLBuffer EBO;
            public int TileCount;
        }
    }
}
