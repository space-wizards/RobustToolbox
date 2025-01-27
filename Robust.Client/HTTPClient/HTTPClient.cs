using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Robust.Client.Audio;
using Robust.Client.Audio.Sources;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio.Sources;
using Robust.Shared.IoC;
using Robust.Shared.Configuration;
using Robust.Shared.Exceptions;
using Robust.Shared.Log;

using NVorbis;


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

        Task<string> PlayAudioFromCDN(string url);

        Task<string> GetFileAsync(string url);

    }

    public class CDNConsumer : ICDNConsumer
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

        [Dependency] private readonly IAudioManager _audioMgr = default!;

        [Dependency] private readonly IJSON _json = default!;
        private readonly Dictionary<string, bool> _cachedFiles = new Dictionary<string, bool>();
        private List<IAudioSource> _activeSources = new List<IAudioSource>();
        private ISawmill _sawmill = default!;

        private readonly HttpClient _httpClient;
        // move whitelisted domains to a configuration file and access it through IConfigurationManager
        private readonly string[] _whitelistedDomains = { "ia902304.us.archive.org", "example.com", "anotherexample.com" };

        //in windows this should be appdata of the game, should be in a configuration file
        private readonly string _downloadDirectory = "%APPDATA%\\Space Station 14\\data\\Cache"; // this should also be in a configuration file

        // this should also be in a configuration file
        private readonly string _manifestFilename = "cdn_manifest.json";

        // TODO: this is a temporary solution,
        // public IBufferedAudioSource Source { get; set; }


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
        ///  Plays fetches and plays audio from an URL using GetFileAsync
        /// </summary>
        /// <param name="url"></param>
        public async Task<string> PlayAudioFromCDN(string url)
        {
            string filepath = await GetFileAsync(url);

            if (filepath != "Failed to download file")
            {
                PlayOggAudioFile(filepath);
                return "Audio played";
            }
            return "Failed to play audio";
        }



        /// <summary>
        ///    Downloads a file from a CDN.
        ///    The file is cached to the local filesystem inside _downloadDirectory.
        ///    The file is saved with the same name as the file on the CDN.
        ///    If the file already exists, it will not be downloaded again.
        ///    The file is validated to ensure it is an audio file and from a whitelisted domain.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<string> GetFileAsync(string url)
        {

            string filename = Path.GetFileName(url);
            // expand %appdata% to the actual path
            string fullDownloadDir = Environment.ExpandEnvironmentVariables(_downloadDirectory);
            string fullOutputPath = Path.Combine(fullDownloadDir, filename);

            // Create the download directory if it doesn't exist
            if (!Directory.Exists(fullDownloadDir))
            {
                Directory.CreateDirectory(fullDownloadDir);
            }

            // Check if the file already exists. this could be less hacky.
            if (File.Exists(fullOutputPath))
            {
                _sawmill.Info($"File already exists at {_downloadDirectory}");
                return fullOutputPath;
            }

            // Check if the URL is valid and whitelisted
            if (!IsValidUrl(url))
            {
                return "Invalid URL";
            }

            // Download the file
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
                if (!(mediaType.Equals("application/ogg", StringComparison.OrdinalIgnoreCase)
                    || mediaType.Equals("audio/ogg", StringComparison.OrdinalIgnoreCase)))
                {
                    _sawmill.Error($"Invalid media type {nameof(mediaType)}");
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
                        _sawmill.Error($"Failed to save file: {e}");
                        return "Failed to save file";
                    }
                }

                _sawmill.Info($"File downloaded to {fullOutputPath}");
                return fullOutputPath;
            }
            catch (HttpRequestException e)
            {
                _sawmill.Error($"Failed to download file: {e}");
                return "Failed to download file";
            }
        }

        /// <summary>
        ///     This is not implemented yet.
        ///     Downloads a manifest file from a CDN.
        ///     The manifest file is a JSON file that contains a list of files that are available to download.
        ///     The manifest file is parsed and loaded into memory.
        /// </summary>
        private async Task<string> GetManifestAsync(string url)
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

        /// <summary>
        ///   This is not implemented yet.
        ///   Downloads a file from a CDN in chunks.
        ///   This is useful for downloading large files.
        ///   Has a callback to play audio while the file is being downloaded.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<string> GetChunkedFileAsync(string url, int chunkSize = -1)
        {
            string filename = Path.GetFileName(url);
            string outputPath = Path.Combine(_downloadDirectory, filename);
            // expand %appdata% to the actual path
            string fullOutputPath = Environment.ExpandEnvironmentVariables(outputPath);

            // Check if the file already exists
            if (File.Exists(fullOutputPath))
            {
                _sawmill.Info($"File already exists at {outputPath}");

                PlayOggAudioFile(fullOutputPath);

                return "File already exists";
            }

            // Check if the URL is valid and whitelisted
            if (!IsValidUrl(url))
            {
                _sawmill.Error("Invalid URL");
                return "Invalid URL";
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

            if (chunkSize != -1)
                request.Headers.Range = new RangeHeaderValue(0, chunkSize);

            long totalBytes = 0;
            long bytesDownloaded = 0;

            // TODO: getting the total size of the file should already be available in a manifest file inside cdn
            // this should be refactored to get the total size from the manifest file so we don't have to make a HEAD request
            try
            {
                // Get the total size of the file
                using (var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)))
                {
                    response.EnsureSuccessStatusCode();
                    totalBytes = response.Content.Headers.ContentLength ?? 0;

                }
            }
            catch (HttpRequestException e)
            {
                _sawmill.Error("Failed to get file size");
                return "Failed to get file size";
            }


            while (bytesDownloaded < totalBytes)
            {
                var chunkRequest = new HttpRequestMessage(HttpMethod.Get, url);

                if (chunkSize == -1)
                    chunkSize = (int)totalBytes;

                chunkRequest.Headers.Range = new RangeHeaderValue(bytesDownloaded, bytesDownloaded + chunkSize - 1);

                try
                {

                    using (var response = await _httpClient.SendAsync(chunkRequest))
                    {
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsByteArrayAsync();

                        // // Queue the buffer to the audio source
                        // QueueBuffer(content);

                        MemoryStream stream = new MemoryStream();

                        await stream.WriteAsync(content, (int)bytesDownloaded, content.Length);

                        //_audioMgr.LoadAudioPartialOggVorbis(stream, (int)bytesDownloaded, content.Length, filename);

                        bytesDownloaded += content.Length;
                    }
                }
                catch (HttpRequestException e)
                {
                    _sawmill.Error("Failed to download file in chunks");
                    return "Failed to download file in chunks";
                }


                _sawmill.Info($"Downloaded {bytesDownloaded}/{totalBytes} bytes");
            }

            _sawmill.Info("File downloaded in chunks");
            return "File downloaded in chunks";
        }


        // for testing purposes only
        private void PlayOggAudioFile(string filepath)
        {
            string filename = Path.GetFileName(filepath);
            using FileStream fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            AudioStream audioStream = _audioMgr.LoadAudioOggVorbis(fileStream, filename);
            var source = _audioMgr.CreateAudioSource(audioStream);

            if (source != null)
            {
                _activeSources.Add(source);
                source.StartPlaying();

                // TODO: add an event listener to remove the source from the list when it finishes playing
                // source.PlaybackFinished += (src) =>
                // {
                //     src.Dispose();
                //     _activeSources.Remove(src);
                // };
            }
            else
                _sawmill.Error("Failed to create audio source");
        }
        // Check if the URL is valid and whitelisted
        private bool IsValidUrl(string url)
        {
            // Check if URL is valid
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                _sawmill.Error("Invalid URL");
                return false;
            }
            // uncomment this if to enforce domain whitelist
            // Check if domain is whitelisted
            // var domain = uri.Host;
            // if (!Array.Exists(_whitelistedDomains, d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)))
            // {
            //     _sawmill.Error("Requested domain is not whitelisted");
            //     return false;
            // }

            // uncomment this if to enforce HTTPS
            // Ensure URL uses HTTPS
            // if (!url.StartsWith("https://"))
            // {
            //     // _sawmill.Error("CDNConsumer: URL must use HTTPS");
            //     return false;
            // }

            return true;
        }

    }
}
