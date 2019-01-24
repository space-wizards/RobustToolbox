using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.Graphics
{
    internal partial class DisplayManagerOpenGL
    {
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

            var renderHandle = new RenderHandle(this)
            {
                CurrentSpace = CurrentSpace.ScreenSpace
            };

            GL.VertexArrayVertexBuffer(Vertex2DVAO, 0, AnotherVBO, IntPtr.Zero, 4 * sizeof(float));
            GL.VertexArrayElementBuffer(Vertex2DVAO, AnotherEBO);

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

            foreach (var (_, commandList) in renderHandle._drawingHandles)
            {
                _processCommandList(commandList);
            }

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

            _window.SwapBuffers();
        }

        private void _processCommandList(RenderCommandList list)
        {
            foreach (var command in list.Commands)
            {
                switch (command)
                {
                    case RenderCommandTexture renderCommandTexture:
                        var loaded = _loadedTextures[renderCommandTexture.TextureId];
                        var (x, y) = renderCommandTexture.Position;
                        var vertices = new float[]
                        {
                            x + loaded.Width, y, 1, 1,
                            x, y, 0, 1,
                            x + loaded.Width, y + loaded.Height, 1, 0,
                            x, y + loaded.Height, 0, 0
                        };
                        GL.NamedBufferSubData(AnotherVBO, IntPtr.Zero, vertices.Length * sizeof(float), vertices);
                        GL.BindTextureUnit(0, loaded.OpenGLObject);
                        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
                        break;
                    case RenderCommandTransform renderCommandTransform:
                        break;
                }
            }
        }

        private class RenderHandle : IRenderHandle, IDisposable
        {
            public CurrentSpace CurrentSpace { get; set; }
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
                if (CurrentSpace != CurrentSpace.WorldSpace)
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
                if (CurrentSpace != CurrentSpace.ScreenSpace)
                {
                    throw new InvalidOperationException(
                        "Cannot create world drawing handle while not drawing in screen space.");
                }

                var handle = new DrawingHandleScreen(this, _drawingHandles.Count);
                _drawingHandles.Add((handle, _getFromPool(_manager._poolCommandList)));
                return handle;
            }

            public void SetModelTransform(ref Matrix3 matrix, int handleId)
            {
                _assertNotDisposed();

                var command = _getFromPool(_manager._poolCommandTransform);
                var list = _drawingHandles[handleId].Item2;
                command.Matrix = matrix;
                list.Commands.Add(command);
            }

            public void DrawTexture(Texture texture, Vector2 position, int handleId)
            {
                _assertNotDisposed();

                var command = _getFromPool(_manager._poolCommandTexture);
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
