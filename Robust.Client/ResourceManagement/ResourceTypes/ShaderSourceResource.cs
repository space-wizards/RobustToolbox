using System.IO;
using System.Threading;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ResourceManagement
{
    /// <summary>
    ///     Loads the **source code** of a shader.
    /// </summary>
    internal sealed class ShaderSourceResource : BaseResource
    {
        [ViewVariables]
        internal ClydeHandle ClydeHandle { get; private set; }

        [ViewVariables]
        internal ParsedShader ParsedShader { get; private set; } = default!;

        public override void Load(IResourceCache cache, ResPath path)
        {
            using (var stream = cache.ContentFileRead(path))
            using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
            {
                ParsedShader = ShaderParser.Parse(reader, cache);
            }

            var clyde = IoCManager.Resolve<IClydeInternal>();
            ClydeHandle = clyde.LoadShader(ParsedShader, path.ToString());
        }

        public override void Reload(IResourceCache cache, ResPath path, CancellationToken ct = default)
        {
            ct = ct != default ? ct : new CancellationTokenSource(30000).Token;

            for (;;)
            {
                try
                {
                    using var stream = cache.ContentFileRead(path);
                    using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
                    ParsedShader = ShaderParser.Parse(reader, cache);
                    break;
                }
                catch (IOException ioe)
                {
                    if (!PathHelpers.IsFileInUse(ioe))
                    {
                        throw;
                    }

                    ct.ThrowIfCancellationRequested();

                    Thread.Sleep(3);
                }
            }

            var clyde = IoCManager.Resolve<IClydeInternal>();
            clyde.ReloadShader(ClydeHandle, ParsedShader);
        }
    }
}
