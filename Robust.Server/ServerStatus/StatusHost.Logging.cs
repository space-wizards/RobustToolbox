using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Microsoft.Extensions.Logging;
using LogLevel = Robust.Shared.Log.LogLevel;

namespace Robust.Server.ServerStatus
{

    internal sealed partial class StatusHost
    {

        private Dictionary<string, SawmillWrapper> _sawmillCache = new Dictionary<string, SawmillWrapper>();

        public ILogger CreateLogger(string categoryName)
        {
            if (!_sawmillCache.TryGetValue(categoryName, out var wrapper))
            {
                var newCatName = categoryName;
                if (newCatName.StartsWith("Microsoft.AspNetCore.Server.Kestrel"))
                {
                    newCatName = "http";
                }
                else
                {
                    newCatName = newCatName.Replace("Microsoft.AspNetCore.", "aspnet.");
                }

                wrapper = new SawmillWrapper(Logger.GetSawmill($"{Sawmill}.{newCatName}"));
                _sawmillCache[categoryName] = wrapper;
            }

            return wrapper;
        }

        public void AddProvider(ILoggerProvider provider)
            => throw new NotImplementedException();

        private static void ConfigureSawmills()
        {
            var logMgr = IoCManager.Resolve<ILogManager>();
            logMgr.GetSawmill("statushost.http").Level = LogLevel.Warning;
            logMgr.GetSawmill("statushost.aspnet").Level = LogLevel.Warning;
        }

    }

}
