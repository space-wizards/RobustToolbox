using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Robust.Shared.IoC;
using Robust.Shared.Configuration;
using Robust.Shared.Exceptions;
using Robust.Shared.Log;



namespace Robust.Client.HTTPClient
{
    /// <summary>
    ///    Interface for a Read-only client that can download files from a CDN.
    ///    This will be used to download and stream audio files from a whitelisted endpoint.
    ///    CDN must tell client what files are available to download by using a JSON file.
    /// </summary>
    public interface ICDNConsumer
    {

        void Initialize();
        Task<string> GetFileAsync(string url);
    }

    public class CDNConsumer : ICDNConsumer
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        // The are so many error loggers, honestly don't know which one to use
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;


        [Dependency] private readonly IJSON _json = default!;
        private ISawmill _sawmill = default!;

        private readonly HttpClient _httpClient;
        // move whitelisted domains to a configuration file and access it through IConfigurationManager
        private readonly string[] _whitelistedDomains = { "ia902304.us.archive.org", "example.com", "anotherexample.com" };

        //in windows this should be appdata of the game, should be in a configuration file
        private readonly string _downloadDirectory = "%APPDATA%\\Space Station 14\\data\\Cache"; // this should also be in a configuration file

        // this should also be in a configuration file
        private readonly string _manifestFilename = "cdn_manifest.json";


        public CDNConsumer()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30), // timeouts prevent hanging requests.
                MaxResponseContentBufferSize = 5_000_000 // Limit the response size (5MB here)
            };

        }

        public void Initialize()
        {
            _sawmill = Logger.GetSawmill("cdn");
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
                return "Invalid URL";
            }
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                // Validate the response
                // here we are checking if the response is a JSON file
                var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (!mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    // _sawmill.Error($"Invalid media type {nameof(mediaType)}");
                    return "Invalid media type";
                }
                var manifest = await response.Content.ReadAsStringAsync();

                var parsedManifest = _json.Parse(manifest);
                if (parsedManifest == null)
                {
                    // _sawmill.Error("CDNConsumer: Failed to parse manifest");
                    return "Failed to parse manifest";
                }
            }
            catch (HttpRequestException e)
            {
                _runtimeLog.LogException(e, "CDNConsumer: Failed to download file manifest");
                return "Failed to download file manifest";
            }

            return "Manifest downloaded";
        }
        public async Task<string> GetFileAsync(string url)
        {

            string filename = Path.GetFileName(url);
            string outputPath = Path.Combine(_downloadDirectory, filename);

            if (!Directory.Exists(_downloadDirectory))
            {
                Directory.CreateDirectory(_downloadDirectory);
            }

            // expand %appdata% to the actual path
            string fullOutputPath = Environment.ExpandEnvironmentVariables(outputPath);

            // Check if the file already exists
            if (File.Exists(fullOutputPath))
            {
                _sawmill.Info($"File already exists at {outputPath}");
                return outputPath;
            }

            // Check if the URL is valid and whitelisted
            if (!IsValidUrl(url))
            {
                return "Invalid URL";
            }
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                // Validate the response
                // here we are checking if the response is an audio file
                // TODO: ideally this should verify integrity of the file using a hash
                var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
                var fileExtension = Path.GetExtension(url);


                // TODO: this shouldn't be hardcoded, whitelisting file types should be in a configuration file
                if (!mediaType.Equals("application/ogg", StringComparison.OrdinalIgnoreCase))
                {
                    // _sawmill.Error($"CDNConsumer: Invalid media type {nameof(mediaType)}");
                    return "Invalid media type";
                }




                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(fullOutputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    try
                    {

                        await contentStream.CopyToAsync(fileStream);

                    }
                    catch (Exception e)
                    {
                        _sawmill.Error($"Failed to save file {e}");
                        return "Failed to save file";
                    }
                }

                _sawmill.Info($"File downloaded to {outputPath}");
                return outputPath;
            }
            catch (HttpRequestException e)
            {
                _runtimeLog.LogException(e, "CDNConsumer: Failed to download file");
                return "Failed to download file";
            }
        }

        // Check if the URL is valid and whitelisted
        private bool IsValidUrl(string url)
        {
            // Check if URL is valid
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                // _sawmill.Error("CDNConsumer: Invalid URL");
                return false;
            }
            // Check if domain is whitelisted
            var domain = uri.Host;
            if (!Array.Exists(_whitelistedDomains, d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)))
            {
                // _sawmill.Error("CDNConsumer: Domain is not whitelisted");
                return false;
            }

            // Ensure URL uses HTTPS
            if (!url.StartsWith("https://"))
            {
                // _sawmill.Error("CDNConsumer: URL must use HTTPS");
                return false;
            }

            return true;
        }
    }
}
