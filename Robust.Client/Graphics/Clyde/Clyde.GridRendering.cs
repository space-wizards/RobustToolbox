using System;
using System.Collections.Generic;
using System.Linq;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private readonly Dictionary<EntityUid, Dictionary<Vector2i, MapChunkData>> _mapChunkData =
            new();

        /// <summary>
        /// To avoid spamming errors we'll just log it once and move on.
        /// </summary>
        private HashSet<Type> _erroredGridOverlays = new();

        private Vertex2D[]? _chunkMeshBuilderVertexBuffer;
        private ushort[]? _chunkMeshBuilderIndexBuffer;

        private int _verticesPerChunk(MapChunk chunk) => chunk.ChunkSize * chunk.ChunkSize * 4;
        private int _indicesPerChunk(MapChunk chunk) => chunk.ChunkSize * chunk.ChunkSize * GetQuadBatchIndexCount();

        private List<Entity<MapGridComponent>> _grids = new();
        private bool _drawTileEdges;

        private void RenderTileEdgesChanges(bool value)
        {
            _drawTileEdges = value;
            if (!value)
                return;

            // Dirty all Edges
            foreach (var gridData in _mapChunkData.Values)
            {
                foreach (var chunk in gridData.Values)
                {
                    chunk.EdgeDirty = true;
                }
            }
        }

        private void _drawGrids(Viewport viewport, Box2 worldAABB, Box2Rotated worldBounds, IEye eye)
        {
            var mapId = eye.Position.MapId;
            if (!_mapManager.MapExists(mapId))
            {
                // fall back to nullspace map
                mapId = MapId.Nullspace;
            }

            _grids.Clear();
            _mapManager.FindGridsIntersecting(mapId, worldBounds, ref _grids);

            var requiresFlush = true;
            GLShaderProgram gridProgram = default!;
            var gridOverlays = GetOverlaysForSpace(OverlaySpace.WorldSpaceGrids);
            var mapSystem = _entityManager.System<SharedMapSystem>();

            foreach (var mapGrid in _grids)
            {
                if (!_mapChunkData.TryGetValue(mapGrid, out var data))
                {
                    continue;
                }

                if (requiresFlush)
                {
                    SetTexture(TextureUnit.Texture0, _tileDefinitionManager.TileTextureAtlas);
                    SetTexture(TextureUnit.Texture1, _lightingReady ? viewport.LightRenderTarget.Texture : _stockTextureWhite);
                    gridProgram = ActivateShaderInstance(_defaultShader.Handle).Item1;
                    SetupGlobalUniformsImmediate(gridProgram, (ClydeTexture) _tileDefinitionManager.TileTextureAtlas);

                    gridProgram.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
                    gridProgram.SetUniformTextureMaybe(UniILightTexture, TextureUnit.Texture1);
                    gridProgram.SetUniform(UniIModUV, new Vector4(0, 0, 1, 1));
                }

                gridProgram.SetUniform(UniIModelMatrix, _transformSystem.GetWorldMatrix(mapGrid));
                var enumerator = mapSystem.GetMapChunks(mapGrid.Owner, mapGrid.Comp, worldBounds);

                // Handle base texture updates.
                while (enumerator.MoveNext(out var chunk))
                {
                    DebugTools.Assert(chunk.FilledTiles > 0);
                    var datum = EnsureChunkInitialized(data, chunk, mapGrid);

                    if (!datum.Dirty)
                        continue;

                    _updateChunkMesh(mapGrid, chunk, datum);

                    if (!_drawTileEdges)
                        continue;

                    // Dirty edge tiles for next step.
                    datum.EdgeDirty = true;

                    for (var x = -1; x <= 1; x++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            var neighbor = chunk.Indices + new Vector2i(x, y);

                            if (!mapGrid.Comp.Chunks.TryGetValue(neighbor, out var neighborChunk))
                                continue;

                            var neighborDatum = EnsureChunkInitialized(data, neighborChunk, mapGrid);
                            neighborDatum.EdgeDirty = true;
                        }
                    }
                }

                // Handle edge sprites.
                if (_drawTileEdges)
                {
                    enumerator = mapSystem.GetMapChunks(mapGrid.Owner, mapGrid.Comp, worldBounds);
                    while (enumerator.MoveNext(out var chunk))
                    {
                        var datum = data[chunk.Indices];
                        if (datum.EdgeDirty)
                            _updateChunkEdges(mapGrid, chunk, datum);
                    }
                }

                enumerator = mapSystem.GetMapChunks(mapGrid.Owner, mapGrid.Comp, worldBounds);

                // Draw chunks
                while (enumerator.MoveNext(out var chunk))
                {
                    var datum = data[chunk.Indices];
                    DebugTools.Assert(datum.TileCount > 0);
                    if (datum.TileCount > 0)
                    {
                        BindVertexArray(datum.VAO);
                        CheckGlError();

                        _debugStats.LastGLDrawCalls += 1;
                        GL.DrawElements(GetQuadGLPrimitiveType(), datum.TileCount * GetQuadBatchIndexCount(), DrawElementsType.UnsignedShort, 0);
                        CheckGlError();
                    }

                    if (_drawTileEdges && datum.EdgeCount > 0)
                    {
                        BindVertexArray(datum.EdgeVAO);
                        CheckGlError();

                        _debugStats.LastGLDrawCalls += 1;
                        GL.DrawElements(GetQuadGLPrimitiveType(), datum.EdgeCount * GetQuadBatchIndexCount(), DrawElementsType.UnsignedShort, 0);
                        CheckGlError();
                    }
                }

                requiresFlush = false;

                foreach (var overlay in gridOverlays)
                {
                    if (overlay is not IGridOverlay iGrid)
                    {
                        if (!_erroredGridOverlays.Add(overlay.GetType()))
                        {
                            _clydeSawmill.Error($"Tried to render grid overlay {overlay.GetType()} that doesn't implement {nameof(IGridOverlay)}");
                        }

                        continue;
                    }

                    iGrid.Grid = mapGrid;
                    iGrid.RequiresFlush = false;
                    RenderSingleWorldOverlay(overlay, viewport, OverlaySpace.WorldSpaceGrids, worldAABB, worldBounds);
                    requiresFlush |= iGrid.RequiresFlush;
                }

                if (requiresFlush)
                {
                    FlushRenderQueue();
                }
            }

            CullEmptyChunks();
        }

        private MapChunkData EnsureChunkInitialized(Dictionary<Vector2i, MapChunkData> data, MapChunk chunk, Entity<MapGridComponent> mapGrid)
        {
            if (!data.TryGetValue(chunk.Indices, out var datum))
            {
                data[chunk.Indices] = datum = new MapChunkData();
                _initChunkBuffers(mapGrid, chunk, datum);
            }

            return datum;
        }

        private void CullEmptyChunks()
        {
            foreach (var (grid, chunks) in _mapChunkData)
            {
                var gridComp = _entityManager.GetComponent<MapGridComponent>(grid);
                foreach (var (index, chunk) in chunks)
                {
                    if (!chunk.Dirty || gridComp.Chunks.ContainsKey(index))
                    {
                        DebugTools.Assert(gridComp.Chunks[index].FilledTiles > 0);
                        continue;
                    }

                    DeleteChunk(chunk);
                    chunks.Remove(index);
                }
            }
        }

        private void _updateChunkMesh(Entity<MapGridComponent> grid, MapChunk chunk, MapChunkData datum)
        {
            Span<ushort> indexBuffer = EnsureSize(ref _chunkMeshBuilderIndexBuffer, _indicesPerChunk(chunk));
            Span<Vertex2D> vertexBuffer = EnsureSize(ref _chunkMeshBuilderVertexBuffer, _verticesPerChunk(chunk));

            var i = 0;
            var chunkSize = grid.Comp.ChunkSize;
            var chunkOriginScaled = chunk.Indices * chunkSize;

            for (ushort x = 0; x < chunkSize; x++)
            {
                for (ushort y = 0; y < chunkSize; y++)
                {
                    var gridX = x + chunkOriginScaled.X;
                    var gridY = y + chunkOriginScaled.Y;
                    var tile = chunk.GetTile(x, y);

                    // Tile render
                    if (x != chunkSize && y != chunkSize)
                    {
                        // ReSharper disable once IntVariableOverflowInUncheckedContext
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

                        WriteTileToBuffers(i, gridX, gridY, vertexBuffer, indexBuffer, region);
                        i += 1;
                    }
                }
            }

            var indexSlice = indexBuffer[..(i * GetQuadBatchIndexCount())];
            var vertSlice = vertexBuffer[..(i * 4)];

            GL.BindVertexArray(datum.VAO);
            CheckGlError();
            datum.EBO.Use();
            datum.VBO.Use();
            datum.EBO.Reallocate(indexSlice);
            datum.VBO.Reallocate(vertSlice);

            datum.TileCount = i;
            datum.Dirty = false;
        }

        private void _updateChunkEdges(Entity<MapGridComponent> grid, MapChunk chunk, MapChunkData datum)
        {
            // Need a buffer that can potentially store all neighbor tiles
            Span<ushort> indexBuffer = EnsureSize(ref _chunkMeshBuilderIndexBuffer, _indicesPerChunk(chunk) * 8);
            Span<Vertex2D> vertexBuffer = EnsureSize(ref _chunkMeshBuilderVertexBuffer, _verticesPerChunk(chunk) * 8);

            var i = 0;
            var chunkSize = grid.Comp.ChunkSize;
            var chunkOriginScaled = chunk.Indices * chunkSize;
            var maps = _entityManager.System<SharedMapSystem>();

            for (ushort x = 0; x < chunkSize; x++)
            {
                for (ushort y = 0; y < chunkSize; y++)
                {
                    var gridX = x + chunkOriginScaled.X;
                    var gridY = y + chunkOriginScaled.Y;
                    var tile = chunk.GetTile(x, y);
                    if (!_tileDefinitionManager.TryGetDefinition(tile.TypeId, out var tileDef))
                        continue;

                    // Edge render
                    for (var nx = -1; nx <= 1; nx++)
                    {
                        for (var ny = -1; ny <= 1; ny++)
                        {
                            if (nx == 0 && ny == 0)
                                continue;

                            var neighborIndices = new Vector2i(gridX + nx, gridY + ny);
                            if (!maps.TryGetTile(grid.Comp, neighborIndices, out var neighborTile))
                                continue;

                            if (!_tileDefinitionManager.TryGetDefinition(neighborTile.TypeId, out var neighborDef))
                                continue;

                            // If it's the same tile then no edge to be drawn.
                            if (tile.TypeId == neighborTile.TypeId || neighborDef.EdgeSprites.Count == 0)
                                continue;

                            // If neighbor is a lower or same priority then us then don't draw on our tile.
                            if (neighborDef.EdgeSpritePriority <= tileDef.EdgeSpritePriority)
                                continue;

                            var direction = new Vector2i(nx, ny).AsDirection().GetOpposite();
                            var regionMaybe = _tileDefinitionManager.TileAtlasRegion(neighborTile.TypeId, direction);

                            if (regionMaybe == null)
                                continue;

                            var region = regionMaybe[0];
                            WriteTileToBuffers(i, gridX, gridY, vertexBuffer, indexBuffer, region);
                            i += 1;
                        }
                    }
                }
            }

            // We don't save the edge buffers back because we might need to re-use it if a neighbor chunk updates.
            var indexSlice = indexBuffer[..(i * GetQuadBatchIndexCount())];
            var vertSlice = vertexBuffer[..(i * 4)];

            GL.BindVertexArray(datum.EdgeVAO);
            CheckGlError();
            datum.EdgeEBO.Use();
            datum.EdgeVBO.Use();
            datum.EdgeEBO.Reallocate(indexSlice);
            datum.EdgeVBO.Reallocate(vertSlice);

            datum.EdgeCount = i;
            datum.EdgeDirty = false;
        }

        private unsafe void _initChunkBuffers(Entity<MapGridComponent> grid, MapChunk chunk, MapChunkData datum)
        {
            var vboSize = _verticesPerChunk(chunk) * sizeof(Vertex2D);
            var eboSize = _indicesPerChunk(chunk) * sizeof(ushort);

            // Base VAO
            var vao = GenVertexArray();
            BindVertexArray(vao);
            CheckGlError();

            var vbo = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                vboSize, $"Grid {grid.Owner} chunk {chunk.Indices} VBO");
            var ebo = new GLBuffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                eboSize, $"Grid {grid.Owner} chunk {chunk.Indices} EBO");

            ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, vao, $"Grid {grid.Owner} chunk {chunk.Indices} VAO");
            SetupVAOLayout();
            CheckGlError();

            // Assign VBO and EBO to VAO.
            // OpenGL 3.x is such a good API.
            vbo.Use();
            ebo.Use();

            datum.EBO = ebo;
            datum.VBO = vbo;
            datum.VAO = vao;

            // EdgeVAO
            var edgeVao = GenVertexArray();
            BindVertexArray(edgeVao);
            CheckGlError();

            var edgeVbo = new GLBuffer(this, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw,
                vboSize * 8, $"Grid {grid.Owner} chunk {chunk.Indices} EdgeVBO");
            var edgeEbo = new GLBuffer(this, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw,
                eboSize * 8, $"Grid {grid.Owner} chunk {chunk.Indices} EdgeEBO");

            ObjectLabelMaybe(ObjectLabelIdentifier.VertexArray, vao, $"Grid {grid.Owner} chunk {chunk.Indices} EdgeVAO");
            SetupVAOLayout();
            CheckGlError();

            edgeVbo.Use();
            edgeEbo.Use();

            datum.EdgeEBO = edgeEbo;
            datum.EdgeVBO = edgeVbo;
            datum.EdgeVAO = edgeVao;
        }

        private void DeleteChunk(MapChunkData data)
        {
            DeleteVertexArray(data.VAO);
            CheckGlError();
            data.VBO.Delete();
            data.EBO.Delete();
        }

        private void _updateTileMapOnUpdate(ref TileChangedEvent args)
        {
            var gridData = _mapChunkData.GetOrNew(args.Entity);
            if (gridData.TryGetValue(args.ChunkIndex, out var data))
                data.Dirty = true;
        }

        private void _updateOnGridCreated(GridStartupEvent ev)
        {
            var gridId = ev.EntityUid;
            _mapChunkData.GetOrNew(gridId);
        }

        private void _updateOnGridRemoved(GridRemovalEvent ev)
        {
            var gridId = ev.EntityUid;

            var data = _mapChunkData[gridId];
            foreach (var chunkDatum in data.Values)
            {
                DeleteChunk(chunkDatum);
            }

            _mapChunkData.Remove(gridId);
        }

        private static T[] EnsureSize<T>(ref T[]? field, int size)
        {
            if (field == null || field.Length < size)
                field = new T[size];

            return field;
        }

        private void WriteTileToBuffers(
            int i,
            int gridX,
            int gridY,
            Span<Vertex2D> vertexBuffer,
            Span<ushort> indexBuffer,
            Box2 region)
        {
            var vIdx = i * 4;
            vertexBuffer[vIdx + 0] = new Vertex2D(gridX, gridY, region.Left, region.Bottom, Color.White);
            vertexBuffer[vIdx + 1] = new Vertex2D(gridX + 1, gridY, region.Right, region.Bottom, Color.White);
            vertexBuffer[vIdx + 2] = new Vertex2D(gridX + 1, gridY + 1, region.Right, region.Top, Color.White);
            vertexBuffer[vIdx + 3] = new Vertex2D(gridX, gridY + 1, region.Left, region.Top, Color.White);
            var nIdx = i * GetQuadBatchIndexCount();
            var tIdx = (ushort)(i * 4);
            QuadBatchIndexWrite(indexBuffer, ref nIdx, tIdx);
        }

        private sealed class MapChunkData
        {
            public bool EdgeDirty = true;
            public bool Dirty = true;

            public uint VAO;
            public GLBuffer VBO = default!;
            public GLBuffer EBO = default!;
            public int TileCount;

            public uint EdgeVAO;
            public GLBuffer EdgeVBO = default!;
            public GLBuffer EdgeEBO = default!;
            public int EdgeCount;

            public MapChunkData()
            {
            }
        }
    }
}
