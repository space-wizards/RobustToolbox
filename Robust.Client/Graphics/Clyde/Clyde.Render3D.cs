using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using OpenTK.Graphics.OpenGL;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Shared.Interfaces.GameObjects;
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

            public enum Sprite3DRenderMode
            {
                Floor,  // Draw Flat on the ground, no extra transformations
                Table,  // Higher than floor
                SimpleSprite, // Always rotate to face the camera
                Wall // Render 4 sides + top cap
            }

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
                    0.5f, 0.5f, 0.0f,       1.0f, 0.0f,
                    0.5f, -0.5f, 0.0f,      1.0f, 1.0f,
                    -0.5f, 0.5f, 0.0f,      0.0f, 0.0f,

                    0.5f, -0.5f, 0.0f,      1.0f, 1.0f,
                    -0.5f, -0.5f, 0.0f,     0.0f, 1.0f,
                    -0.5f, 0.5f, 0.0f,      0.0f, 0.0f,
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
            public void DrawRect3D(Matrix4 transform, Texture texture, Color color, UIBox2? subRegion = null)
            {
                var region = new Vector4(0, 0, 1, 1);

                if (texture is AtlasTexture atlas)
                {
                    texture = atlas.SourceTexture;
                    region = new Vector4(
                        atlas.SubRegion.Left / texture.Width,
                        atlas.SubRegion.Top / texture.Height,
                        atlas.SubRegion.Right / texture.Width,
                        atlas.SubRegion.Bottom / texture.Height);
                } else if (subRegion.HasValue)
                {
                    region = new Vector4(
                        subRegion.Value.Left,
                        subRegion.Value.Top,
                        subRegion.Value.Right,
                        subRegion.Value.Bottom);
                }

                var loadedTexture = clyde._loadedTextures[ ((ClydeTexture)texture).TextureId ];

                GL.BindVertexArray(this.VAO3D.Handle);

                var (program, loaded) = clyde.ActivateShaderInstance(this.modelShader.Handle);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, loadedTexture.OpenGLObject.Handle);

                program.SetUniformTextureMaybe(UniIMainTexture, TextureUnit.Texture0);
                program.SetUniformTextureMaybe(UniILightTexture, TextureUnit.Texture1);

                // Need to take the transpose because linal is hell I guess.
                transform.Transpose();

                // Model matrix
                program.SetUniformMaybe(UniIModelMatrix, transform);
                // Set ModUV to our subregion
                program.SetUniformMaybe(UniIModUV, region);

                program.SetUniformMaybe(UniIModulate, color); // modulate
                program.SetUniformMaybe(UniITexturePixelSize, new Vector2(1, 1)); // meh

                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            }

            Vector3 CameraPos = Vector3.Zero;
            Matrix4 FaceCameraRotation = Matrix4.Identity;

            public Angle CameraAngle2D { get; private set; }

            public (Matrix4,Matrix4) GetProjViewMatrices3D(Vector2 eyePos)
            {
                float dist = 4;
                float dist_v = 8;//0.3f;
                float angle = clyde._renderTime;// * 0.1f;
                var basePos = new Vector3(eyePos.X, eyePos.Y, 0);
                CameraAngle2D = new Angle(angle);
                CameraPos = basePos + new Vector3(MathF.Sin(angle) * -dist, MathF.Cos(angle) * -dist, dist_v);

                //var s = Math.Sin(this._renderTime);
                //var viewMatrixWorld = Matrix4.LookAt(new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
                var viewMatrixWorld = Matrix4.CreateTranslation(-CameraPos);
                //viewMatrixWorld *= Matrix4.Scale(0.01f);
                var cameraRotation = Matrix4.CreateRotationZ(angle);
                cameraRotation *= Matrix4.CreateRotationX(-0.7f);

                viewMatrixWorld *= cameraRotation;
                //viewMatrixWorld.Invert();
                //var viewMatrixWorld = Matrix4.Identity;
                FaceCameraRotation = cameraRotation;
                FaceCameraRotation.Transpose();

                float aspect = (float)clyde.ScreenSize.X / clyde.ScreenSize.Y;

                var projMatrixWorld = Matrix4.CreatePerspectiveFieldOfView(1.5f, aspect, 0.1f, 100);

                return (projMatrixWorld, viewMatrixWorld);
            }

            public Matrix4 GetSpriteTransform(Sprite3DRenderMode renderMode, int n = 0)
            {
                switch (renderMode)
                {
                    case Sprite3DRenderMode.Floor:
                        return Matrix4.CreateTranslation(0, 0, -.5f);
                    case Sprite3DRenderMode.Table:
                        return Matrix4.CreateTranslation(0, 0, -.25f);
                    case Sprite3DRenderMode.Wall:
                        switch (n)
                        {
                            case 0:
                                return Matrix4.CreateRotationY(MathHelper.PiOver2) * Matrix4.CreateTranslation(.5f, 0, 0);
                            case 1:
                                return Matrix4.CreateRotationY(MathHelper.PiOver2) * Matrix4.CreateTranslation(-.5f, 0, 0);
                            case 2:
                                return Matrix4.CreateRotationX(MathHelper.PiOver2) * Matrix4.CreateTranslation(0, .5f, 0);
                            case 3:
                                return Matrix4.CreateRotationX(MathHelper.PiOver2) * Matrix4.CreateTranslation(0, -.5f, 0);
                            default:
                                return Matrix4.CreateTranslation(0, 0, .5f);
                        }
                    case Sprite3DRenderMode.SimpleSprite:
                        return FaceCameraRotation;
                }
                return Matrix4.Identity;
            }

            public static Sprite3DRenderMode InferRenderMode(IEntity ent)
            {
                switch (ent.Name)
                {
                    case "worktop":
                        return Sprite3DRenderMode.Table;
                    case "Solid wall":
                        return Sprite3DRenderMode.Wall;
                    case "Catwalk":
                    case "Wire":
                        return Sprite3DRenderMode.Floor;
                }
                if (ent.Name.Contains("Airlock"))
                {
                    return Sprite3DRenderMode.Wall;
                }

                return Sprite3DRenderMode.SimpleSprite;
            }
        }

        Render3D? render3d;

        Render3D GetRender3D()
        {
            if (render3d == null)
            {
                render3d = new Render3D(this);
            }
            return render3d;
        }
    }
}
