using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        uniform vec3 uTint;

        out vec3 vColor;

        void main()
        {
            vColor = mix(aColor, uTint, 0.72);
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
        var autoPlay = HasArgument(args, "--autoplay");

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
            var tintLocation = GL.GetUniformLocation((int) program, "uTint");
            if (tintLocation < 0)
                throw new InvalidOperationException("The 3D shader has no uTint uniform.");

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

            var interactive = frameLimit is null && !autoPlay;
            if (interactive && !SDL.SDL_SetWindowRelativeMouseMode(window, true))
                Console.Error.WriteLine($"Relative mouse mode unavailable: {SDL.SDL_GetError()}");

            Console.WriteLine("Controls: WASD move, mouse look, Space jump, Escape quit");

            var character = new KinematicCharacter3D(DemoWorld3D.SpawnPosition, DemoWorld3D.CollisionBounds);
            var yaw = MathF.PI;
            var pitch = -0.34f;
            var moveForward = false;
            var moveBackward = false;
            var moveLeft = false;
            var moveRight = false;
            var jumpRequested = false;
            var autoPlayTime = 0f;
            var autoJumpSent = false;
            var previousTimestamp = Stopwatch.GetTimestamp();
            var simulationAccumulator = 0f;
            const float fixedStep = 1f / 120f;
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

                    if (type == SDL.SDL_EventType.SDL_EVENT_MOUSE_MOTION && interactive)
                    {
                        yaw += ev.motion.xrel * 0.0025f;
                        pitch = Math.Clamp(pitch - ev.motion.yrel * 0.0025f, -1.15f, 0.45f);
                    }

                    if (type is SDL.SDL_EventType.SDL_EVENT_KEY_DOWN or SDL.SDL_EventType.SDL_EVENT_KEY_UP)
                    {
                        var pressed = type == SDL.SDL_EventType.SDL_EVENT_KEY_DOWN;
                        switch (ev.key.scancode)
                        {
                            case SDL.SDL_Scancode.SDL_SCANCODE_W:
                                moveForward = pressed;
                                break;
                            case SDL.SDL_Scancode.SDL_SCANCODE_S:
                                moveBackward = pressed;
                                break;
                            case SDL.SDL_Scancode.SDL_SCANCODE_A:
                                moveLeft = pressed;
                                break;
                            case SDL.SDL_Scancode.SDL_SCANCODE_D:
                                moveRight = pressed;
                                break;
                            case SDL.SDL_Scancode.SDL_SCANCODE_SPACE when pressed && !ev.key.repeat:
                                jumpRequested = true;
                                break;
                            case SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE when pressed:
                                running = false;
                                break;
                        }
                    }
                }

                var currentTimestamp = Stopwatch.GetTimestamp();
                var frameTime = Math.Min(
                    (currentTimestamp - previousTimestamp) / (float) Stopwatch.Frequency,
                    0.1f);
                previousTimestamp = currentTimestamp;
                simulationAccumulator += frameTime;

                var forward = new Vector2(MathF.Sin(yaw), MathF.Cos(yaw));
                var right = new Vector2(forward.Y, -forward.X);
                var movement = forward * ((moveForward ? 1f : 0f) - (moveBackward ? 1f : 0f)) +
                               right * ((moveRight ? 1f : 0f) - (moveLeft ? 1f : 0f));

                while (simulationAccumulator >= fixedStep)
                {
                    var stepMovement = movement;
                    var stepJump = jumpRequested;
                    if (autoPlay)
                    {
                        autoPlayTime += fixedStep;
                        stepMovement = autoPlayTime switch
                        {
                            < 0.9f => forward,
                            < 1.5f => right,
                            _ => Vector2.Zero,
                        };
                        stepJump = !autoJumpSent && autoPlayTime >= 0.25f;
                        autoJumpSent |= stepJump;
                    }

                    character.Step(new CharacterInput3D(stepMovement, stepJump), fixedStep);
                    jumpRequested = false;
                    simulationAccumulator -= fixedStep;
                }

                SDL.SDL_GetWindowSizeInPixels(window, out var width, out var height);
                if (width <= 0 || height <= 0)
                    continue;

                GL.Viewport(0, 0, width, height);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                var horizontalLook = MathF.Cos(pitch);
                var lookDirection = Vector3.Normalize(new Vector3(
                    MathF.Sin(yaw) * horizontalLook,
                    MathF.Cos(yaw) * horizontalLook,
                    MathF.Sin(pitch)));
                var cameraTarget = character.Position + Vector3.UnitZ * 0.35f;
                var cameraDirection = -lookDirection;
                var cameraDistance = ResolveCameraDistance(cameraTarget, cameraDirection, 3.5f);
                var camera = cameraTarget + cameraDirection * cameraDistance;
                var view = Matrix4x4.CreateLookAt(camera, cameraTarget, Vector3.UnitZ);
                var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                    MathF.PI / 3f,
                    width / (float) height,
                    0.05f,
                    100f);

                GL.UseProgram(program);
                GL.BindVertexArray(vertexArray);
                DrawWorld(mvpLocation, tintLocation, view, projection, character.Position, yaw);

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

            Console.WriteLine(
                $"Player: {character.Position.X:F3}, {character.Position.Y:F3}, {character.Position.Z:F3}; " +
                $"grounded={character.IsGrounded}");

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

    private static bool HasArgument(string[] args, string expected)
    {
        foreach (var argument in args)
        {
            if (string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static float ResolveCameraDistance(Vector3 target, Vector3 direction, float desiredDistance)
    {
        var ray = new Ray3(target, direction);
        var distance = desiredDistance;

        foreach (var bounds in DemoWorld3D.CollisionBounds)
        {
            if (ray.TryIntersect(bounds, out var hitDistance) && hitDistance <= desiredDistance)
                distance = MathF.Min(distance, MathF.Max(0.4f, hitDistance - 0.08f));
        }

        return distance;
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

    private static unsafe void DrawWorld(
        int mvpLocation,
        int tintLocation,
        Matrix4x4 view,
        Matrix4x4 projection,
        Vector3 playerPosition,
        float playerYaw)
    {
        foreach (var worldObject in DemoWorld3D.Objects)
            DrawCube(
                worldObject.Transform,
                mvpLocation,
                tintLocation,
                new Vector3(0.25f, 0.55f, 0.75f),
                view,
                projection);

        var playerRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -playerYaw);
        DrawCube(new SpatialTransform(
            playerPosition - Vector3.UnitZ * 0.2f,
            playerRotation,
            new Vector3(0.62f, 0.42f, 1.2f)),
            mvpLocation,
            tintLocation,
            new Vector3(1f, 0.25f, 0.08f),
            view,
            projection);
        DrawCube(new SpatialTransform(
            playerPosition + Vector3.UnitZ * 0.56f,
            playerRotation,
            new Vector3(0.55f)),
            mvpLocation,
            tintLocation,
            new Vector3(1f, 0.58f, 0.22f),
            view,
            projection);
    }

    private static unsafe void DrawCube(
        SpatialTransform transform,
        int mvpLocation,
        int tintLocation,
        Vector3 tint,
        Matrix4x4 view,
        Matrix4x4 projection)
    {
        // System.Numerics stores row-vector matrices in row-major order. OpenGL reads the same
        // bytes as a column-major matrix, which already provides the required transpose.
        var mvp = transform.Matrix * view * projection;
        GL.UniformMatrix4(mvpLocation, 1, false, (float*) &mvp);
        GL.Uniform3(tintLocation, tint.X, tint.Y, tint.Z);
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
