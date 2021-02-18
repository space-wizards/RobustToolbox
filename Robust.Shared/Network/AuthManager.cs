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
        string? Token { get; set; }
        string? PubKey { get; set; }

        void LoadFromEnv();
    }

    internal sealed class AuthManager : IAuthManager
    {
        public const string DefaultAuthServer = "https://central.spacestation14.io/auth/";

        public NetUserId? UserId { get; set; }
        public string? Server { get; set; } = DefaultAuthServer;
        public string? Token { get; set; }
        public string? PubKey { get; set; }

        public void LoadFromEnv()
        {
            if (TryGetVar("ROBUST_AUTH_SERVER", out var server))
            {
                Server = server;
            }

            if (TryGetVar("ROBUST_AUTH_USERID", out var userId))
            {
                UserId = new NetUserId(Guid.Parse(userId));
            }

            if (TryGetVar("ROBUST_AUTH_PUBKEY", out var pubKey))
            {
                PubKey = pubKey;
            }

            if (TryGetVar("ROBUST_AUTH_TOKEN", out var token))
            {
                Token = token;
            }

            static bool TryGetVar(string var, [NotNullWhen(true)] out string? val)
            {
                val = Environment.GetEnvironmentVariable(var);
                return val != null;
            }
        }
    }
}
