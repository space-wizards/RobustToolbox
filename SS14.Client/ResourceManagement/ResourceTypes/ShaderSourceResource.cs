using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Utility;

namespace SS14.Client.ResourceManagement.ResourceTypes
{
    /// <summary>
    ///     Loads the **source code** of a shader.
    /// </summary>
    public class ShaderSourceResource : BaseResource
    {
        #if GODOT
        internal Godot.Shader GodotShader { get; private set; }
        #endif

        private readonly Dictionary<string, ShaderParamType> Parameters = new Dictionary<string, ShaderParamType>();

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            #if GODOT
            using (var stream = cache.ContentFileRead(path))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var code = reader.ReadToEnd();
                GodotShader = new Godot.Shader
                {
                    Code = code,
                };
            }

            var properties = Godot.VisualServer.ShaderGetParamList(GodotShader.GetRid());
            foreach (var dict in properties.Cast<IDictionary<object, object>>())
            {
                Parameters.Add((string)dict["name"], DetectParamType(dict));
            }
            #endif
        }

        internal bool TryGetShaderParamType(string paramName, out ShaderParamType shaderParamType)
        {
            return Parameters.TryGetValue(paramName, out shaderParamType);
        }

        private ShaderParamType DetectParamType(IDictionary<object, object> dict)
        {
            #if GODOT
            var type = (Godot.Variant.Type)dict["type"];
            var hint = (Godot.PropertyHint)dict["hint"];
            var hint_string = (string)dict["hint_string"];

            switch (type)
            {
                // See RasterizerStorageGLES3::shader_get_param_list for how I figured this out.
                case Godot.Variant.Type.Nil:
                    return ShaderParamType.Void;
                case Godot.Variant.Type.Bool:
                    return ShaderParamType.Bool;
                case Godot.Variant.Type.Int:
                    if (hint == Godot.PropertyHint.Flags)
                    {
                        // The bvecs are a flags variable,
                        // Hint string is in the form x,y,z,w,
                        // So these length matches check WHICH it is.
                        if (hint_string.Length == 3)
                        {
                            return ShaderParamType.BVec2;
                        }
                        else if (hint_string.Length == 5)
                        {
                            return ShaderParamType.BVec3;
                        }
                        else if (hint_string.Length == 7)
                        {
                            return ShaderParamType.BVec4;
                        }
                        else
                        {
                            throw new NotImplementedException("Hint string is not the right length?");
                        }
                    }
                    else
                    {
                        return ShaderParamType.Int;
                    }
                case Godot.Variant.Type.Real:
                    return ShaderParamType.Float;
                case Godot.Variant.Type.Vector2:
                    return ShaderParamType.Vec2;
                case Godot.Variant.Type.Vector3:
                    return ShaderParamType.Vec3;
                case Godot.Variant.Type.Transform:
                case Godot.Variant.Type.Basis:
                case Godot.Variant.Type.Transform2d:
                    throw new NotImplementedException("Matrices are not implemented yet.");
                case Godot.Variant.Type.Plane:
                case Godot.Variant.Type.Color:
                    return ShaderParamType.Vec4;
                case Godot.Variant.Type.Object:
                    if (hint_string == "Texture")
                    {
                        return ShaderParamType.Sampler2D;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                case Godot.Variant.Type.IntArray:
                    // Godot merges all the integral vectors into this.
                    // Can't be more specific sadly.
                    return ShaderParamType.IntVec;
                default:
                    throw new NotSupportedException($"Unknown variant type: {type}");
            }
            #else
            throw new NotImplementedException();
            #endif
        }
    }
}
