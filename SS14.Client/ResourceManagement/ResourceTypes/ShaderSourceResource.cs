using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.IoC;
using SS14.Shared.Utility;

namespace SS14.Client.ResourceManagement.ResourceTypes
{
    /// <summary>
    ///     Loads the **source code** of a shader.
    /// </summary>
    public class ShaderSourceResource : BaseResource
    {
        internal Godot.Shader GodotShader { get; private set; }
        internal int ClydeHandle { get; private set; } = -1;
        internal ParsedShader ParsedShader { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            using (var stream = cache.ContentFileRead(path))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                ParsedShader = ShaderParser.Parse(reader);
            }

            switch (GameController.Mode)
            {
                case GameController.DisplayMode.Headless:
                    return;
                case GameController.DisplayMode.Godot:
                    GodotShader = new Godot.Shader
                    {
                        Code = _getGodotCode(),
                    };
                    break;
                case GameController.DisplayMode.OpenGL:
                    ClydeHandle = IoCManager.Resolve<IDisplayManagerOpenGL>().LoadShader(ParsedShader);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (GameController.OnGodot)
            {
                GodotShader = new Godot.Shader
                {
                    Code = _getGodotCode(),
                };
            }
            else
            {
                var clyde = IoCManager.Resolve<IDisplayManagerOpenGL>();
                // TODO: vertex shaders.
                ClydeHandle = clyde.LoadShader(ParsedShader, path.ToString());
            }
        }

        private string _getGodotCode()
        {
            var output = new StringBuilder();

            output.Append("shader_type canvas_item;\n");

            foreach (var uniform in ParsedShader.Uniforms.Values)
            {
                if (uniform.DefaultValue != null)
                {
                    output.AppendFormat("uniform {0} {1} = {2};", uniform.Type.GetNativeType(), uniform.Name,
                        uniform.DefaultValue);
                }
                else
                {
                    output.AppendFormat("uniform {0} {1};", uniform.Type.GetNativeType(), uniform.Name);
                }
            }

            foreach (var varying in ParsedShader.Varyings.Values)
            {
                output.AppendFormat("varying {0} {1};", varying.Type.GetNativeType(), varying.Name);
            }

            foreach (var function in ParsedShader.Functions)
            {
                output.AppendFormat("{0} {1}(", function.ReturnType.GetNativeType(), function.Name);
                var first = true;
                foreach (var parameter in function.Parameters)
                {
                    if (!first)
                    {
                        output.Append(", ");
                    }

                    first = false;

                    output.AppendFormat("{0} {1} {2}", parameter.Qualifiers.GetString(), parameter.Type.GetNativeType(),
                        parameter.Name);
                }

                output.AppendFormat(") {{\n{0}\n}}\n", function.Body);
            }

            return output.ToString();
        }
    }
}
