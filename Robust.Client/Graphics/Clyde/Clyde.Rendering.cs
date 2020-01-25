using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.Components.Containers;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using StencilOp = OpenTK.Graphics.OpenGL.StencilOp;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private RenderHandle _renderHandle;

        /// <summary>
        ///     Are we current rendering screen space or world space? Some code works differently between the two.
        /// </summary>
        private CurrentSpace _currentSpace;

        private CurrentSpace _queuedSpace;

        private bool _lightingReady;

        /// <summary>
        ///     The current model matrix we would use.
        ///     Necessary since certain drawing operations mess with it still.
        /// </summary>
        private Matrix3 _currentModelMatrix = Matrix3.Identity;

        // The amount of quads we can render with ushort indices, leaving open 65536 for primitive restart.
        private const ushort MaxBatchQuads = (2 << 13) - 1; // In human terms: (2**16/4)-1 = 16383

        private readonly Vertex2D[] BatchVertexData = new Vertex2D[MaxBatchQuads * 4];

        // Need 5 indices per quad: 4 to draw the quad with triangle strips and another one as primitive restart.
        private readonly ushort[] BatchIndexData = new ushort[MaxBatchQuads * 5];

        private int BatchVertexIndex;
        private int BatchIndexIndex;

        // Contains information about the currently running batch.
        // So we can flush it if the next draw call is incompatible.
        private BatchMetaData? _batchMetaData;
        private ClydeHandle _queuedShader;

        private ProjViewMatrices _currentMatrices;

        private float _renderTime;

        private bool _isScissoring;
        private bool _isStencilling;

        private readonly RefList<RenderCommand> _queuedRenderCommands = new RefList<RenderCommand>();

        private readonly RefList<(SpriteComponent sprite, Matrix3 worldMatrix, Angle worldRotation)> _drawingSpriteList =
            new RefList<(SpriteComponent, Matrix3, Angle)>();

        private readonly RefList<(SpriteComponent sprite, Vector2 worldPosition, Angle worldRotation, Render3D.Sprite3DRenderMode renderMode)> _drawingSpriteList3D =
            new RefList<(SpriteComponent, Vector2, Angle, Render3D.Sprite3DRenderMode)>();

        public void Render()
        {
            var size = ScreenSize;
            if (size.X == 0 || size.Y == 0 || _isMinimized)
            {
                ClearFramebuffer(Color.Black);

                // We have to keep running swapbuffers here
                // or else the user's PC will turn into a heater!!
                SwapBuffers();
                return;
            }

            _debugStats.Reset();

            // Basic pre-render busywork.
            // Clear screen to black.
            ClearFramebuffer(Color.Black);

            // Update shared UBOs.
            _updateUniformConstants();

            SetSpaceFull(CurrentSpace.ScreenSpace);

            // Short path to render only the splash.
            if (_drawingSplash)
            {
                _drawSplash(_renderHandle);
                FlushRenderQueue();
                SwapBuffers();
                return;
            }

            void RenderOverlays(OverlaySpace space)
            {
                using (DebugGroup($"Overlays: {space}"))
                {
                    foreach (var overlay in _overlayManager.AllOverlays
                        .Where(o => o.Space == space)
                        .OrderBy(o => o.ZIndex))
                    {
                        overlay.ClydeRender(_renderHandle);
                    }

                    FlushRenderQueue();
                }
            }

            RenderOverlays(OverlaySpace.ScreenSpaceBelowWorld);

            SetSpaceFull(CurrentSpace.WorldSpace);

            // Calculate world-space AABB for camera, to cull off-screen things.
            var eye = _eyeManager.CurrentEye;
            var worldBounds = Box2.CenteredAround(eye.Position.Position,
                _framebufferSize / (float) EyeManager.PIXELSPERMETER * eye.Zoom);

            using (DebugGroup("Lights"))
            {
                _drawLights(worldBounds);
            }

            if (eye.Is3D)
            {
                // Start 3D rendering. Enable depth buffer.
                GL.Enable(EnableCap.DepthTest);
                GL.DepthMask(true);
                GL.DepthFunc(DepthFunction.Less);
            }

            using (DebugGroup("Grids"))
            {
                _drawGrids(worldBounds);
            }

            using (DebugGroup("Entities"))
            {
                DrawEntities(worldBounds);
            }

            // Any 3D rendering should be done.
            GL.Disable(EnableCap.DepthTest);

            RenderOverlays(OverlaySpace.WorldSpace);

            _lightingReady = false;

            SetSpaceFull(CurrentSpace.ScreenSpace);

            RenderOverlays(OverlaySpace.ScreenSpace);

            using (DebugGroup("UI"))
            {
                _userInterfaceManager.Render(_renderHandle);
                FlushRenderQueue();
            }

            // And finally, swap those buffers!
            SwapBuffers();
        }

        private void DrawEntities(Box2 worldBounds)
        {
            if (_eyeManager.CurrentMap == MapId.Nullspace || !_mapManager.HasMapEntity(_eyeManager.CurrentMap))
            {
                return;
            }

            // So we could calculate the correct size of the entities based on the contents of their sprite...
            // Or we can just assume that no entity is larger than 10x10 and get a stupid easy check.
            // TODO: Make this check more accurate.
            var widerBounds = worldBounds.Enlarged(5);

            var mapEntity = _mapManager.GetMapEntity(_eyeManager.CurrentMap);

            var identity = Matrix3.Identity;

            bool is3d = _eyeManager.CurrentEye.Is3D;

            if (is3d)
            {
                ProcessSpriteEntities3D(mapEntity, ref identity, Angle.Zero, widerBounds, _drawingSpriteList3D);

                var clyde3d = GetRender3D();

                for (var i = 0; i < _drawingSpriteList3D.Count; i++)
                {
                    ref var entry = ref _drawingSpriteList3D[i];

                    entry.sprite.Render3D(clyde3d, entry.worldPosition, entry.worldRotation, entry.renderMode);
                }
                _drawingSpriteList3D.Clear();
            } else
            {
                ProcessSpriteEntities(mapEntity, ref identity, Angle.Zero, widerBounds, _drawingSpriteList);

                // We use a separate list for indexing so that the sort is faster.
                var indexList = ArrayPool<int>.Shared.Rent(_drawingSpriteList.Count);

                for (var i = 0; i < _drawingSpriteList.Count; i++)
                {
                    indexList[i] = i;
                }

                Array.Sort(indexList, 0, _drawingSpriteList.Count, new SpriteDrawingOrderComparer(_drawingSpriteList));

                for (var i = 0; i < _drawingSpriteList.Count; i++)
                {
                    ref var entry = ref _drawingSpriteList[indexList[i]];
                    Vector2i roundedPos = default;
                    if (entry.sprite.PostShader != null)
                    {
                        _renderHandle.UseRenderTarget(EntityPostRenderTarget);
                        _renderHandle.Clear(new Color());
                        // Calculate viewport so that the entity thinks it's drawing to the same position,
                        // which is necessary for light application,
                        // but it's ACTUALLY drawing into the center of the render target.
                        var spritePos = entry.sprite.Owner.Transform.WorldPosition;
                        var screenPos = _eyeManager.WorldToScreen(spritePos);
                        var (roundedX, roundedY) = roundedPos = (Vector2i) screenPos;
                        var flippedPos = new Vector2i(roundedX, ScreenSize.Y - roundedY);
                        flippedPos -= EntityPostRenderTarget.Size / 2;
                        _renderHandle.Viewport(Box2i.FromDimensions(-flippedPos, ScreenSize));
                    }

                    entry.sprite.Render(_renderHandle.DrawingHandleWorld, entry.worldMatrix, entry.worldRotation);

                    if (entry.sprite.PostShader != null)
                    {
                        _renderHandle.UseRenderTarget(null);
                        _renderHandle.Viewport(Box2i.FromDimensions(Vector2i.Zero, ScreenSize));

                        _renderHandle.UseShader(entry.sprite.PostShader);
                        _renderHandle.SetSpace(CurrentSpace.ScreenSpace);
                        _renderHandle.SetModelTransform(Matrix3.Identity);

                        var rounded = roundedPos - EntityPostRenderTarget.Size / 2;

                        var box = UIBox2i.FromDimensions(rounded, EntityPostRenderTarget.Size);

                        _renderHandle.DrawTexture(EntityPostRenderTarget.Texture, box.BottomLeft,
                            box.TopRight, Color.White, null, 0);

                        _renderHandle.SetSpace(CurrentSpace.WorldSpace);
                        _renderHandle.UseShader(null);
                    }
                }
                _drawingSpriteList.Clear();
            }


            FlushRenderQueue();
        }

        private void ProcessSpriteEntities(IEntity entity, ref Matrix3 parentTransform, Angle parentRotation,
            Box2 worldBounds, RefList<(SpriteComponent, Matrix3, Angle)> list)
        {
            entity.TryGetComponent(out ContainerManagerComponent containerManager);

            var localMatrix = entity.Transform.GetLocalMatrix();
            Matrix3.Multiply(ref parentTransform, ref localMatrix, out var matrix);
            var rotation = parentRotation + entity.Transform.LocalRotation;

            foreach (var child in entity.Transform.ChildEntityUids)
            {
                var childEntity = _entityManager.GetEntity(child);

                if (containerManager != null && containerManager.TryGetContainer(childEntity, out var container) &&
                    !container.ShowContents)
                {
                    continue;
                }

                var childTransform = childEntity.Transform;

                var worldPosition = Matrix3.Transform(matrix, childTransform.LocalPosition);

                if (worldBounds.Contains(worldPosition))
                {
                    if (childEntity.TryGetComponent(out SpriteComponent sprite) && sprite.Visible)
                    {
                        ref var entry = ref list.AllocAdd();

                        var childLocalMatrix = childTransform.GetLocalMatrix();
                        Matrix3.Multiply(ref matrix, ref childLocalMatrix, out entry.Item2);
                        var childWorldRotation = rotation + childTransform.LocalRotation;

                        entry.Item1 = sprite;
                        entry.Item3 = childWorldRotation;

                        /*if (include3DRenderMode)
                        {
                            entry.Item4 = Render3D.InferRenderMode(childEntity);
                        }*/
                    }
                }

                if (childTransform.ChildCount > 0)
                {
                    ProcessSpriteEntities(childEntity, ref matrix, rotation, worldBounds, list);
                }
            }
        }

        private void ProcessSpriteEntities3D(IEntity entity, ref Matrix3 parentTransform, Angle parentRotation,
            Box2 worldBounds, RefList<(SpriteComponent, Vector2, Angle, Render3D.Sprite3DRenderMode)> list)
        {
            entity.TryGetComponent(out ContainerManagerComponent containerManager);

            var localMatrix = entity.Transform.GetLocalMatrix();
            Matrix3.Multiply(ref parentTransform, ref localMatrix, out var matrix);
            var rotation = parentRotation + entity.Transform.LocalRotation;

            foreach (var child in entity.Transform.ChildEntityUids)
            {
                var childEntity = _entityManager.GetEntity(child);

                if (containerManager != null && containerManager.TryGetContainer(childEntity, out var container) &&
                    !container.ShowContents)
                {
                    continue;
                }

                var childTransform = childEntity.Transform;

                var worldPosition = Matrix3.Transform(matrix, childTransform.LocalPosition);

                if (worldBounds.Contains(worldPosition))
                {
                    if (childEntity.TryGetComponent(out SpriteComponent sprite) && sprite.Visible)
                    {
                        ref var entry = ref list.AllocAdd();

                        var childWorldRotation = rotation + childTransform.LocalRotation;

                        entry.Item1 = sprite;
                        entry.Item2 = worldPosition;
                        entry.Item3 = childWorldRotation;
                        entry.Item4 = Render3D.InferRenderMode(childEntity);
                    }
                }

                if (childTransform.ChildCount > 0)
                {
                    ProcessSpriteEntities3D(childEntity, ref matrix, rotation, worldBounds, list);
                }
            }
        }

        private void _drawSplash(IRenderHandle handle)
        {
            var texture = _resourceCache.GetResource<TextureResource>("/Textures/Logo/logo.png").Texture;

            handle.DrawingHandleScreen.DrawTexture(texture, (ScreenSize - texture.Size) / 2);
        }

        /// <summary>
        ///     Updates uniform constants shared to all shaders, such as time and pixel size.
        /// </summary>
        private void _updateUniformConstants()
        {
            var constants = new UniformConstants(Vector2.One / ScreenSize, _renderTime);
            UniformConstantsUBO.Reallocate(constants);
        }

        private ProjViewMatrices _screenMatrices()
        {
            var viewMatrixScreen = Matrix3.Identity;

            // Screen projection matrix.
            var projMatrixScreen = Matrix3.Identity;
            projMatrixScreen.R0C0 = 2f / ScreenSize.X;
            projMatrixScreen.R1C1 = -2f / ScreenSize.Y;
            projMatrixScreen.R0C2 = -1;
            projMatrixScreen.R1C2 = 1;

            return new ProjViewMatrices(projMatrixScreen, viewMatrixScreen);
        }

        private ProjViewMatrices _worldMatrices()
        {
            var eye = _eyeManager.CurrentEye;


            if (eye.Is3D)
            {
                var (projMatrixWorld, viewMatrixWorld) = GetRender3D().GetProjViewMatrices3D(eye.Position.Position);
                return new ProjViewMatrices(projMatrixWorld, viewMatrixWorld);
            } else
            {
                var toScreen = _eyeManager.WorldToScreen(eye.Position.Position);
                // Round camera position to a screen pixel to avoid weird issues on odd screen sizes.
                toScreen = ((float) Math.Floor(toScreen.X), (float) Math.Floor(toScreen.Y));
                var cameraWorldAdjusted = _eyeManager.ScreenToMap(toScreen);

                var viewMatrixWorld = Matrix4.CreateTranslation(new Vector3(-cameraWorldAdjusted.X, -cameraWorldAdjusted.Y, 0));
                viewMatrixWorld *= Matrix4.Scale(1/ eye.Zoom.X, 1/ eye.Zoom.Y, 1);

                var projMatrixWorld = Matrix4.Identity;
                projMatrixWorld.M11 = EyeManager.PIXELSPERMETER * 2f / ScreenSize.X;
                projMatrixWorld.M22 = EyeManager.PIXELSPERMETER * 2f / ScreenSize.Y;

                return new ProjViewMatrices(projMatrixWorld, viewMatrixWorld);
            }
        }

        private void _drawLights(Box2 worldBounds)
        {
            if (!_lightManager.Enabled || _eyeManager.CurrentEye.Is3D)
            {
                return;
            }

            var map = _eyeManager.CurrentMap;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, LightRenderTarget.ObjectHandle.Handle);
            var converted = Color.FromSrgb(new Color(0.1f, 0.1f, 0.1f));
            GL.ClearColor(converted.R, converted.G, converted.B, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var (lightW, lightH) = _lightMapSize();
            GL.Viewport(0, 0, lightW, lightH);

            _lightShader.Use();

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);

            var lastRange = float.NaN;
            var lastPower = float.NaN;
            var lastColor = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
            Texture lastMask = null;

            foreach (var component in _componentManager.GetAllComponents<PointLightComponent>())
            {
                if (!component.Enabled || component.Owner.Transform.MapID != map)
                {
                    continue;
                }

                var transform = component.Owner.Transform;
                var lightPos = transform.WorldMatrix.Transform(component.Offset);

                var lightBounds = Box2.CenteredAround(lightPos, Vector2.One * component.Radius * 2);

                if (!lightBounds.Intersects(worldBounds))
                {
                    continue;
                }

                Texture mask = null;
                var rotation = Angle.Zero;
                if (component.Mask != null)
                {
                    mask = component.Mask;
                    rotation = component.Rotation;

                    if (component.MaskAutoRotate)
                    {
                        rotation += transform.WorldRotation;
                    }
                }

                var maskTexture = mask ?? Texture.White;
                if (lastMask != maskTexture)
                {
                    var maskHandle = _loadedTextures[((ClydeTexture) maskTexture).TextureId].OpenGLObject;
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, maskHandle.Handle);
                    lastMask = maskTexture;
                    _lightShader.SetUniformTexture("lightMask", TextureUnit.Texture0);
                }

                if (!FloatMath.CloseTo(lastRange, component.Radius))
                {
                    lastRange = component.Radius;
                    _lightShader.SetUniform("lightRange", lastRange);
                }

                if (!FloatMath.CloseTo(lastPower, component.Energy))
                {
                    lastPower = component.Energy;
                    _lightShader.SetUniform("lightPower", lastPower);
                }

                if (lastColor != component.Color)
                {
                    lastColor = component.Color;
                    _lightShader.SetUniform("lightColor", lastColor);
                }

                _lightShader.SetUniform("lightCenter", lightPos);

                var offset = new Vector2(component.Radius, component.Radius);

                Matrix3 matrix;
                if (mask == null)
                {
                    matrix = Matrix3.Identity;
                }
                else
                {
                    // Only apply rotation if a mask is said, because else it doesn't matter.
                    matrix = Matrix3.CreateRotation(rotation);
                }

                (matrix.R0C2, matrix.R1C2) = lightPos;

                _drawQuad(-offset, offset, ref matrix, _lightShader);
            }

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, ScreenSize.X, ScreenSize.Y);

            _lightingReady = true;
        }

        private void _setProjViewMatrices(in ProjViewMatrices matrices)
        {
            _currentMatrices = matrices;
            ProjViewUBO.Reallocate(matrices);
        }

        private void ProcessRenderCommands()
        {
            foreach (ref var command in _queuedRenderCommands)
            {
                switch (command.Type)
                {
                    case RenderCommandType.DrawBatch:
                        DrawCommandBatch(ref command.DrawBatch);
                        break;

                    case RenderCommandType.Scissor:
                        var oldIsScissoring = _isScissoring;
                        _isScissoring = command.Scissor.EnableScissor;
                        if (_isScissoring)
                        {
                            if (!oldIsScissoring)
                            {
                                GL.Enable(EnableCap.ScissorTest);
                            }

                            ref var s = ref command.Scissor.Scissor;
                            // Don't forget to flip it, these coordinates have bottom left as origin.
                            GL.Scissor(s.Left, _framebufferSize.Y - s.Bottom, s.Width, s.Height);
                        }
                        else if (oldIsScissoring)
                        {
                            GL.Disable(EnableCap.ScissorTest);
                        }

                        break;

                    case RenderCommandType.ViewMatrix:
                        var matrices = new ProjViewMatrices(_currentMatrices, command.ViewMatrix.Matrix);
                        _setProjViewMatrices(matrices);
                        break;

                    case RenderCommandType.ResetViewMatrix:
                        _setSpace(_currentSpace);
                        break;

                    case RenderCommandType.SwitchSpace:
                        _setSpace(command.SwitchSpace.NewSpace);
                        break;

                    case RenderCommandType.RenderTarget:
                        if (command.RenderTarget.RenderTarget.Value == 0)
                        {
                            _popDebugGroupMaybe();
                            // Bind window framebuffer.
                            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                            var (w, h) = ScreenSize;
                            GL.Viewport(0, 0, w, h);
                        }
                        else
                        {
                            _pushDebugGroupMaybe("fb");
                            var renderTarget = _renderTargets[command.RenderTarget.RenderTarget];

                            GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTarget.ObjectHandle.Handle);
                            var (w, h) = renderTarget.Size;
                            GL.Viewport(0, 0, w, h);
                        }

                        break;

                    case RenderCommandType.Viewport:
                        ref var vp = ref command.Viewport.Viewport;
                        GL.Viewport(vp.Left, vp.Bottom, vp.Width, vp.Height);
                        break;

                    case RenderCommandType.Clear:
                        ref var color = ref command.Clear.Color;
                        GL.ClearColor(color.R, color.G, color.B, color.A);
                        GL.Clear(ClearBufferMask.ColorBufferBit);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void DrawCommandBatch(ref RenderCommandDrawBatch command)
        {
            var loadedTexture = _loadedTextures[command.TextureId];

            GL.BindVertexArray(BatchVAO.Handle);

            var (program, loaded) = ActivateShaderInstance(command.ShaderInstance);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, loadedTexture.OpenGLObject.Handle);

            GL.ActiveTexture(TextureUnit.Texture1);
            if (_lightingReady && loaded.HasLighting)
            {
                var lightTexture = _loadedTextures[LightRenderTarget.Texture.TextureId].OpenGLObject;
                GL.BindTexture(TextureTarget.Texture2D, lightTexture.Handle);
            }
            else
            {
                var white = _loadedTextures[_stockTextureWhite.TextureId].OpenGLObject;
                GL.BindTexture(TextureTarget.Texture2D, white.Handle);
            }

            program.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
            program.SetUniformTextureMaybe(UniILightTexture, TextureUnit.Texture1);

            // Model matrix becomes identity since it's built into the batch mesh.
            program.SetUniformMaybe(UniIModelMatrix, Matrix3.Identity);
            // Reset ModUV to ensure it's identity and doesn't touch anything.
            program.SetUniformMaybe(UniIModUV, new Vector4(0, 0, 1, 1));

            program.SetUniformMaybe(UniIModulate, command.Modulate);
            program.SetUniformMaybe(UniITexturePixelSize, Vector2.One / loadedTexture.Size);


            var primitiveType = MapPrimitiveType(command.PrimitiveType);
            if (command.Indexed)
            {
                GL.DrawElements(primitiveType, command.Count, DrawElementsType.UnsignedShort,
                    command.StartIndex * sizeof(ushort));
            }
            else
            {
                GL.DrawArrays(primitiveType, command.StartIndex, command.Count);
            }

            _debugStats.LastGLDrawCalls += 1;
        }

        private PrimitiveType MapPrimitiveType(BatchPrimitiveType type)
        {
            return type switch
            {
                BatchPrimitiveType.Triangles => PrimitiveType.Triangles,
                BatchPrimitiveType.TrianglesFan => PrimitiveType.TriangleFan,
                BatchPrimitiveType.Line => PrimitiveType.Lines,
                _ => PrimitiveType.Triangles
            };
        }

        private void _drawQuad(Vector2 a, Vector2 b, ref Matrix3 modelMatrix, ShaderProgram program)
        {
            GL.BindVertexArray(QuadVAO.Handle);
            var rectTransform = Matrix3.Identity;
            (rectTransform.R0C0, rectTransform.R1C1) = b - a;
            (rectTransform.R0C2, rectTransform.R1C2) = a;
            rectTransform.Multiply(modelMatrix);
            program.SetUniformMaybe(UniIModelMatrix, rectTransform);

            _debugStats.LastGLDrawCalls += 1;
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        /// <summary>
        ///     Flush the render handle, processing and re-pooling all the command lists.
        /// </summary>
        private void FlushRenderQueue()
        {
            // Finish any batches that may have been WiP.
            BreakBatch();

            if (BatchVertexIndex != 0)
            {
                BatchVBO.Reallocate(new Span<Vertex2D>(BatchVertexData, 0, BatchVertexIndex));
                BatchVertexIndex = 0;

                if (BatchIndexIndex != 0)
                {
                    BatchEBO.Reallocate(new Span<ushort>(BatchIndexData, 0, BatchIndexIndex));
                }

                BatchIndexIndex = 0;
            }

            ProcessRenderCommands();
            _queuedRenderCommands.Clear();

            // Reset renderer state.
            _currentModelMatrix = Matrix3.Identity;
            _queuedShader = _defaultShader.Handle;
            _disableScissor();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _disableScissor()
        {
            if (_isScissoring)
            {
                GL.Disable(EnableCap.ScissorTest);
            }

            _isScissoring = false;
        }

        private void _setSpace(CurrentSpace newSpace)
        {
            _currentSpace = newSpace;

            switch (newSpace)
            {
                case CurrentSpace.ScreenSpace:
                    _setProjViewMatrices(_screenMatrices());
                    break;
                case CurrentSpace.WorldSpace:
                    _setProjViewMatrices(_worldMatrices());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newSpace), newSpace, null);
            }
        }

        private void SetSpaceFull(CurrentSpace newSpace)
        {
            _setSpace(newSpace);

            SetQueuedSpace(newSpace);
        }

        private void SetQueuedSpace(CurrentSpace newSpace)
        {
            _queuedSpace = newSpace;
        }

        private void ClearFramebuffer(Color color)
        {
            GL.ClearColor(color.ConvertOpenTK());
            GL.ClearStencil(0);
            GL.ClearDepth(1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit | ClearBufferMask.DepthBufferBit);
        }

        private (ShaderProgram, LoadedShader) ActivateShaderInstance(ClydeHandle handle)
        {
            var instance = _shaderInstances[handle];
            var shader = _loadedShaders[instance.ShaderHandle];
            var program = shader.Program;

            program.Use();

            // Assign shader parameters to uniform since they may be dirty.
            foreach (var (name, value) in instance.Parameters)
            {
                if (!program.HasUniform(name))
                {
                    // Can happen if the GLSL compiler removes uniforms due to them being unused.
                    // Safe to just ignore them then I'd say.
                    continue;
                }

                switch (value)
                {
                    case float f:
                        program.SetUniform(name, f);
                        break;
                    case Vector2 vector2:
                        program.SetUniform(name, vector2);
                        break;
                    case Vector3 vector3:
                        program.SetUniform(name, vector3);
                        break;
                    case Vector4 vector4:
                        program.SetUniform(name, vector4);
                        break;
                    case Color color:
                        program.SetUniform(name, color);
                        break;
                    case int i:
                        program.SetUniform(name, i);
                        break;
                    case Vector2i vector2I:
                        program.SetUniform(name, vector2I);
                        break;
                    case bool b:
                        program.SetUniform(name, b ? 1 : 0);
                        break;
                    case Matrix3 matrix3:
                        program.SetUniform(name, matrix3);
                        break;
                    case Matrix4 matrix4:
                        program.SetUniform(name, matrix4);
                        break;
                    default:
                        throw new InvalidOperationException($"Unable to handle shader parameter {name}: {value}");
                }
            }

            // Handle stencil parameters.

            if (instance.Stencil.Enabled)
            {
                if (!_isStencilling)
                {
                    GL.Enable(EnableCap.StencilTest);
                    _isStencilling = true;
                }

                GL.StencilMask(instance.Stencil.WriteMask);
                GL.StencilFunc(ToGLStencilFunc(instance.Stencil.Func), instance.Stencil.Ref, instance.Stencil.ReadMask);
                GL.StencilOp(StencilOp.Keep, StencilOp.Keep, ToGLStencilOp(instance.Stencil.Op));
            }
            else if (_isStencilling)
            {
                GL.Disable(EnableCap.StencilTest);
                _isStencilling = false;
            }

            return (program, shader);
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static bool StrictColorEquality(in Color a, in Color b)
        {
            return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
        }

        private ref RenderCommand AllocRenderCommand(RenderCommandType type)
        {
            ref var command = ref _queuedRenderCommands.AllocAdd();
            command.Type = type;
            return ref command;
        }

        private void DrawSetModelTransform(in Matrix3 matrix)
        {
            _currentModelMatrix = matrix;
        }

        private void DrawSetViewTransform(in Matrix3 matrix)
        {
            ref var command = ref AllocRenderCommand(RenderCommandType.ViewMatrix);

            command.ViewMatrix.Matrix = matrix;
        }

        private void DrawResetViewTransform()
        {
            AllocRenderCommand(RenderCommandType.ResetViewMatrix);
        }

        private void DrawTexture(ClydeHandle texture, Vector2 a, Vector2 b, Color modulate, UIBox2? subRegion,
            Angle angle)
        {
            EnsureBatchState(texture, modulate, true, BatchPrimitiveType.TrianglesFan, _queuedShader);

            var loadedTexture = _loadedTextures[texture];

            UIBox2 sr;
            if (subRegion.HasValue)
            {
                var (w, h) = loadedTexture.Size;
                var csr = subRegion.Value;
                if (_queuedSpace == CurrentSpace.WorldSpace)
                {
                    sr = new UIBox2(csr.Left / w, csr.Top / h, csr.Right / w, csr.Bottom / h);
                }
                else
                {
                    sr = new UIBox2(csr.Left / w, csr.Bottom / h, csr.Right / w, csr.Top / h);
                }
            }
            else
            {
                if (_queuedSpace == CurrentSpace.WorldSpace)
                {
                    sr = new UIBox2(0, 0, 1, 1);
                }
                else
                {
                    sr = new UIBox2(0, 1, 1, 0);
                }
            }

            Vector2 bl;
            Vector2 br;
            Vector2 tr;
            Vector2 tl;
            if (angle == Angle.Zero)
            {
                bl = _currentModelMatrix.Transform(a);
                br = _currentModelMatrix.Transform(new Vector2(b.X, a.Y));
                tr = _currentModelMatrix.Transform(b);
                tl = _currentModelMatrix.Transform(new Vector2(a.X, b.Y));
            }
            else
            {
                bl = _currentModelMatrix.Transform(angle.RotateVec(a));
                br = _currentModelMatrix.Transform(angle.RotateVec(new Vector2(b.X, a.Y)));
                tr = _currentModelMatrix.Transform(angle.RotateVec(b));
                tl = _currentModelMatrix.Transform(angle.RotateVec(new Vector2(a.X, b.Y)));
            }

            // TODO: split batch if necessary.
            var vIdx = BatchVertexIndex;
            BatchVertexData[vIdx + 0] = new Vertex2D(bl, sr.BottomLeft);
            BatchVertexData[vIdx + 1] = new Vertex2D(br, sr.BottomRight);
            BatchVertexData[vIdx + 2] = new Vertex2D(tr, sr.TopRight);
            BatchVertexData[vIdx + 3] = new Vertex2D(tl, sr.TopLeft);
            BatchVertexIndex += 4;
            var nIdx = BatchIndexIndex;
            var tIdx = (ushort) vIdx;
            BatchIndexData[nIdx + 0] = tIdx;
            BatchIndexData[nIdx + 1] = (ushort) (tIdx + 1);
            BatchIndexData[nIdx + 2] = (ushort) (tIdx + 2);
            BatchIndexData[nIdx + 3] = (ushort) (tIdx + 3);
            BatchIndexData[nIdx + 4] = ushort.MaxValue;
            BatchIndexIndex += 5;

            _debugStats.LastClydeDrawCalls += 1;
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            EnsureBatchState(_stockTextureWhite.TextureId, color, false, BatchPrimitiveType.Line, _queuedShader);

            a = _currentModelMatrix.Transform(a);
            b = _currentModelMatrix.Transform(b);

            // TODO: split batch if necessary.
            var vIdx = BatchVertexIndex;
            BatchVertexData[vIdx + 0] = new Vertex2D(a, Vector2.Zero);
            BatchVertexData[vIdx + 1] = new Vertex2D(b, Vector2.Zero);
            BatchVertexIndex += 2;

            _debugStats.LastClydeDrawCalls += 1;
        }

        private void DrawSetScissor(UIBox2i? scissorBox)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.Scissor);

            command.Scissor.EnableScissor = scissorBox.HasValue;
            if (scissorBox.HasValue)
            {
                command.Scissor.Scissor = scissorBox.Value;
            }
        }

        private void DrawSwitchSpace(CurrentSpace space)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.SwitchSpace);

            command.SwitchSpace.NewSpace = space;

            SetQueuedSpace(space);
        }

        private void DrawUseShader(ClydeHandle handle)
        {
            _queuedShader = handle;
        }

        private void DrawClear(Color color)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.Clear);

            command.Clear.Color = color;
        }

        private void DrawViewport(Box2i viewport)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.Viewport);

            command.Viewport.Viewport = viewport;
        }

        private void DrawRenderTarget(ClydeHandle handle)
        {
            BreakBatch();

            ref var command = ref AllocRenderCommand(RenderCommandType.RenderTarget);

            command.RenderTarget.RenderTarget = handle;
        }

        /// <summary>
        ///     Ensures that batching metadata matches the current batch.
        ///     If not, the current batch is finished and a new one is started.
        /// </summary>
        private void EnsureBatchState(ClydeHandle textureId, Color color, bool indexed,
            BatchPrimitiveType primitiveType, ClydeHandle shaderInstance)
        {
            if (_batchMetaData.HasValue)
            {
                var metaData = _batchMetaData.Value;
                if (metaData.TextureId == textureId &&
                    StrictColorEquality(metaData.Color, color) &&
                    indexed == metaData.Indexed &&
                    metaData.PrimitiveType == primitiveType &&
                    metaData.ShaderInstance == shaderInstance)
                {
                    // Data matches, don't have to do anything.
                    return;
                }

                // Data does not match. Finish batch...
                FinishBatch();
            }

            // ... and start another.
            _batchMetaData = new BatchMetaData(textureId, color, indexed, primitiveType,
                indexed ? BatchIndexIndex : BatchVertexIndex, shaderInstance);
        }

        private void FinishBatch()
        {
            if (!_batchMetaData.HasValue)
            {
                return;
            }

            var metaData = _batchMetaData.Value;

            var indexed = metaData.Indexed;
            var currentIndex = indexed ? BatchIndexIndex : BatchVertexIndex;

            ref var command = ref AllocRenderCommand(RenderCommandType.DrawBatch);

            command.DrawBatch.Indexed = indexed;
            command.DrawBatch.StartIndex = metaData.StartIndex;
            command.DrawBatch.PrimitiveType = metaData.PrimitiveType;
            command.DrawBatch.Modulate = metaData.Color;
            command.DrawBatch.TextureId = metaData.TextureId;
            command.DrawBatch.ShaderInstance = metaData.ShaderInstance;

            command.DrawBatch.Count = currentIndex - metaData.StartIndex;

            _debugStats.LastBatches += 1;
        }

        private static StencilOp ToGLStencilOp(Shaders.StencilOp op)
        {
            return op switch
            {
                Shaders.StencilOp.Keep => StencilOp.Keep,
                Shaders.StencilOp.Zero => StencilOp.Zero,
                Shaders.StencilOp.Replace => StencilOp.Replace,
                Shaders.StencilOp.IncrementClamp => StencilOp.Incr,
                Shaders.StencilOp.IncrementWrap => StencilOp.IncrWrap,
                Shaders.StencilOp.DecrementClamp => StencilOp.Decr,
                Shaders.StencilOp.DecrementWrap => StencilOp.DecrWrap,
                Shaders.StencilOp.Invert => StencilOp.Invert,
                _ => throw new NotSupportedException()
            };
        }

        private static StencilFunction ToGLStencilFunc(StencilFunc op)
        {
            return op switch
            {
                StencilFunc.Never => StencilFunction.Never,
                StencilFunc.Always => StencilFunction.Always,
                StencilFunc.Less => StencilFunction.Less,
                StencilFunc.LessOrEqual => StencilFunction.Lequal,
                StencilFunc.Greater => StencilFunction.Greater,
                StencilFunc.GreaterOrEqual => StencilFunction.Gequal,
                StencilFunc.NotEqual => StencilFunction.Notequal,
                StencilFunc.Equal => StencilFunction.Equal,
                _ => throw new NotSupportedException()
            };
        }

        /// <summary>
        ///     Renderer state that cannot be changed mid-batch has been modified and a new batch will have to be started.
        /// </summary>
        private void BreakBatch()
        {
            FinishBatch();

            _batchMetaData = null;
        }

        // Use a tagged union to store all render commands.
        // This significantly improves performance vs doing sum types via inheritance.
        // Also means I don't have to declare a pool for every command type.
        [StructLayout(LayoutKind.Explicit)]
        private struct RenderCommand
        {
            [FieldOffset(0)] public RenderCommandType Type;

            [FieldOffset(4)] public RenderCommandDrawBatch DrawBatch;
            [FieldOffset(4)] public RenderCommandViewMatrix ViewMatrix;
            [FieldOffset(4)] public RenderCommandScissor Scissor;
            [FieldOffset(4)] public RenderCommandSwitchSpace SwitchSpace;
            [FieldOffset(4)] public RenderCommandRenderTarget RenderTarget;
            [FieldOffset(4)] public RenderCommandViewport Viewport;
            [FieldOffset(4)] public RenderCommandClear Clear;
        }

        private struct RenderCommandDrawBatch
        {
            public ClydeHandle TextureId;
            public ClydeHandle ShaderInstance;
            public Color Modulate;

            public int StartIndex;
            public int Count;
            public bool Indexed;
            public BatchPrimitiveType PrimitiveType;
        }

        private struct RenderCommandViewMatrix
        {
            public Matrix3 Matrix;
        }

        private struct RenderCommandScissor
        {
            public bool EnableScissor;
            public UIBox2i Scissor;
        }

        private struct RenderCommandSwitchSpace
        {
            public CurrentSpace NewSpace;
        }

        private struct RenderCommandRenderTarget
        {
            public ClydeHandle RenderTarget;
        }

        private struct RenderCommandViewport
        {
            public Box2i Viewport;
        }

        private struct RenderCommandClear
        {
            public Color Color;
        }

        private enum RenderCommandType
        {
            DrawBatch,

            ViewMatrix,
            ResetViewMatrix,
            SwitchSpace,
            Viewport,

            Scissor,
            RenderTarget,

            Clear
        }

        private enum CurrentSpace
        {
            ScreenSpace = 0,
            WorldSpace = 1,
        }

        private struct PopDebugGroup : IDisposable
        {
            private readonly Clyde _clyde;

            public PopDebugGroup(Clyde clyde)
            {
                _clyde = clyde;
            }

            public void Dispose()
            {
                _clyde._popDebugGroupMaybe();
            }
        }

        private readonly struct BatchMetaData
        {
            public readonly ClydeHandle TextureId;
            public readonly Color Color;
            public readonly bool Indexed;
            public readonly BatchPrimitiveType PrimitiveType;
            public readonly int StartIndex;
            public readonly ClydeHandle ShaderInstance;

            public BatchMetaData(ClydeHandle textureId, Color color, bool indexed, BatchPrimitiveType primitiveType,
                int startIndex, ClydeHandle shaderInstance)
            {
                TextureId = textureId;
                Color = color;
                Indexed = indexed;
                PrimitiveType = primitiveType;
                StartIndex = startIndex;
                ShaderInstance = shaderInstance;
            }
        }

        private enum BatchPrimitiveType
        {
            Triangles,
            TrianglesFan,
            Line
        }

        private sealed class SpriteDrawingOrderComparer : IComparer<int>
        {
            private readonly RefList<(SpriteComponent, Matrix3, Angle)> _drawList;

            public SpriteDrawingOrderComparer(RefList<(SpriteComponent, Matrix3, Angle)> drawList)
            {
                _drawList = drawList;
            }

            public int Compare(int x, int y)
            {
                var a = _drawList[x].Item1;
                var b = _drawList[y].Item1;

                var cmp = ((int) a.DrawDepth).CompareTo((int) b.DrawDepth);
                if (cmp != 0)
                {
                    return cmp;
                }

                cmp = a.RenderOrder.CompareTo(b.RenderOrder);

                if (cmp != 0)
                {
                    return cmp;
                }

                return a.Owner.Uid.CompareTo(b.Owner.Uid);
            }
        }
    }
}
