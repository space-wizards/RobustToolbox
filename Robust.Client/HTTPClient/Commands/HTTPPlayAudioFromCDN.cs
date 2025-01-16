using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.HTTPClient.Commands
{
    public sealed class HTTPPlayAudioFromCDN : LocalizedCommands
    {
        [Dependency] private readonly ICDNConsumer _consumer = default!;

        public override string Command => "playaudiocdn";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteLine("Usage: playaudiocdn <url>");
                return;
            }

            var url = args[0];
            _consumer.PlayAudioFromCDN(url);
        }
    }
}
