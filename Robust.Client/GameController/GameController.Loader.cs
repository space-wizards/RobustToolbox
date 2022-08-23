using Robust.Client;
using Robust.LoaderApi;

[assembly: LoaderEntryPoint(typeof(GameController.LoaderEntryPoint))]

namespace Robust.Client
{
    internal partial class GameController
    {
        internal sealed class LoaderEntryPoint : ILoaderEntryPoint
        {
            public void Main(IMainArgs args)
            {
                Start(args.Args, new GameControllerOptions(), contentStart: false, args);
            }
        }
    }
}
