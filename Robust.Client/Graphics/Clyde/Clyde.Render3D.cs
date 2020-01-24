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

                var test = new float[] {
                    0.0f, 0.0f, 0.0f,       1.0f, 0.0f,
                    0.0f, -1.0f, 0.0f,      1.0f, 1.0f,
                    -1.0f, 0.0f, 0.0f,      0.0f, 0.0f,

                    0.0f, -1.0f, 0.0f,      1.0f, 1.0f,
                    -1.0f, -1.0f, 0.0f,     0.0f, 1.0f,
                    -1.0f, 0.0f, 0.0f,      0.0f, 0.0f,
                };

                var test_bytes = MemoryMarshal.AsBytes<float>(test);

                VBO3D = new Buffer(clyde, BufferTarget.ArrayBuffer, BufferUsageHint.DynamicDraw, test_bytes, "VBO3D");
                VBO3D.Use();

                // Vertex Coords
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vert_size, 0);
                GL.EnableVertexAttribArray(0);
                // Texture Coords.
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, vert_size, 3 * sizeof(float));
                GL.EnableVertexAttribArray(1);
            }

            /// Terribly unoptimized, should probably have a sprite batch for 3D quads if this evolves beyond just being a meme.
            public void DrawRect3D(Matrix4 transform, Texture texture, Color color)
            {
                var region = new Vector4(0, 0, 1, 1);

                if (texture is AtlasTexture atlas)
                {
                    texture = atlas.SourceTexture;
                    var subRegion = atlas.SubRegion;
                    region = new Vector4(
                        subRegion.Left / texture.Width,
                        subRegion.Top / texture.Height,
                        subRegion.Right / texture.Width,
                        subRegion.Bottom / texture.Height);
                }

                var loadedTexture = clyde._loadedTextures[ ((ClydeTexture)texture).TextureId ];

                GL.BindVertexArray(this.VAO3D.Handle);

                var (program, loaded) = clyde.ActivateShaderInstance(this.modelShader.Handle);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, loadedTexture.OpenGLObject.Handle);

                program.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
                program.SetUniformTextureMaybe(UniILightTexture, TextureUnit.Texture1);

                // Model matrix
                program.SetUniformMaybe(UniIModelMatrix, transform);
                // Set ModUV to our subregion
                program.SetUniformMaybe(UniIModUV, region);

                program.SetUniformMaybe(UniIModulate, color); // modulate
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
