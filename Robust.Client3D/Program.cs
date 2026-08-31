using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Shared.Maths;
using SDL3;

namespace Robust.Client3D;

internal static class Program
{
    private const string VertexShaderSource = """
        #version 330 core

        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec3 aColor;

        uniform mat4 uMvp;

        out vec3 vColor;

        void main()
        {
            vColor = aColor;
            gl_Position = uMvp * vec4(aPosition, 1.0);
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core

        in vec3 vColor;
        out vec4 fragColor;

        void main()
        {
            fragColor = vec4(vColor, 1.0);
        }
        """;

    [STAThread]
    public static unsafe int Main(string[] args)
    {
        var frameLimit = ReadFrameLimit(args);
        var screenshotPath = ReadScreenshotPath(args);

        if (!SDL.SDL_Init(SDL.SDL_InitFlags.SDL_INIT_VIDEO | SDL.SDL_InitFlags.SDL_INIT_EVENTS))
        {
            Console.Error.WriteLine($"SDL initialization failed: {SDL.SDL_GetError()}");
            return 1;
        }

        IntPtr window = IntPtr.Zero;
        IntPtr context = IntPtr.Zero;
        uint vertexArray = 0;
        uint vertexBuffer = 0;
        uint program = 0;

        try
        {
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, 3);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK,
                SDL.SDL_GL_CONTEXT_PROFILE_CORE);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLAttr.SDL_GL_CONTEXT_FLAGS,
                SDL.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLAttr.SDL_GL_DOUBLEBUFFER, 1);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLAttr.SDL_GL_DEPTH_SIZE, 24);

            window = SDL.SDL_CreateWindow(
                "RussianCM - incompatible 3D engine prototype",
                1280,
                720,
                SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);

            if (window == IntPtr.Zero)
                throw new InvalidOperationException($"Window creation failed: {SDL.SDL_GetError()}");

            context = SDL.SDL_GL_CreateContext(window);
            if (context == IntPtr.Zero)
                throw new InvalidOperationException($"OpenGL context creation failed: {SDL.SDL_GetError()}");

            if (!SDL.SDL_GL_MakeCurrent(window, context))
                throw new InvalidOperationException($"OpenGL context activation failed: {SDL.SDL_GetError()}");

            GL.LoadBindings(new SdlBindingsContext());
            SDL.SDL_GL_SetSwapInterval(1);

            Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");
            Console.WriteLine($"OpenGL: {GL.GetString(StringName.Version)}");

            program = CreateProgram(VertexShaderSource, FragmentShaderSource);
            var mvpLocation = GL.GetUniformLocation((int) program, "uMvp");
            if (mvpLocation < 0)
                throw new InvalidOperationException("The 3D shader has no uMvp uniform.");

            var vertices = CreateCubeVertices();
            GL.GenVertexArrays(1, out vertexArray);
            GL.GenBuffers(1, out vertexBuffer);
            GL.BindVertexArray(vertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);

            fixed (float* vertexPointer = vertices)
            {
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    vertices.Length * sizeof(float),
                    (IntPtr) vertexPointer,
                    BufferUsageHint.StaticDraw);
            }

            const int vertexStride = 6 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexStride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexStride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.ClearColor(0.025f, 0.035f, 0.065f, 1f);

            var startTime = Environment.TickCount64;
            var frame = 0;
            var running = true;

            while (running)
            {
                while (SDL.SDL_PollEvent(out var ev))
                {
                    var type = (SDL.SDL_EventType) ev.type;
                    if (type is SDL.SDL_EventType.SDL_EVENT_QUIT or
                        SDL.SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED)
                    {
                        running = false;
                    }
                }

                SDL.SDL_GetWindowSizeInPixels(window, out var width, out var height);
                if (width <= 0 || height <= 0)
                    continue;

                GL.Viewport(0, 0, width, height);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                var elapsed = (Environment.TickCount64 - startTime) / 1000f;
                var orbit = elapsed * 0.22f;
                var camera = new Vector3(
                    MathF.Sin(orbit) * 12f,
                    -MathF.Cos(orbit) * 12f,
                    7.5f);
                var view = Matrix4x4.CreateLookAt(camera, new Vector3(0f, 0f, 1.2f), Vector3.UnitZ);
                var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                    MathF.PI / 3f,
                    width / (float) height,
                    0.05f,
                    100f);

                GL.UseProgram(program);
                GL.BindVertexArray(vertexArray);
                DrawWorld(mvpLocation, view, projection, elapsed);

                if (screenshotPath is not null &&
                    (frameLimit is null ? frame == 0 : frame + 1 >= frameLimit.Value))
                {
                    SaveFramebuffer(screenshotPath, width, height);
                    screenshotPath = null;
                }

                SDL.SDL_GL_SwapWindow(window);
                frame++;
                if (frameLimit is not null && frame >= frameLimit.Value)
                    running = false;
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (vertexBuffer != 0)
                GL.DeleteBuffer(vertexBuffer);
            if (vertexArray != 0)
                GL.DeleteVertexArray(vertexArray);
            if (program != 0)
                GL.DeleteProgram(program);
            if (context != IntPtr.Zero)
                SDL.SDL_GL_DestroyContext(context);
            if (window != IntPtr.Zero)
                SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }
    }

    private static int? ReadFrameLimit(string[] args)
    {
        const string prefix = "--frames=";
        foreach (var argument in args)
        {
            if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(argument[prefix.Length..], out var frames) && frames > 0)
                return frames;
        }

        return null;
    }

    private static string? ReadScreenshotPath(string[] args)
    {
        const string prefix = "--screenshot=";
        foreach (var argument in args)
        {
            if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var path = argument[prefix.Length..];
            if (!string.IsNullOrWhiteSpace(path))
                return Path.GetFullPath(path);
        }

        return null;
    }

    private static unsafe void SaveFramebuffer(string path, int width, int height)
    {
        var stride = (width * 3 + 3) & ~3;
        var pixels = new byte[stride * height];

        GL.PixelStore(PixelStoreParameter.PackAlignment, 4);
        fixed (byte* pixelPointer = pixels)
        {
            GL.ReadPixels(
                0,
                0,
                width,
                height,
                PixelFormat.Bgr,
                PixelType.UnsignedByte,
                (IntPtr) pixelPointer);
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        const int headerSize = 54;
        writer.Write((byte) 'B');
        writer.Write((byte) 'M');
        writer.Write(headerSize + pixels.Length);
        writer.Write(0);
        writer.Write(headerSize);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short) 1);
        writer.Write((short) 24);
        writer.Write(0);
        writer.Write(pixels.Length);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);
        writer.Write(pixels);

        Console.WriteLine($"Rendered frame: {path}");
    }

    private static unsafe void DrawWorld(int mvpLocation, Matrix4x4 view, Matrix4x4 projection, float elapsed)
    {
        DrawCube(new SpatialTransform(
            new Vector3(0f, 0f, -0.3f),
            Quaternion.Identity,
            new Vector3(9f, 9f, 0.6f)), mvpLocation, view, projection);

        DrawCube(new SpatialTransform(
            new Vector3(0f, 4.6f, 2.2f),
            Quaternion.Identity,
            new Vector3(9f, 0.35f, 5f)), mvpLocation, view, projection);
        DrawCube(new SpatialTransform(
            new Vector3(-4.6f, 0f, 2.2f),
            Quaternion.Identity,
            new Vector3(0.35f, 9f, 5f)), mvpLocation, view, projection);

        DrawCube(new SpatialTransform(
            new Vector3(2.4f, 1.5f, 0.65f),
            Quaternion.CreateFromYawPitchRoll(0.35f, 0f, 0f),
            new Vector3(2.3f, 1.4f, 1.3f)), mvpLocation, view, projection);
        DrawCube(new SpatialTransform(
            new Vector3(-2.3f, 2.1f, 1f),
            Quaternion.CreateFromYawPitchRoll(-0.25f, 0f, 0f),
            new Vector3(1.25f, 1.25f, 2f)), mvpLocation, view, projection);
        DrawCube(new SpatialTransform(
            new Vector3(2.8f, -2.4f, 1.5f),
            Quaternion.CreateFromYawPitchRoll(0.65f, 0.2f, 0f),
            new Vector3(0.7f, 0.7f, 3f)), mvpLocation, view, projection);

        var animatedRotation = Quaternion.CreateFromYawPitchRoll(elapsed * 0.8f, elapsed * 0.35f, 0f);
        DrawCube(new SpatialTransform(
            new Vector3(-0.4f, -0.6f, 1.45f + MathF.Sin(elapsed * 1.5f) * 0.2f),
            animatedRotation,
            new Vector3(1.45f)), mvpLocation, view, projection);
    }

    private static unsafe void DrawCube(
        SpatialTransform transform,
        int mvpLocation,
        Matrix4x4 view,
        Matrix4x4 projection)
    {
        // System.Numerics stores row-vector matrices in row-major order. OpenGL reads the same
        // bytes as a column-major matrix, which already provides the required transpose.
        var mvp = transform.Matrix * view * projection;
        GL.UniformMatrix4(mvpLocation, 1, false, (float*) &mvp);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }

    private static uint CreateProgram(string vertexSource, string fragmentSource)
    {
        var vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);
        var program = (uint) GL.CreateProgram();

        try
        {
            GL.AttachShader(program, vertex);
            GL.AttachShader(program, fragment);
            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var linked);
            if (linked != 1)
                throw new InvalidOperationException($"3D shader link failed: {GL.GetProgramInfoLog((int) program)}");

            return program;
        }
        catch
        {
            GL.DeleteProgram(program);
            throw;
        }
        finally
        {
            GL.DetachShader(program, vertex);
            GL.DetachShader(program, fragment);
            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);
        }
    }

    private static uint CompileShader(ShaderType type, string source)
    {
        var shader = (uint) GL.CreateShader(type);
        GL.ShaderSource((int) shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var compiled);

        if (compiled == 1)
            return shader;

        var message = GL.GetShaderInfoLog((int) shader);
        GL.DeleteShader(shader);
        throw new InvalidOperationException($"3D shader compilation failed: {message}");
    }

    private static float[] CreateCubeVertices()
    {
        var vertices = new List<float>(36 * 6);

        AddFace(vertices,
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.22f, 0.78f, 0.96f));
        AddFace(vertices,
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.08f, 0.18f, 0.35f));
        AddFace(vertices,
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.14f, 0.46f, 0.78f));
        AddFace(vertices,
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(0.32f, 0.56f, 0.88f));
        AddFace(vertices,
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.25f, 0.68f, 0.72f));
        AddFace(vertices,
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.56f, 0.34f, 0.86f));

        return vertices.ToArray();
    }

    private static void AddFace(
        List<float> destination,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 d,
        Vector3 color)
    {
        AddVertex(destination, a, color);
        AddVertex(destination, b, color);
        AddVertex(destination, c, color);
        AddVertex(destination, a, color);
        AddVertex(destination, c, color);
        AddVertex(destination, d, color);
    }

    private static void AddVertex(List<float> destination, Vector3 position, Vector3 color)
    {
        destination.Add(position.X);
        destination.Add(position.Y);
        destination.Add(position.Z);
        destination.Add(color.X);
        destination.Add(color.Y);
        destination.Add(color.Z);
    }
}
