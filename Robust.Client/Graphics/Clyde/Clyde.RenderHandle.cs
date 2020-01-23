using System;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private sealed class RenderHandle : IRenderHandle
        {
            private readonly Clyde _clyde;

            public DrawingHandleScreen DrawingHandleScreen { get; }
            public DrawingHandleWorld DrawingHandleWorld { get; }

            public RenderHandle(Clyde clyde)
            {
                _clyde = clyde;

                DrawingHandleScreen = new DrawingHandleScreenImpl(this);
                DrawingHandleWorld = new DrawingHandleWorldImpl(this);
            }

            public void SetModelTransform(in Matrix3 matrix)
            {
                _clyde.DrawSetModelTransform(matrix);
            }

            public void SetViewTransform(in Matrix3 matrix)
            {
                _clyde.DrawSetViewTransform(matrix);
            }

            public void ResetViewTransform()
            {
                _clyde.DrawResetViewTransform();
            }

            public void DrawTexture(Texture texture, Vector2 a, Vector2 b, Color modulate, UIBox2? subRegion,
                Angle angle)
            {
                if (texture is AtlasTexture atlas)
                {
                    texture = atlas.SourceTexture;
                    if (subRegion.HasValue)
                    {
                        var offset = atlas.SubRegion.TopLeft;
                        subRegion = new UIBox2(
                            subRegion.Value.TopLeft + offset,
                            subRegion.Value.BottomRight + offset);
                    }
                    else
                    {
                        subRegion = atlas.SubRegion;
                    }
                }

                var clydeTexture = (ClydeTexture) texture;

                _clyde.DrawTexture(clydeTexture.TextureId, a, b, modulate, subRegion, angle);
            }

            public void SetScissor(UIBox2i? scissorBox)
            {
                _clyde.DrawSetScissor(scissorBox);
            }

            public void SetSpace(CurrentSpace space)
            {
                _clyde.DrawSwitchSpace(space);
            }

            public void DrawEntity(IEntity entity, Vector2 position, Vector2 scale, Direction? overrideDirection)
            {
                if (entity.Deleted)
                {
                    throw new ArgumentException("Tried to draw an entity has been deleted.", nameof(entity));
                }

                var sprite = entity.GetComponent<SpriteComponent>();

                // Switch rendering to world space.
                SetSpace(CurrentSpace.WorldSpace);

                {
                    var ofsX = position.X - _clyde.ScreenSize.X / 2f;
                    var ofsY = position.Y - _clyde.ScreenSize.Y / 2f;

                    var viewMatrix = Matrix3.Identity;
                    viewMatrix.R0C0 = scale.X;
                    viewMatrix.R1C1 = scale.Y;
                    viewMatrix.R0C2 = ofsX / EyeManager.PIXELSPERMETER;
                    viewMatrix.R1C2 = -ofsY / EyeManager.PIXELSPERMETER;

                    SetViewTransform(viewMatrix);
                }

                // Draw the entity.
                sprite.Render(
                    DrawingHandleWorld,
                    overrideDirection == null
                        ? entity.Transform.WorldRotation
                        : Angle.Zero,
                    overrideDirection);

                // Reset to screen space
                SetSpace(CurrentSpace.ScreenSpace);
            }

            public void DrawLine(Vector2 a, Vector2 b, Color color)
            {
                _clyde.DrawLine(a, b, color);
            }

            public void UseShader(ShaderInstance shader)
            {
                if (shader != null && shader.Disposed)
                {
                    throw new ArgumentException("Unable to use disposed shader instance.", nameof(shader));
                }

                var clydeShader = (ClydeShaderInstance) shader;

                _clyde.DrawUseShader(clydeShader?.Handle ?? _clyde._defaultShader.Handle);
            }

            public void Viewport(Box2i viewport)
            {
                _clyde.DrawViewport(viewport);
            }

            public void UseRenderTarget(IRenderTarget renderTarget)
            {
                var target = (RenderTarget) renderTarget;

                _clyde.DrawRenderTarget(target?.Handle ?? default);
            }

            public void Clear(Color color)
            {
                _clyde.DrawClear(color);
            }

            private sealed class DrawingHandleScreenImpl : DrawingHandleScreen
            {
                private readonly RenderHandle _renderHandle;

                public DrawingHandleScreenImpl(RenderHandle renderHandle)
                {
                    _renderHandle = renderHandle;
                }

                public override void SetTransform(in Matrix3 matrix)
                {
                    _renderHandle.SetModelTransform(matrix);
                }

                public override void UseShader(ShaderInstance shader)
                {
                    _renderHandle.UseShader(shader);
                }

                public override void DrawCircle(Vector2 position, float radius, Color color)
                {
                    // TODO: Implement this.
                }

                public override void DrawLine(Vector2 from, Vector2 to, Color color)
                {
                    _renderHandle.DrawLine(@from, to, color * Modulate);
                }

                public override void DrawRect(UIBox2 rect, Color color, bool filled = true)
                {
                    if (filled)
                    {
                        DrawTextureRect(Texture.White, rect, color);
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
                    _renderHandle.DrawTexture(texture, rect.TopLeft, rect.BottomRight, color,
                        subRegion, 0);
                }
            }

            private sealed class DrawingHandleWorldImpl : DrawingHandleWorld
            {
                private readonly RenderHandle _renderHandle;

                public DrawingHandleWorldImpl(RenderHandle renderHandle)
                {
                    _renderHandle = renderHandle;
                }

                public override void SetTransform(in Matrix3 matrix)
                {
                    _renderHandle.SetModelTransform(matrix);
                }

                public override void UseShader(ShaderInstance shader)
                {
                    _renderHandle.UseShader(shader);
                }

                public override void DrawCircle(Vector2 position, float radius, Color color)
                {
                    // TODO: Implement this.
                }

                public override void DrawLine(Vector2 from, Vector2 to, Color color)
                {
                    _renderHandle.DrawLine(@from, to, color * Modulate);
                }

                public override void DrawRect(Box2 rect, Color color, bool filled = true)
                {
                    if (filled)
                    {
                        DrawTextureRect(Texture.White, rect, color);
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
                        DrawTextureRect(Texture.White, rect, color);
                    }
                    else
                    {
                        DrawLine(rect.TopLeft, rect.TopRight, color);
                        DrawLine(rect.TopRight, rect.BottomRight, color);
                        DrawLine(rect.BottomRight, rect.BottomLeft, color);
                        DrawLine(rect.BottomLeft, rect.TopLeft, color);
                    }
                }

                public override void DrawTextureRectRegion(Texture texture, Box2 rect, UIBox2? subRegion = null,
                    Color? modulate = null)
                {
                    var color = (modulate ?? Color.White) * Modulate;

                    _renderHandle.DrawTexture(texture, rect.BottomLeft, rect.TopRight, color, subRegion, 0);
                }

                public override void DrawTextureRectRegion(Texture texture, in Box2Rotated rect,
                    UIBox2? subRegion = null, Color? modulate = null)
                {
                    var color = (modulate ?? Color.White) * Modulate;

                    _renderHandle.DrawTexture(texture, rect.Box.BottomLeft, rect.Box.TopRight, color, subRegion,
                        (float) rect.Rotation);
                }
            }
        }
    }
}
