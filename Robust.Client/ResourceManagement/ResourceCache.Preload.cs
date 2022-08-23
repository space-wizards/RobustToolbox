using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.Client.ResourceManagement
{
    internal partial class ResourceCache
    {
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        public void PreloadTextures()
        {
            var sawmill = _logManager.GetSawmill("res.preload");

            if (!_configurationManager.GetCVar(CVars.ResTexturePreloadingEnabled))
            {
                sawmill.Debug($"Skipping texture preloading due to CVar value.");
                return;
            }

            PreloadTextures(sawmill);
            PreloadRsis(sawmill);
        }

        private void PreloadTextures(ISawmill sawmill)
        {
            sawmill.Debug("Preloading textures...");
            var sw = Stopwatch.StartNew();
            var resList = GetTypeDict<TextureResource>();

            var texList = ContentFindFiles("/Textures/")
                // Skip PNG files inside RSIs.
                .Where(p => p.Extension == "png" && !p.ToString().Contains(".rsi/") && !resList.ContainsKey(p))
                .Select(p => new TextureResource.LoadStepData {Path = p})
                .ToArray();

            Parallel.ForEach(texList, data =>
            {
                try
                {
                    TextureResource.LoadPreTexture(this, data);
                }
                catch (Exception e)
                {
                    // Mark failed loads as bad and skip them in the next few stages.
                    // Avoids any silly array resizing or similar.
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                }
            });

            foreach (var data in texList)
            {
                if (data.Bad)
                    continue;

                try
                {
                    TextureResource.LoadTexture(_clyde, data);
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                }
            }

            var errors = 0;
            foreach (var data in texList)
            {
                if (data.Bad)
                {
                    errors += 1;
                    continue;
                }

                try
                {
                    var texResource = new TextureResource();
                    texResource.LoadFinish(this, data);
                    resList[data.Path] = texResource;
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                    errors += 1;
                }
            }

            sawmill.Debug(
                "Preloaded {CountLoaded} textures ({CountErrored} errored) in {LoadTime}",
                texList.Length,
                errors,
                sw.Elapsed);
        }

        private void PreloadRsis(ISawmill sawmill)
        {
            var sw = Stopwatch.StartNew();
            var resList = GetTypeDict<RSIResource>();

            var rsiList = ContentFindFiles("/Textures/")
                .Where(p => p.ToString().EndsWith(".rsi/meta.json"))
                .Select(c => c.Directory)
                .Where(p => !resList.ContainsKey(p))
                .Select(p => new RSIResource.LoadStepData {Path = p})
                .ToArray();

            Parallel.ForEach(rsiList, data =>
            {
                try
                {
                    RSIResource.LoadPreTexture(this, data);
                }
                catch (Exception e)
                {
                    // Mark failed loads as bad and skip them in the next few stages.
                    // Avoids any silly array resizing or similar.
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                }
            });

            foreach (var data in rsiList)
            {
                if (data.Bad)
                    continue;

                try
                {
                    RSIResource.LoadTexture(_clyde, data);
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                }
            }

            Parallel.ForEach(rsiList, data =>
            {
                if (data.Bad)
                    return;

                try
                {
                    RSIResource.LoadPostTexture(data);
                }
                catch (Exception e)
                {
                    data.Bad = true;
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                }
            });

            var errors = 0;
            foreach (var data in rsiList)
            {
                if (data.Bad)
                {
                    errors += 1;
                    continue;
                }

                try
                {
                    var rsiRes = new RSIResource();
                    rsiRes.LoadFinish(this, data);
                    resList[data.Path] = rsiRes;
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                    errors += 1;
                }
            }

            sawmill.Debug(
                "Preloaded {CountLoaded} RSIs ({CountErrored} errored) in {LoadTime}",
                rsiList.Length,
                errors,
                sw.Elapsed);

        }
    }
}
