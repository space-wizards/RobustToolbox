using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using SS14.Client.GameObjects;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.Graphics
{
    internal partial class DisplayManagerOpenGL
    {
        private Matrix3 _currentModelMatrix = Matrix3.Identity;
        private CurrentSpace _currentSpace;

        public void Render(FrameEventArgs args)
        {
            if (GameController.Mode != GameController.DisplayMode.OpenGL)
            {
                return;
            }

            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.BindVertexArray(Vertex2DVAO);
            GL.UseProgram(Vertex2DProgram);

            var eye = _eyeManager.CurrentEye;

            // TODO: UBO?
            OpenTK.Matrix3 projMatrixWorld;
            OpenTK.Matrix3 projMatrixScreen;
            OpenTK.Matrix3 viewMatrixWorld;
            var viewMatrixScreen = OpenTK.Matrix3.Identity;
            {
                // Make projection matrices.
                // World projection matrix.
                var worldProj = Matrix3.Identity;
                worldProj.R0C0 = EyeManager.PIXELSPERMETER * 2f / _window.Width;
                worldProj.R1C1 = EyeManager.PIXELSPERMETER * 2f / _window.Height;
                projMatrixWorld = worldProj.ConvertOpenTK();
                // Screen projection matrix.
                var screenProj = Matrix3.Identity;
                screenProj.R0C0 = 2f / _window.Width;
                screenProj.R1C1 = -2f / _window.Height;
                screenProj.R0C2 = -1;
                screenProj.R1C2 = 1;
                projMatrixScreen = screenProj.ConvertOpenTK();

                // Make view matrices.
                // World view matrix.
                var worldView = Matrix3.Identity;
                worldView.R0C0 = 1 / eye.Zoom.X;
                worldView.R1C1 = 1 / eye.Zoom.Y;
                worldView.R0C2 = -eye.Position.X / eye.Zoom.X;
                worldView.R1C2 = -eye.Position.Y / eye.Zoom.Y;
                viewMatrixWorld = worldView.ConvertOpenTK();
            }

            var renderHandle = new RenderHandle(this);
            _currentSpace = CurrentSpace.ScreenSpace;


            {
                var identity = OpenTK.Matrix3.Identity;
                GL.UniformMatrix3(Vertex2DUniformModel, false, ref identity);
            }

            // Render ScreenSpaceBelowWorld overlays.
            {
                // Switch matrices to screen space.
                GL.UniformMatrix3(Vertex2DUniformView, true, ref viewMatrixScreen);
                GL.UniformMatrix3(Vertex2DUniformProjection, true, ref projMatrixScreen);
                foreach (var overlay in _overlayManager.AllOverlays
                    .Where(o => o.Space == OverlaySpace.ScreenSpaceBelowWorld)
                    .OrderBy(o => o.ZIndex))
                {
                    overlay.OpenGLRender(renderHandle);
                }
            }

            _flushRenderHandle(renderHandle);

            // Render grids.
            var map = _eyeManager.CurrentMap;

            var tex = _resourceCache.GetResource<TextureResource>("/Textures/Tiles/floor_steel.png");
            var loadedTex = _loadedTextures[((OpenGLTexture) tex.Texture).OpenGLTextureId];

            GL.BindTextureUnit(0, loadedTex.OpenGLObject);

            // Make view matrix.
            var vertices = new float[65536 * 4];
            var indices = new ushort[65536 / 4 * 6];
            ushort nth = 0;

            GL.UniformMatrix3(Vertex2DUniformView, true, ref viewMatrixWorld);
            GL.UniformMatrix3(Vertex2DUniformProjection, true, ref projMatrixWorld);

            GL.VertexArrayVertexBuffer(Vertex2DVAO, 0, AnotherVBO, IntPtr.Zero, 4 * sizeof(float));
            GL.VertexArrayElementBuffer(Vertex2DVAO, AnotherEBO);

            foreach (var grid in _mapManager.GetMap(map).GetAllGrids())
            {
                var matrix = Matrix3.Identity;
                matrix.R0C2 = grid.WorldPosition.X;
                matrix.R1C2 = grid.WorldPosition.Y;
                var otkm = matrix.ConvertOpenTK();
                GL.UniformMatrix3(Vertex2DUniformModel, true, ref otkm);

                foreach (var tileRef in grid.GetAllTiles())
                {
                    var vidx = nth * 16;
                    vertices[vidx] = tileRef.X + 0.5f;
                    vertices[vidx + 1] = tileRef.Y + 0.5f;
                    vertices[vidx + 2] = 1;
                    vertices[vidx + 3] = 0;
                    vertices[vidx + 4] = tileRef.X - 0.5f;
                    vertices[vidx + 5] = tileRef.Y + 0.5f;
                    vertices[vidx + 6] = 0;
                    vertices[vidx + 7] = 0;
                    vertices[vidx + 8] = tileRef.X + 0.5f;
                    vertices[vidx + 9] = tileRef.Y - 0.5f;
                    vertices[vidx + 10] = 1;
                    vertices[vidx + 11] = 1;
                    vertices[vidx + 12] = tileRef.X - 0.5f;
                    vertices[vidx + 13] = tileRef.Y - 0.5f;
                    vertices[vidx + 14] = 0;
                    vertices[vidx + 15] = 1;
                    var nidx = nth * 6;
                    var tidx = (ushort) (nth * 4);
                    indices[nidx] = tidx;
                    indices[nidx + 1] = (ushort) (tidx + 1);
                    indices[nidx + 2] = (ushort) (tidx + 2);
                    indices[nidx + 3] = (ushort) (tidx + 1);
                    indices[nidx + 4] = (ushort) (tidx + 2);
                    indices[nidx + 5] = (ushort) (tidx + 3);
                    checked
                    {
                        nth += 1;
                    }
                }

                if (nth == 0)
                {
                    continue;
                }

                GL.NamedBufferSubData(AnotherVBO, IntPtr.Zero, nth * sizeof(float) * 16, vertices);
                GL.NamedBufferSubData(AnotherEBO, IntPtr.Zero, nth * sizeof(ushort) * 6, indices);

                GL.DrawElements(PrimitiveType.Triangles, nth * 6, DrawElementsType.UnsignedShort, 0);
            }

            _currentSpace = CurrentSpace.WorldSpace;

            var drawingHandle = renderHandle.CreateHandleWorld();

            // Draw entities.
            foreach (var entity in _entityManager.GetEntities())
            {
                if (!entity.TryGetComponent(out SpriteComponent sprite))
                {
                    continue;
                }

                var modelMatrix = entity.Transform.WorldMatrix.ConvertOpenTK();
                sprite.OpenGLRender(drawingHandle);
            }

            _flushRenderHandle(renderHandle);

            _window.SwapBuffers();
        }

        private void _processCommandList(RenderCommandList list)
        {
            foreach (var command in list.Commands)
            {
                switch (command)
                {
                    case RenderCommandTexture renderCommandTexture:
                    {
                        // Use QuadVBO to render a single quad and modify the model matrix to position it where we need it.
                        var loaded = _loadedTextures[renderCommandTexture.TextureId];
                        Vector2 size;
                        if (renderCommandTexture.HasSubRegion)
                        {
                            size = renderCommandTexture.SubRegion.Size;
                            var subRegion = renderCommandTexture.SubRegion;
                            var (w, h) = loaded.Size;
                            var vec = new OpenTK.Vector4(subRegion.Left / w, subRegion.Top / h, subRegion.Right / w,
                                subRegion.Bottom / h);
                            GL.Uniform4(Vertex2DUniformModUV, vec);
                        }
                        else
                        {
                            size = loaded.Size;
                            GL.Uniform4(Vertex2DUniformModUV, new OpenTK.Vector4(0, 0, 1, 1));
                        }

                        if (_currentSpace == CurrentSpace.WorldSpace)
                        {
                            size /= EyeManager.PIXELSPERMETER;
                        }

                        var rectTransform = Matrix3.Identity;
                        (rectTransform.R0C0, rectTransform.R1C1) = size;
                        (rectTransform.R0C2, rectTransform.R1C2) = renderCommandTexture.Position;
                        rectTransform.Multiply(ref _currentModelMatrix);
                        var oRectTransform = rectTransform.ConvertOpenTK();

                        GL.UniformMatrix3(Vertex2DUniformModel, true, ref oRectTransform);
                        GL.BindVertexBuffer(0, QuadVBO, IntPtr.Zero, 4 * sizeof(float));
                        GL.BindTextureUnit(0, loaded.OpenGLObject);
                        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
                        break;
                    }
                    case RenderCommandTransform renderCommandTransform:
                        _currentModelMatrix = renderCommandTransform.Matrix;
                        break;
                }
            }
        }

        /// <summary>
        ///     Flush the render handle, processing and re-pooling all the command lists.
        /// </summary>
        private void _flushRenderHandle(RenderHandle handle)
        {
            foreach (var (drawHandle, commandList) in handle._drawingHandles)
            {
                drawHandle.Dispose();
                _processCommandList(commandList);

                // TODO: Would it make sense to merge this into the same loop in _processCommandList?
                // Seems like a waste to iterate twice.
                foreach (var command in commandList.Commands)
                {
                    switch (command)
                    {
                        case RenderCommandTexture renderCommandTexture:
                            _returnCommandTexture(renderCommandTexture);
                            break;
                        case RenderCommandTransform renderCommandTransform:
                            _returnCommandTransform(renderCommandTransform);
                            break;
                    }
                }

                commandList.Commands.Clear();
                _returnCommandList(commandList);
            }

            handle._drawingHandles.Clear();
        }

        private class RenderHandle : IRenderHandle, IDisposable
        {
            private readonly DisplayManagerOpenGL _manager;

            public readonly List<(DrawingHandle, RenderCommandList)> _drawingHandles =
                new List<(DrawingHandle, RenderCommandList)>();

            private bool _disposed;

            public RenderHandle(DisplayManagerOpenGL manager)
            {
                _manager = manager;
            }

            public DrawingHandleWorld CreateHandleWorld()
            {
                _assertNotDisposed();
                if (_manager._currentSpace != CurrentSpace.WorldSpace)
                {
                    throw new InvalidOperationException(
                        "Cannot create world drawing handle while not drawing in world space.");
                }

                var handle = new DrawingHandleWorld(this, _drawingHandles.Count);
                _drawingHandles.Add((handle, _getFromPool(_manager._poolCommandList)));
                return handle;
            }

            public DrawingHandleScreen CreateHandleScreen()
            {
                _assertNotDisposed();
                if (_manager._currentSpace != CurrentSpace.ScreenSpace)
                {
                    throw new InvalidOperationException(
                        "Cannot create world drawing handle while not drawing in screen space.");
                }

                var handle = new DrawingHandleScreen(this, _drawingHandles.Count);
                _drawingHandles.Add((handle, _manager._getNewCommandList()));
                return handle;
            }

            public void SetModelTransform(ref Matrix3 matrix, int handleId)
            {
                _assertNotDisposed();

                var command = _manager._getNewCommandTransform();
                var list = _drawingHandles[handleId].Item2;
                command.Matrix = matrix;
                list.Commands.Add(command);
            }

            public void DrawTexture(Texture texture, Vector2 position, int handleId)
            {
                _assertNotDisposed();

                var command = _manager._getNewCommandTexture();
                switch (texture)
                {
                    case BlankTexture _:
                        texture = IoCManager.Resolve<IResourceCache>().GetFallback<TextureResource>();
                        break;
                    case AtlasTexture atlas:
                    {
                        texture = atlas.SourceTexture;
                        command.SubRegion = atlas.SubRegion;
                        command.HasSubRegion = true;
                        break;
                    }
                }

                var list = _drawingHandles[handleId].Item2;
                command.TextureId = ((OpenGLTexture) texture).OpenGLTextureId;
                command.Position = position;
                list.Commands.Add(command);
            }

            public void DrawTextureRect(Texture texture, Vector2 a, Vector2 b, int handleId)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                _disposed = true;
            }

            private void _assertNotDisposed()
            {
                DebugTools.Assert(!_disposed);
            }
        }

        private enum CurrentSpace
        {
            ScreenSpace,
            WorldSpace,
        }

        private class RenderCommandList
        {
            public List<RenderCommand> Commands { get; } = new List<RenderCommand>();
        }

        private abstract class RenderCommand
        {
        }

        private class RenderCommandTexture : RenderCommand
        {
            public int TextureId { get; set; }
            public Vector2 Position { get; set; }
            public bool HasSubRegion { get; set; }
            public UIBox2 SubRegion { get; set; }
        }

        private class RenderCommandTransform : RenderCommand
        {
            public Matrix3 Matrix { get; set; }
        }
    }


    internal interface IRenderHandle
    {
        DrawingHandleWorld CreateHandleWorld();
        DrawingHandleScreen CreateHandleScreen();

        void SetModelTransform(ref Matrix3 matrix, int handleId);
        void DrawTexture(Texture texture, Vector2 position, int handleId);
        void DrawTextureRect(Texture texture, Vector2 a, Vector2 b, int handleId);
    }
}
