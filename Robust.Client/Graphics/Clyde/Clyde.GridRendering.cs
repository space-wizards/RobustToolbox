using System;
using System.Collections.Generic;
using System.Linq;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly Dictionary<EntityUid, Dictionary<Vector2i, MapChunkData>> _mapChunkData =
            new();

        /// <summary>
        /// To avoid spamming errors we'll just log it once and move on.
        /// </summary>
        private HashSet<Type> _erroredGridOverlays = new();

        private int _verticesPerChunk(MapChunk chunk) => chunk.ChunkSize * chunk.ChunkSize * 4;
        private int _indicesPerChunk(MapChunk chunk) => chunk.ChunkSize * chunk.ChunkSize * GetQuadBatchIndexCount();

        private List<Entity<MapGridComponent>> _grids = new();

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

                var transform = _entityManager.GetComponent<TransformComponent>(mapGrid);
                gridProgram.SetUniform(UniIModelMatrix, transform.WorldMatrix);
                var enumerator = mapSystem.GetMapChunks(mapGrid.Owner, mapGrid.Comp, worldBounds);

                while (enumerator.MoveNext(out var chunk))
                {
                    DebugTools.Assert(chunk.FilledTiles > 0);
                    if (!data.TryGetValue(chunk.Indices, out var datum))
                        data[chunk.Indices] = datum = _initChunkBuffers(mapGrid, chunk);

                    if (datum.Dirty)
                        _updateChunkMesh(mapGrid, chunk, datum);

                    DebugTools.Assert(datum.TileCount > 0);
                    if (datum.TileCount == 0)
                        continue;

                    BindVertexArray(datum.VAO);
                    CheckGlError();

                    _debugStats.LastGLDrawCalls += 1;
                    GL.DrawElements(GetQuadGLPrimitiveType(), datum.TileCount * GetQuadBatchIndexCount(), DrawElementsType.UnsignedShort, 0);
                    CheckGlError();
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
            Span<ushort> indexBuffer = stackalloc ushort[_indicesPerChunk(chunk)];
            Span<Vertex2D> vertexBuffer = stackalloc Vertex2D[_verticesPerChunk(chunk)];

            var i = 0;
            var chunkSize = grid.Comp.ChunkSize;
            var chunkOriginScaled = chunk.Indices * chunkSize;
            var maps = _entityManager.System<SharedMapSystem>();

            for (short x = -1; x <= chunkSize; x++)
            {
                for (short y = -1; y <= chunkSize; y++)
                {
                    var gridX = x + chunkOriginScaled.X;
                    var gridY = y + chunkOriginScaled.Y;
                    var tile = chunk.GetTile((ushort) x, (ushort) y);
                    var tileDef = _tileDefinitionManager[tile.TypeId];

                    // Tile render
                    if (x != -1  && y != -1 &&
                        x != chunkSize && y != chunkSize)
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

                        var vIdx = i * 4;
                        vertexBuffer[vIdx + 0] = new Vertex2D(gridX, gridY, region.Left, region.Bottom, Color.White);
                        vertexBuffer[vIdx + 1] = new Vertex2D(gridX + 1, gridY, region.Right, region.Bottom, Color.White);
                        vertexBuffer[vIdx + 2] = new Vertex2D(gridX + 1, gridY + 1, region.Right, region.Top, Color.White);
                        vertexBuffer[vIdx + 3] = new Vertex2D(gridX, gridY + 1, region.Left, region.Top, Color.White);
                        var nIdx = i * GetQuadBatchIndexCount();
                        var tIdx = (ushort)(i * 4);
                        QuadBatchIndexWrite(indexBuffer, ref nIdx, tIdx);
                        i += 1;
                    }
                    // Edge render
                    for (var nx = -1; nx <= 1; nx++)
                    {
                        for (var ny = -1; ny <= 1; ny++)
                        {
                            var neighborIndices = new Vector2i(gridX + x, gridY + y);
                            maps.TryGetTile(grid.Comp, neighborIndices, out var neighborTile);
                            var neighborDef = _tileDefinitionManager[neighborTile.TypeId];

                            // If it's the same tile then no edge to be drawn.
                            if (tile.TypeId == neighborTile.TypeId || neighborDef.EdgeSprites.Count == 0)
                                continue;

                            // Don't draw if the the neighbor tile edges should draw over us (or if we have the same priority)
                            if (neighborDef.EdgeSpritePriority >= tileDef.EdgeSpritePriority)
                                continue;

                            var direction = new Vector2i(x, y).AsDirection();

                            // No edge tile
                            if (!tileDef.EdgeSprites.TryGetValue(direction, out var edgePath))
                                continue;

                            // TODO: copy from above need to get atlas region for neighbor if possible

                            var texture = _resourceCache.GetResource<TextureResource>(edgePath);
                            var box = Box2.FromDimensions(neighborIndices, grid.Comp.TileSizeVector);

                            var angle = Angle.Zero;

                            // If we ever need one for both cardinals and corners then update this.
                            switch (direction)
                            {
                                // Corner sprites
                                case Direction.SouthEast:
                                    break;
                                case Direction.NorthEast:
                                    angle = new Angle(MathF.PI / 2f);
                                    break;
                                case Direction.NorthWest:
                                    angle = new Angle(MathF.PI);
                                    break;
                                case Direction.SouthWest:
                                    angle = new Angle(MathF.PI * 1.5f);
                                    break;
                                // Edge sprites
                                case Direction.South:
                                    break;
                                case Direction.East:
                                    angle = new Angle(MathF.PI / 2f);
                                    break;
                                case Direction.North:
                                    angle = new Angle(MathF.PI);
                                    break;
                                case Direction.West:
                                    angle = new Angle(MathF.PI * 1.5f);
                                    break;
                            }

                            var vIdx = i * 4;
                            vertexBuffer[vIdx + 0] = new Vertex2D(gridX, gridY, region.Left, region.Bottom, Color.White);
                            vertexBuffer[vIdx + 1] = new Vertex2D(gridX + 1, gridY, region.Right, region.Bottom, Color.White);
                            vertexBuffer[vIdx + 2] = new Vertex2D(gridX + 1, gridY + 1, region.Right, region.Top, Color.White);
                            vertexBuffer[vIdx + 3] = new Vertex2D(gridX, gridY + 1, region.Left, region.Top, Color.White);
                            var nIdx = i * GetQuadBatchIndexCount();
                            var tIdx = (ushort)(i * 4);
                            QuadBatchIndexWrite(indexBuffer, ref nIdx, tIdx);
                            i += 1;

                            if (angle == Angle.Zero)
                                args.WorldHandle.DrawTextureRect(texture.Texture, box);
                            else
                                args.WorldHandle.DrawTextureRect(texture.Texture, new Box2Rotated(box, angle, box.Center));
                        }
                    }
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

        private unsafe MapChunkData _initChunkBuffers(Entity<MapGridComponent> grid, MapChunk chunk)
        {
            var vao = GenVertexArray();
            BindVertexArray(vao);
            CheckGlError();

            var vboSize = _verticesPerChunk(chunk) * sizeof(Vertex2D);
            var eboSize = _indicesPerChunk(chunk) * sizeof(ushort);

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

            var datum = new MapChunkData(vao, vbo, ebo)
            {
                Dirty = true
            };

            return datum;
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
