using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.ResourceManagement.ResourceTypes
{
    /// <summary>
    ///     Loads the **source code** of a shader.
    /// </summary>
    public class ShaderSourceResource : BaseResource
    {
        internal int ClydeHandle { get; private set; } = -1;
        internal ParsedShader ParsedShader { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            using (var stream = cache.ContentFileRead(path))
            using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
            {
                ParsedShader = ShaderParser.Parse(reader);
            }

            var clyde = IoCManager.Resolve<IClyde>();
            // TODO: vertex shaders.
            ClydeHandle = clyde.LoadShader(ParsedShader, path.ToString());
        }

        private string _getGodotCode()
        {
            var output = new StringBuilder();

            output.Append("shader_type canvas_item;\n");

            if ((ParsedShader.RenderMode & ShaderRenderMode.Unshaded) != 0)
            {
                output.Append("render_mode unshaded;\n");
            }

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
