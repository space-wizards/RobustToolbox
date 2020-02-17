using System.IO;
using Robust.Client.Graphics;
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
    internal class ShaderSourceResource : BaseResource
    {
        internal ClydeHandle ClydeHandle { get; private set; }
        internal ParsedShader ParsedShader { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            using (var stream = cache.ContentFileRead(path))
            using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
            {
                ParsedShader = ShaderParser.Parse(reader, cache);
            }

            var clyde = IoCManager.Resolve<IClydeInternal>();
            ClydeHandle = clyde.LoadShader(ParsedShader, path.ToString());
        }

        public override void Reload(IResourceCache cache, ResourcePath path)
        {
            using (var stream = cache.ContentFileRead(path))
            using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
            {
                ParsedShader = ShaderParser.Parse(reader, cache);
            }

            var clyde = IoCManager.Resolve<IClydeInternal>();
            clyde.ReloadShader(ClydeHandle, ParsedShader);
        }
    }
}
