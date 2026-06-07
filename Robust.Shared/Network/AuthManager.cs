using System;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Network
{
    // Basically turbo-lightweight IConfigurationManager for the purposes of auth var loading from env.

    /// <summary>
    ///     Stores client authentication parameters.
    /// </summary>
    internal interface IAuthManager
    {
        NetUserId? UserId { get; set; }
        string? Server { get; set; }
        string? StarlightApi { get; set; } // Starlight-edit
        string? Token { get; set; }
        string? PubKey { get; set; }
        string? DiscordToken { get; set; } // Starlight-edit

        /// <summary>
        /// If true, the user allows HWID information to be provided to servers.
        /// </summary>
        bool AllowHwid { get; set; }

        void LoadFromEnv();
    }

    internal sealed class AuthManager : IAuthManager
    {
        public const string DefaultAuthServer = "https://auth.spacestation14.com/";
        public const string DefaultStarlightAPI = "https://starlight.network/"; // Starlight-edit

        public NetUserId? UserId { get; set; }
        public string? Server { get; set; } = DefaultAuthServer;
        public string? StarlightApi { get; set; } = DefaultStarlightAPI; // Starlight-edit
        public string? Token { get; set; }
        public string? PubKey { get; set; }
        public string? DiscordToken { get; set; } // Starlight-edit
        public bool AllowHwid { get; set; } = true;

        public void LoadFromEnv()
        {
            if (TryGetVar("ROBUST_AUTH_SERVER", out var server))
                Server = server;

            if (TryGetVar("ROBUST_AUTH_USERID", out var userId))
                UserId = new NetUserId(Guid.Parse(userId));

            if (TryGetVar("ROBUST_AUTH_PUBKEY", out var pubKey))
                PubKey = pubKey;

            if (TryGetVar("ROBUST_AUTH_TOKEN", out var token))
                Token = token;

            if (TryGetVar("ROBUST_AUTH_ALLOW_HWID", out var allowHwid))
                AllowHwid = allowHwid.Trim() == "1";

            // Starlight-start
            if (TryGetVar("STARLIGHT_API_SERVER", out var starlightApi))
                StarlightApi = starlightApi;

            if (TryGetVar("STARLIGHT_AUTH_TOKEN", out var discordToken))
                DiscordToken = discordToken;
            // Starlight-end

            static bool TryGetVar(string var, [NotNullWhen(true)] out string? val)
            {
                val = Environment.GetEnvironmentVariable(var);
                return val != null;
            }
        }
    }
}
