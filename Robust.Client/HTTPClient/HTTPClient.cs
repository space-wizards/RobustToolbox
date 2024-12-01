using System;
using System.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;



namespace Robust.Client.HTTPClient
{
    /// <summary>
    ///    Interface for a Read-only client that can download files from a CDN.
    ///    This will be used to download and stream audio files from a whitelisted endpoint.
    ///    CDN must tell client what files are available to download by using a JSON file.
    /// </summary>
    public interface ICDNConsumer
    {
        Task<string> GetFileAsync(string url, string outputPath);
    }

    public class CDNConsumer : ICDNConsumer
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        // The are so many error loggers, honestly don't know which one to use
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

        private readonly HttpClient _httpClient;
        // move whiteListed domains to a configuration file and access it through IConfigurationManager
        private readonly string[] _whitelistedDomains = { "example.com", "anotherexample.com" };

        // this should also be in a configuration file
        private readonly string _manifestFilename = "manifest.json";


        public CDNConsumer()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30), // timeouts prevent hanging requests.
                MaxResponseContentBufferSize = 1_000_000 // Limit the response size (1MB here)
            };
        }

        /// <summary>
        ///     Downloads a manifest file from a CDN.
        ///     The manifest file is a JSON file that contains a list of files that are available to download.
        ///     The manifest file will be cached to the local filesystem. (im not sure if this is a good idea)
        /// </summary>
        public async Task<string> GetManifestAsync(string url)
        {
            // Check if the URL is valid and whitelisted
            if (!IsValidUrl(url))
            {
                _runtimeLog.LogException(e, "CDNConsumer: Invalid URL");
                return null;
            }
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
            }

            // Validate the response
            // here we are checking if the response is a JSON file
            var mediaType = response.Content.Headers.ContentType.MediaType;
            if (!mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                _runtimeLog.LogException(e, $"Invalid media type {nameof(mediaType)}");
            }

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(_manifestFilename, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                await contentStream.CopyToAsync(fileStream);
            }

        }
        public async Task<string> GetFileAsync(string url, string outputPath)
        {
            // Check if the URL is valid and whitelisted
            if (!IsValidUrl(url))
            {
                _runtimeLog.LogException(e, "CDNConsumer: Invalid URL");
                return null;
            }

            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                _runtimeLog.LogException(e, "CDNConsumer: Failed to download file");
                return null;
            }

            // Validate the response
            // here we are checking if the response is an audio file
            // ideally this should verify integrity of the file using a hash
            var mediaType = response.Content.Headers.ContentType.MediaType;
            var fileExtension = Path.GetExtension(outputPath);


            // this shouldn't be hardcoded, whitelisting file types should be in a configuration file
            if (!mediaType.Equals("audio/ogg", StringComparison.OrdinalIgnoreCase))
            {
                _runtimeLog.LogException(e, $"CDNConsumer: Invalid media type {nameof(mediaType)}");
            }

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                await contentStream.CopyToAsync(fileStream);
            }

        }

        // Check if the URL is valid and whitelisted
        private bool IsValidUrl(string url)
        {
            // Check if URL is valid
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                _runtimeLog.LogException(e, "CDNConsumer: Invalid URL");
                return false;
            }
            // Check if domain is whitelisted
            var domain = uri.Host;
            if (!Array.Exists(_whitelistedDomains, d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)))
            {
                _runtimeLog.LogException(e, "CDNConsumer: Domain is not whitelisted");
                return false;
            }

            // Ensure URL uses HTTPS
            if (!url.StartsWith("https://"))
            {
                _runtimeLog.LogException(e, "CDNConsumer: URL must use HTTPS");
                return false;
            }

            return true;
        }
    }
}
