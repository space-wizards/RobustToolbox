using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Shared.Network;

public interface IHttpManager
{
    /// <summary>
    ///     Send a GET request to the specified URI and return the response body
    ///     as a stream in an asynchronous operation.
    /// </summary>
    /// <param name="uri">The URI the request is sent to.</param>
    /// <param name="cancel">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation</returns>
    Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancel);

    /// <summary>
    ///     Send a GET request to the specified URI and return the response body
    ///     as a string in an asynchronous operation.
    /// </summary>
    /// <param name="uri">The URI the request is sent to.</param>
    /// <param name="cancel">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation</returns>
    Task<string> GetStringAsync(Uri uri, CancellationToken cancel);

    /// <summary>
    ///     Send a GET request to the specified URI and return the response body
    ///     as a byte array in an asynchronous operation.
    /// </summary>
    /// <param name="uri">The URI the request is sent to.</param>
    /// <param name="cancel">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation</returns>
    Task<byte[]> GetByteArrayAsync(Uri uri, CancellationToken cancel = default);

    /// <summary>
    ///     Send a GET request to the specified URI and return the JSON response
    ///     body as a deserialized object in an asynchronous operation.
    /// </summary>
    /// <param name="uri">The URI the request is sent to.</param>
    /// <param name="cancel">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation</returns>
    Task<T?> GetFromJsonAsync<T>(Uri uri, CancellationToken cancel = default);

    /// <summary>
    ///     Send a GET request to the specified URI and copies the response body
    ///     into a stream in an asynchronous operation.
    /// </summary>
    /// <param name="uri">The URI the request is sent to.</param>
    /// <param name="stream">The stream to copy the response body into.</param>
    /// <param name="cancel">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation</returns>
    Task CopyToAsync(Uri uri, Stream stream, CancellationToken cancel = default);
}
