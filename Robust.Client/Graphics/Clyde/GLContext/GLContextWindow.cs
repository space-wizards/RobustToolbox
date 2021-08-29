using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        /// <summary>
        ///     GL Context(s) provided by the windowing system (GLFW, SDL2...)
        /// </summary>
        private sealed class GLContextWindow : GLContextBase
        {
            private readonly Dictionary<WindowId, WindowData> _windowData = new();

            public override bool GlesOnly => false;

            public GLContextWindow(Clyde clyde) : base(clyde)
            {
            }

            public override GLContextSpec? SpecWithOpenGLVersion(RendererOpenGLVersion version)
            {
                return GetVersionSpec(version);
            }

            public override void UpdateVSync()
            {
                if (Clyde._mainWindow == null)
                    return;

                Clyde._windowing!.GLMakeContextCurrent(Clyde._mainWindow);
                Clyde._windowing.GLSwapInterval(Clyde._vSync ? 1 : 0);
            }

            public override void WindowCreated(WindowReg reg)
            {
                var data = new WindowData
                {
                    Reg = reg
                };

                _windowData[reg.Id] = data;

                if (reg.IsMainWindow)
                {
                    UpdateVSync();
                }
                else
                {
                    Clyde._windowing!.GLMakeContextCurrent(Clyde._mainWindow);

                    CreateWindowRenderTexture(data);
                    InitWindowBlitThread(data);
                }
            }

            public override void WindowDestroyed(WindowReg reg)
            {
                var data = _windowData[reg.Id];
                data.BlitDoneEvent?.Set();
            }


            public override void Shutdown()
            {
                // Nada, window system shutdown handles it.
            }

            public override void SwapAllBuffers()
            {
                BlitSecondaryWindows();

                Clyde._windowing!.WindowSwapBuffers(Clyde._mainWindow!);
            }

            public override void WindowResized(WindowReg reg, Vector2i oldSize)
            {
                if (reg.IsMainWindow)
                    return;

                // Recreate render texture for the window.
                var data = _windowData[reg.Id];
                data.RenderTexture!.Dispose();
                CreateWindowRenderTexture(data);
            }

            public override unsafe void* GetProcAddress(string name)
            {
                return Clyde._windowing!.GLGetProcAddress(name);
            }

            public override void BindWindowRenderTarget(WindowId rtWindowId)
            {
                var data = _windowData[rtWindowId];
                if (data.Reg.IsMainWindow)
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    Clyde.CheckGlError();
                }
                else
                {
                    var loaded = Clyde.RtToLoaded(data.RenderTexture!);
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, loaded.FramebufferHandle.Handle);
                }
            }

            public override void BeforeSharedWindowCreateUnbind()
            {
                Clyde._windowing!.GLMakeContextCurrent(null);
            }

            private void BlitSecondaryWindows()
            {
                // Only got main window.
                if (Clyde._windows.Count == 1)
                    return;

                if (!Clyde._hasGLFenceSync && Clyde._cfg.GetCVar(CVars.DisplayForceSyncWindows))
                {
                    GL.Finish();
                }

                if (Clyde.EffectiveThreadWindowBlit)
                {
                    foreach (var window in _windowData.Values)
                    {
                        if (window.Reg.IsMainWindow)
                            continue;

                        window.BlitDoneEvent!.Reset();
                        window.BlitStartEvent!.Set();
                        window.BlitDoneEvent.Wait();
                    }
                }
                else
                {
                    foreach (var window in _windowData.Values)
                    {
                        if (window.Reg.IsMainWindow)
                            continue;

                        Clyde._windowing!.GLMakeContextCurrent(window.Reg);
                        BlitThreadDoSecondaryWindowBlit(window);
                    }

                    Clyde._windowing!.GLMakeContextCurrent(Clyde._mainWindow!);
                }
            }

            private void BlitThreadDoSecondaryWindowBlit(WindowData window)
            {
                var rt = window.RenderTexture!;

                if (Clyde._hasGLFenceSync)
                {
                    // 0xFFFFFFFFFFFFFFFFUL is GL_TIMEOUT_IGNORED
                    var sync = rt.LastGLSync;
                    GL.WaitSync(sync, WaitSyncFlags.None, unchecked((long) 0xFFFFFFFFFFFFFFFFUL));
                    Clyde.CheckGlError();
                }

                GL.Viewport(0, 0, window.Reg.FramebufferSize.X, window.Reg.FramebufferSize.Y);
                Clyde.CheckGlError();

                Clyde.SetTexture(TextureUnit.Texture0, window.RenderTexture!.Texture);

                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
                Clyde.CheckGlError();

                window.BlitDoneEvent?.Set();
                Clyde._windowing!.WindowSwapBuffers(window.Reg);
            }

            private void BlitThreadInit(WindowData reg)
            {
                Clyde._windowing!.GLMakeContextCurrent(reg.Reg);
                Clyde._windowing.GLSwapInterval(0);

                if (!Clyde._isGLES)
                    GL.Enable(EnableCap.FramebufferSrgb);

                var vao = GL.GenVertexArray();
                GL.BindVertexArray(vao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, Clyde.WindowVBO.ObjectHandle);
                // Vertex Coords
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 0);
                GL.EnableVertexAttribArray(0);
                // Texture Coords.
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Vertex2D.SizeOf, 2 * sizeof(float));
                GL.EnableVertexAttribArray(1);

                var program = Clyde._compileProgram(
                    Clyde._winBlitShaderVert,
                    Clyde._winBlitShaderFrag,
                    new (string, uint)[]
                    {
                        ("aPos", 0),
                        ("tCoord", 1),
                    },
                    includeLib: false);

                GL.UseProgram(program.Handle);
                var loc = GL.GetUniformLocation(program.Handle, "tex");
                Clyde.SetTexture(TextureUnit.Texture0, reg.RenderTexture!.Texture);
                GL.Uniform1(loc, 0);
            }

            private void InitWindowBlitThread(WindowData reg)
            {
                if (Clyde.EffectiveThreadWindowBlit)
                {
                    reg.BlitStartEvent = new ManualResetEventSlim();
                    reg.BlitDoneEvent = new ManualResetEventSlim();
                    reg.BlitThread = new Thread(() => BlitThread(reg))
                    {
                        Name = $"WinBlitThread ID:{reg.Reg.Id}",
                        IsBackground = true
                    };

                    // System.Console.WriteLine("A");
                    reg.BlitThread.Start();
                    // Wait for thread to finish init.
                    reg.BlitDoneEvent.Wait();
                }
                else
                {
                    // Binds GL context.
                    BlitThreadInit(reg);

                    Clyde._windowing!.GLMakeContextCurrent(Clyde._mainWindow!);
                }
            }

            private void BlitThread(WindowData reg)
            {
                BlitThreadInit(reg);

                reg.BlitDoneEvent!.Set();

                try
                {
                    while (true)
                    {
                        reg.BlitStartEvent!.Wait();
                        if (reg.Reg.IsDisposed)
                        {
                            BlitThreadCleanup(reg);
                            return;
                        }

                        reg.BlitStartEvent!.Reset();

                        // Do channel blit.
                        BlitThreadDoSecondaryWindowBlit(reg);
                    }
                }
                catch (AggregateException e)
                {
                    // ok channel closed, we exit.
                    e.Handle(ec => ec is ChannelClosedException);
                }
            }

            private static void BlitThreadCleanup(WindowData reg)
            {
                reg.BlitDoneEvent!.Dispose();
                reg.BlitStartEvent!.Dispose();
            }

            private void CreateWindowRenderTexture(WindowData reg)
            {
                reg.RenderTexture?.Dispose();

                reg.RenderTexture = Clyde.CreateRenderTarget(reg.Reg.FramebufferSize, new RenderTargetFormatParameters
                {
                    ColorFormat = RenderTargetColorFormat.Rgba8Srgb,
                    HasDepthStencil = true
                });
                // Necessary to correctly sync multi-context blitting.
                reg.RenderTexture.MakeGLFence = true;
            }

            private sealed class WindowData
            {
                public WindowReg Reg = default!;

                public RenderTexture? RenderTexture;
                // Used EXCLUSIVELY to run the two rendering commands to blit to the window.
                public Thread? BlitThread;
                public ManualResetEventSlim? BlitStartEvent;
                public ManualResetEventSlim? BlitDoneEvent;
            }
        }
    }
}
