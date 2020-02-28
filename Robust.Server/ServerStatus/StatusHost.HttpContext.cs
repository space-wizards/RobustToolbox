using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.Server.ServerStatus
{

    internal sealed partial class StatusHost
    {

        private HttpContextFactory _ctxFactory;

        public HttpContext CreateContext(IFeatureCollection contextFeatures) => _ctxFactory.Create(contextFeatures);

        public void DisposeContext(HttpContext context, Exception exception)
        {
            if (exception != null)
            {
                Logger.ErrorS(Sawmill, $"Context disposed due to exception: {exception}");
            }

            _ctxFactory.Dispose(context);
        }

        private static HttpContextFactory CreateHttpContextFactory()
        {
            var ctxFacOptions = Options.Create(new FormOptions
            {
            });
            var ctxFactory = new HttpContextFactory(ctxFacOptions, new HttpContextAccessor());
            return ctxFactory;
        }

        private void InitHttpContextThread()
        {
            if (SynchronizationContext.Current == _syncCtx)
            {
                // maybe assert instead?
                return;
            }

            IoCManager.Clear();
            ILogManager logMgr = null;
            WaitSync(() =>
            {
                logMgr = IoCManager.Resolve<ILogManager>();
            }, ApplicationStopping);
            IoCManager.InitThread();
            if (logMgr != null)
            {
                IoCManager.RegisterInstance<ILogManager>(logMgr);
            }

            IoCManager.BuildGraph();
        }

    }

}
