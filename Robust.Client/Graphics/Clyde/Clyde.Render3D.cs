using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using OpenTK.Graphics.OpenGL;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{

    internal partial class Clyde
    {

        public class Render3D
        {
            private Clyde clyde;
            private ClydeShaderInstance modelShader;
            private OGLHandle VAO3D = new OGLHandle(GL.GenVertexArray());
            private Buffer VBO3D;

            public Render3D(Clyde clyde)
            {
                this.clyde = clyde;

                // Load shader
                var defaultModelShader = clyde._resourceCache
                    .GetResource<ShaderSourceResource>("/Shaders/Internal/default-model.swsl").ClydeHandle;

                this.modelShader = (ClydeShaderInstance)clyde.InstanceShader(defaultModelShader);

                // Set up test mesh
                int vert_size = sizeof(float) * 5;

                GL.BindVertexArray(VAO3D.Handle);
                clyde._objectLabelMaybe(ObjectLabelIdentifier.VertexArray, VAO3D, "VAO3D");
                // Vertex Coords
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vert_size, 0);
                GL.EnableVertexAttribArray(0);
                // Texture Coords.
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, vert_size, 2 * sizeof(float));
                GL.EnableVertexAttribArray(1);

                var test = new float[] {
                    0.0f, 0.0f, 0.0f,      0.0f, 0.0f,
                    100.0f, 0.0f, 0.0f,      0.0f, 0.0f,
                    0.0f, 100.0f, 0.0f,      0.0f, 0.0f,

                    0.0f, 0.0f, 0.0f,      0.0f, 0.0f,
                    0.0f, 100.0f, 0.0f,      0.0f, 0.0f,
                    100.0f, 0.0f, 0.0f,      0.0f, 0.0f,
                };

                var test_bytes = MemoryMarshal.AsBytes<float>(test);

                VBO3D = new Buffer(clyde, BufferTarget.ElementArrayBuffer, BufferUsageHint.DynamicDraw, test_bytes, "VBO3D");
            }

            public void DrawRect3D()
            {
                //var loadedTexture = _loadedTextures[command.TextureId];

                GL.BindVertexArray(this.VAO3D.Handle);

                var (program, loaded) = clyde.ActivateShaderInstance(this.modelShader.Handle);

                GL.ActiveTexture(TextureUnit.Texture0);
                //GL.BindTexture(TextureTarget.Texture2D, loadedTexture.OpenGLObject.Handle);

                GL.ActiveTexture(TextureUnit.Texture1);
                /*if (_lightingReady && loaded.HasLighting)
                {
                    var lightTexture = _loadedTextures[LightRenderTarget.Texture.TextureId].OpenGLObject;
                    GL.BindTexture(TextureTarget.Texture2D, lightTexture.Handle);
                }
                else
                {*/
                    var white = clyde._loadedTextures[clyde._stockTextureWhite.TextureId].OpenGLObject;
                    GL.BindTexture(TextureTarget.Texture2D, white.Handle);
                //}

                program.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
                program.SetUniformTextureMaybe(UniILightTexture, TextureUnit.Texture1);

                // Model matrix becomes identity since it's built into the batch mesh.
                program.SetUniformMaybe(UniIModelMatrix, Matrix4.Identity);
                // Reset ModUV to ensure it's identity and doesn't touch anything.
                program.SetUniformMaybe(UniIModUV, new Vector4(0, 0, 1, 1));

                program.SetUniformMaybe(UniIModulate, new Vector4(1, 1, 1, 1)); // modulate
                program.SetUniformMaybe(UniITexturePixelSize, new Vector2(1, 1)); // meh

                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            }
        }

        Render3D? render3d;

        Render3D GetClyde3D()
        {
            if (render3d == null)
            {
                render3d = new Render3D(this);
            }
            return render3d;
        }
    }
}
