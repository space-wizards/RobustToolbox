namespace Robust.Shared.Network;

public interface IHttpManager
{
    Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancel);

    Task<string> GetStringAsync(Uri uri, CancellationToken cancel);
}

