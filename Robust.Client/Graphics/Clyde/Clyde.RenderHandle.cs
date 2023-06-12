using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.Clyde.Rhi;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Client.Graphics.Clyde
{
    // RenderHandle contains the public/internal API surface to control actual rendering operations in Clyde.

    internal partial class Clyde
    {
        private RenderHandle _renderHandle = default!;

        private sealed class RenderHandle : IRenderHandle
        {
            private readonly Clyde _clyde;
            private readonly IEntityManager _entities;
            private readonly SpriteBatch _spriteBatch;

            public DrawingHandleScreen DrawingHandleScreen { get; }
            public DrawingHandleWorld DrawingHandleWorld { get; }

            public RenderHandle(Clyde clyde, IEntityManager entities, SpriteBatch spriteBatch)
            {
                _clyde = clyde;
                _entities = entities;
                _spriteBatch = spriteBatch;

                var white = _clyde.GetStockTexture(ClydeStockTexture.White);
                DrawingHandleScreen = new DrawingHandleScreenImpl(white, this);
                DrawingHandleWorld = new DrawingHandleWorldImpl(white, this);
            }

            public void SetModelTransform(in Matrix3 matrix)
            {
                matrix.Transpose(out var transposed);
                _spriteBatch.SetModelTransform((Matrix3x2)transposed);
            }

            public void SetProjView(in Matrix3 proj, in Matrix3 view)
            {
                // _clyde.DrawSetProjViewTransform(proj, view);
            }

            /// <summary>
            /// Draws a sprite to the screen. The coordinate system is left handed.
            /// Make sure to set <see cref="DrawSetModelTransform"/>
            /// to set the model matrix if needed.
            /// </summary>
            /// <param name="texture">Texture to draw.</param>
            /// <param name="bl">Bottom left vertex of the quad in object space.</param>
            /// <param name="br">Bottom right vertex of the quad in object space.</param>
            /// <param name="tl">Top left vertex of the quad in object space.</param>
            /// <param name="tr">Top right vertex of the quad in object space.</param>
            /// <param name="modulate">A color to multiply the texture by when shading.</param
            /// <param name="subRegion">The four corners of the texture sub region in px.</param>
            public void DrawTextureScreen(Texture texture, Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr,
                in Color modulate, in UIBox2? subRegion)
            {
                var clydeTexture = ExtractTexture(texture, in subRegion, out var csr);

                var (w, h) = clydeTexture.Size;
                var sr = new Box2(csr.Left / w, (h - csr.Top) / h, csr.Right / w, (h - csr.Bottom) / h);

                _spriteBatch.Draw(clydeTexture, bl, br, tl, tr, modulate, sr);
                // _clyde.DrawTexture(clydeTexture.TextureId, bl, br, tl, tr, in modulate, in sr);
            }

            /// <summary>
            /// Draws a sprite to the world. The coordinate system is right handed.
            /// Make sure to set <see cref="DrawSetModelTransform"/>
            /// to set the model matrix if needed.
            /// </summary>
            /// <param name="texture">Texture to draw.</param>
            /// <param name="bl">Bottom left vertex of the quad in object space.</param>
            /// <param name="br">Bottom right vertex of the quad in object space.</param>
            /// <param name="tl">Top left vertex of the quad in object space.</param>
            /// <param name="tr">Top right vertex of the quad in object space.</param>
            /// <param name="modulate">A color to multiply the texture by when shading.</param>
            /// <param name="subRegion">The four corners of the texture sub region in px.</param>
            public void DrawTextureWorld(Texture texture, Vector2 bl, Vector2 br, Vector2 tl, Vector2 tr,
                Color modulate, in UIBox2? subRegion)
            {
                var clydeTexture = ExtractTexture(texture, in subRegion, out var csr);

                var (w, h) = clydeTexture.Size;
                var sr = new Box2(csr.Left / w, (h - csr.Bottom) / h, csr.Right / w, (h - csr.Top) / h);

                // _clyde.DrawTexture(clydeTexture.TextureId, bl, br, tl, tr, in modulate, in sr);
            }

            /// <summary>
            /// Converts a subRegion (px) into texture coords (0-1) of a given texture (cells of the textureAtlas).
            /// </summary>
            private static ClydeTexture ExtractTexture(Texture texture, in UIBox2? subRegion, out UIBox2 sr)
            {
                if (texture is AtlasTexture atlas)
                {
                    texture = atlas.SourceTexture;
                    if (subRegion.HasValue)
                    {
                        var offset = atlas.SubRegion.TopLeft;
                        sr = new UIBox2(
                            subRegion.Value.TopLeft + offset,
                            subRegion.Value.BottomRight + offset);
                    }
                    else
                    {
                        sr = atlas.SubRegion;
                    }
                }
                else
                {
                    sr = subRegion ?? new UIBox2(0, 0, texture.Width, texture.Height);
                }

                var clydeTexture = (ClydeTexture) texture;
                return clydeTexture;
            }

            public void RenderInRenderTarget(IRenderTarget target, Action a, Color? clearColor)
            {
                // TODO: Save/restore SpriteBatch state

                RhiTextureView targetTexture;
                RhiTextureView? depthView = null;
                Vector2i targetSize;

                var loaded = _clyde.RtToLoaded((RenderTargetBase)target);

                if (loaded.IsWindow)
                {
                    targetTexture = loaded.Window!.CurSwapchainView!;
                    targetSize = loaded.Window!.FramebufferSize;
                }
                else
                {
                    var loadedTex = _clyde._loadedTextures[loaded.TextureHandle];
                    targetTexture = loadedTex.DefaultRhiView;

                    depthView = loaded.DepthSencilTextureView;
                    targetSize = loaded.Size;
                }

                _spriteBatch.BeginPass(targetSize, targetTexture, clearColor);

                a();

                _spriteBatch.EndPass();
            }

            public void SetScissor(UIBox2i? scissorBox)
            {
                if (scissorBox is { } box)
                {
                    var (targetWidth, targetHeight) = _spriteBatch.CurrentTargetSize;

                    _spriteBatch.SetScissor(
                        Math.Max(0, box.Left),
                        Math.Max(0, box.Top),
                        Math.Min(targetWidth, box.Width),
                        Math.Min(targetHeight, box.Height)
                    );
                }
                else
                {
                    _spriteBatch.ClearScissor();
                }
            }

            /// <summary>
            /// Draws an entity.
            /// </summary>
            /// <param name="entity">The entity to draw</param>
            /// <param name="position">The local pixel position where the entity should be drawn.</param>
            /// <param name="scale">Scales the drawn entity</param>
            /// <param name="worldRot">The world rotation to use when drawing the entity.
            /// This impacts the sprites RSI direction. Null will retrieve the entity's actual rotation.
            /// </param>
            /// <param name="eyeRot">The effective "eye" angle.
            /// This will cause the entity to be rotated, and may also affect the RSI directions.
            /// Draws the entity at some given angle.</param>
            /// <param name="overrideDirection">RSI direction override.</param>
            /// <param name="sprite">The entity's sprite component</param>
            /// <param name="xform">The entity's transform component.
            /// Only required if <see cref="overrideDirection"/> is null.</param>
            /// <param name="xformSystem">The transform system</param>
            public void DrawEntity(EntityUid entity,
                Vector2 position,
                Vector2 scale,
                Angle? worldRot,
                Angle eyeRot = default,
                Direction? overrideDirection = null,
                SpriteComponent? sprite = null,
                TransformComponent? xform = null,
                SharedTransformSystem? xformSystem = null)
            {
                /*
                if (_entities.Deleted(entity))
                {
                    throw new ArgumentException("Tried to draw an entity has been deleted.", nameof(entity));
                }

                sprite ??= _entities.GetComponent<SpriteComponent>(entity);

                var oldProj = _clyde._currentMatrixProj;
                var oldView = _clyde._currentMatrixView;
                var oldModel = _clyde._currentMatrixModel;

                var newModel = oldModel;
                position += (oldModel.R0C2, oldModel.R1C2);
                newModel.R0C2 = 0;
                newModel.R1C2 = 0;
                SetModelTransform(newModel);

                // Switch rendering to pseudo-world space.
                {
                    _clyde.CalcWorldProjMatrix(_clyde._currentRenderTarget.Size, out var proj);

                    var ofsX = position.X - _clyde._currentRenderTarget.Size.X / 2f;
                    var ofsY = position.Y - _clyde._currentRenderTarget.Size.Y / 2f;
                    ofsX /= EyeManager.PixelsPerMeter;
                    ofsY /= -EyeManager.PixelsPerMeter;

                    // Maaaaybe this is meant to have a minus sign.
                    var rot = -(float) eyeRot.Theta;

                    var view = Matrix3.CreateTransform(ofsX, ofsY, rot, scale.X, scale.Y);
                    SetProjView(proj, view);
                }

                if (worldRot == null)
                {
                    xformSystem ??= _entities.System<SharedTransformSystem>();
                    var query = _entities.GetEntityQuery<TransformComponent>();
                    xform ??= query.GetComponent(entity);
                    worldRot = xformSystem.GetWorldRotation(xform, query);
                }

                // Draw the entity.
                sprite.Render(
                    DrawingHandleWorld,
                    eyeRot,
                    worldRot.Value,
                    overrideDirection);

                // Reset to screen space
                SetProjView(oldProj, oldView);
                SetModelTransform(oldModel);
            */
            }

            public void DrawLine(Vector2 a, Vector2 b, Color color)
            {
                // _clyde.DrawLine(a, b, color);
            }

            public void UseShader(ShaderInstance? shader)
            {
                if (shader != null && shader.Disposed)
                {
                    throw new ArgumentException("Unable to use disposed shader instance.", nameof(shader));
                }

                var clydeShader = (ClydeShaderInstance?) shader;

                // _clyde.DrawUseShader(clydeShader?.Handle ?? _clyde._defaultShader.Handle);
            }

            public void Viewport(Box2i viewport)
            {
                // _clyde.DrawViewport(viewport);
            }

            public void UseRenderTarget(IRenderTarget? renderTarget)
            {
                /*var target = (RenderTexture?) renderTarget;

                _clyde.DrawRenderTarget(target?.Handle ?? default);*/
            }

            /*
            public void Clear(Color color, int stencil = 0, ClearBufferMask mask = ClearBufferMask.ColorBufferBit)
            {
                _clyde.DrawClear(color, stencil, mask);
            }
            */

            public void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
                ReadOnlySpan<DrawVertexUV2DColor> vertices)
            {
                /*
                if (!(texture is ClydeTexture clydeTexture))
                {
                    throw new ArgumentException("Texture must be a basic texture.");
                }

                var castSpan = MemoryMarshal.Cast<DrawVertexUV2DColor, Vertex2D>(vertices);

                _clyde.DrawPrimitives(primitiveTopology, clydeTexture.TextureId, castSpan);
            */
            }

            public void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
                ReadOnlySpan<ushort> indices,
                ReadOnlySpan<DrawVertexUV2DColor> vertices)
            {
                /*if (!(texture is ClydeTexture clydeTexture))
                {
                    throw new ArgumentException("Texture must be a basic texture.");
                }

                var castSpan = MemoryMarshal.Cast<DrawVertexUV2DColor, Vertex2D>(vertices);

                _clyde.DrawPrimitives(primitiveTopology, clydeTexture.TextureId, indices, castSpan);*/
            }

            // ---- (end) ----

            private sealed class DrawingHandleScreenImpl : DrawingHandleScreen
            {
                private readonly RenderHandle _renderHandle;

                public DrawingHandleScreenImpl(Texture white, RenderHandle renderHandle) : base(white)
                {
                    _renderHandle = renderHandle;
                }

                public override void SetTransform(in Matrix3 matrix)
                {
                    _renderHandle.SetModelTransform(matrix);
                }

                public override void UseShader(ShaderInstance? shader)
                {
                    _renderHandle.UseShader(shader);
                }

                public override void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
                    ReadOnlySpan<DrawVertexUV2DColor> vertices)
                {
                    _renderHandle.DrawPrimitives(primitiveTopology, texture, vertices);
                }

                public override void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
                    ReadOnlySpan<ushort> indices, ReadOnlySpan<DrawVertexUV2DColor> vertices)
                {
                    _renderHandle.DrawPrimitives(primitiveTopology, texture, indices, vertices);
                }

                public override void DrawLine(Vector2 from, Vector2 to, Color color)
                {
                    _renderHandle.DrawLine(@from, to, color * Modulate);
                }

                public override void RenderInRenderTarget(IRenderTarget target, Action a, Color? clearColor)
                {
                    _renderHandle.RenderInRenderTarget(target, a, clearColor);
                }

                public override void DrawRect(UIBox2 rect, Color color, bool filled = true)
                {
                    if (filled)
                    {
                        DrawTextureRect(White, rect, color);
                    }
                    else
                    {
                        DrawLine(rect.TopLeft, rect.TopRight, color);
                        DrawLine(rect.TopRight, rect.BottomRight, color);
                        DrawLine(rect.BottomRight, rect.BottomLeft, color);
                        DrawLine(rect.BottomLeft, rect.TopLeft, color);
                    }
                }

                public override void DrawTextureRectRegion(Texture texture, UIBox2 rect, UIBox2? subRegion = null,
                    Color? modulate = null)
                {
                    var color = (modulate ?? Color.White) * Modulate;
                    _renderHandle.DrawTextureScreen(texture, rect.TopLeft, rect.TopRight,
                        rect.BottomLeft, rect.BottomRight, color, subRegion);
                }

                /// <summary>
                /// Draws an entity.
                /// </summary>
                /// <param name="entity">The entity to draw</param>
                /// <param name="position">The local pixel position where the entity should be drawn.</param>
                /// <param name="scale">Scales the drawn entity</param>
                /// <param name="worldRot">The world rotation to use when drawing the entity.
                /// This impacts the sprites RSI direction. Null will retrieve the entity's actual rotation.
                /// </param>
                /// <param name="eyeRot">The effective "eye" angle.
                /// This will cause the entity to be rotated, and may also affect the RSI directions.
                /// Draws the entity at some given angle.</param>
                /// <param name="overrideDirection">RSI direction override.</param>
                /// <param name="sprite">The entity's sprite component</param>
                /// <param name="xform">The entity's transform component.
                /// Only required if <see cref="overrideDirection"/> is null.</param>
                /// <param name="xformSystem">The transform system</param>
                public override void DrawEntity(EntityUid entity,
                    Vector2 position,
                    Vector2 scale,
                    Angle? worldRot,
                    Angle eyeRot = default,
                    Direction? overrideDirection = null,
                    SpriteComponent? sprite = null,
                    TransformComponent? xform = null,
                    SharedTransformSystem? xformSystem = null)
                {
                    _renderHandle.DrawEntity(entity, position, scale, worldRot, eyeRot, overrideDirection, sprite, xform, xformSystem);
                }
            }

            private sealed class DrawingHandleWorldImpl : DrawingHandleWorld
            {
                private readonly RenderHandle _renderHandle;

                public DrawingHandleWorldImpl(Texture white, RenderHandle renderHandle) : base(white)
                {
                    _renderHandle = renderHandle;
                }

                public override void SetTransform(in Matrix3 matrix)
                {
                    _renderHandle.SetModelTransform(matrix);
                }

                public override void UseShader(ShaderInstance? shader)
                {
                    _renderHandle.UseShader(shader);
                }

                public override void DrawCircle(Vector2 position, float radius, Color color, bool filled = true)
                {
                    int divisions = Math.Max(16,(int)(radius * 16));
                    float arcLength = MathF.PI * 2 / divisions;

                    var colorReal = color * Modulate;

                    if (filled)
                    {
                        // Unfilled (using _renderHandle.DrawLine) does the linear conversion internally.
                        // Filled meanwhile uses DrawPrimitives, so has to do it here.
                        colorReal = Color.FromSrgb(color);
                    }

                    Span<DrawVertexUV2DColor> filledTriangle = stackalloc DrawVertexUV2DColor[3];

                    // Draws a "circle", but its just a polygon with a bunch of sides
                    // this is the GL_LINES version, not GL_LINE_STRIP
                    for (int i = 0; i < divisions; i++)
                    {
                        var startPos = new Vector2(MathF.Cos(arcLength * i) * radius, MathF.Sin(arcLength * i) * radius);
                        var endPos = new Vector2(MathF.Cos(arcLength * (i+1)) * radius, MathF.Sin(arcLength * (i + 1)) * radius);

                        if(!filled)
                            _renderHandle.DrawLine(startPos + position, endPos + position, colorReal);
                        else
                        {
                            filledTriangle[0] = new DrawVertexUV2DColor(startPos + position, colorReal);
                            filledTriangle[1] = new DrawVertexUV2DColor(endPos + position, colorReal);
                            filledTriangle[2] = new DrawVertexUV2DColor(position, colorReal);

                            _renderHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, White, filledTriangle);
                        }
                    }
                }

                public override void DrawLine(Vector2 from, Vector2 to, Color color)
                {
                    _renderHandle.DrawLine(@from, to, color * Modulate);
                }

                public override void RenderInRenderTarget(IRenderTarget target, Action a, Color? clearColor)
                {
                    _renderHandle.RenderInRenderTarget(target, a, clearColor);
                }

                public override void DrawRect(Box2 rect, Color color, bool filled = true)
                {
                    if (filled)
                    {
                        DrawTextureRect(White, rect, color);
                    }
                    else
                    {
                        DrawLine(rect.TopLeft, rect.TopRight, color);
                        DrawLine(rect.TopRight, rect.BottomRight, color);
                        DrawLine(rect.BottomRight, rect.BottomLeft, color);
                        DrawLine(rect.BottomLeft, rect.TopLeft, color);
                    }
                }

                public override void DrawRect(in Box2Rotated rect, Color color, bool filled = true)
                {
                    if (filled)
                    {
                        DrawTextureRect(White, rect, color);
                    }
                    else
                    {
                        DrawLine(rect.TopLeft, rect.TopRight, color);
                        DrawLine(rect.TopRight, rect.BottomRight, color);
                        DrawLine(rect.BottomRight, rect.BottomLeft, color);
                        DrawLine(rect.BottomLeft, rect.TopLeft, color);
                    }
                }

                /// <summary>
                /// Draws a sprite to the world. The coordinate system is right handed.
                /// Make sure to set <see cref="DrawSetModelTransform"/>
                /// to set the model matrix if needed.
                /// </summary>
                /// <param name="texture">Texture to draw.</param>
                /// <param name="quad">The four vertices of the quad in object space (or world if the transform is identity.).</param>
                /// <param name="modulate">A color to multiply the texture by when shading.</param>
                /// <param name="subRegion">The four corners of the texture sub region in px.</param>
                public override void DrawTextureRectRegion(Texture texture, Box2 quad,
                    Color? modulate = null, UIBox2? subRegion = null)
                {
                    var color = (modulate ?? Color.White) * Modulate;

                    _renderHandle.DrawTextureWorld(texture, quad.BottomLeft, quad.BottomRight,
                        quad.TopLeft, quad.TopRight, color, in subRegion);
                }

                /// <summary>
                /// Draws a sprite to the world. The coordinate system is right handed.
                /// Make sure to set <see cref="DrawSetModelTransform"/>
                /// to set the model matrix if needed.
                /// </summary>
                /// <param name="texture">Texture to draw.</param>
                /// <param name="quad">The four vertices of the quad in object space (or world if the transform is identity.).</param>
                /// <param name="modulate">A color to multiply the texture by when shading.</param>
                /// <param name="subRegion">The four corners of the texture sub region in px.</param>
                public override void DrawTextureRectRegion(Texture texture, in Box2Rotated quad,
                    Color? modulate = null, UIBox2? subRegion = null)
                {
                    var color = (modulate ?? Color.White) * Modulate;

                    _renderHandle.DrawTextureWorld(texture, quad.BottomLeft, quad.BottomRight,
                        quad.TopLeft, quad.TopRight, color, in subRegion);
                }

                public override void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
                    ReadOnlySpan<DrawVertexUV2DColor> vertices)
                {
                    _renderHandle.DrawPrimitives(primitiveTopology, texture, vertices);
                }

                public override void DrawPrimitives(DrawPrimitiveTopology primitiveTopology, Texture texture,
                    ReadOnlySpan<ushort> indices, ReadOnlySpan<DrawVertexUV2DColor> vertices)
                {
                    _renderHandle.DrawPrimitives(primitiveTopology, texture, indices, vertices);
                }
            }
        }
    }
}
