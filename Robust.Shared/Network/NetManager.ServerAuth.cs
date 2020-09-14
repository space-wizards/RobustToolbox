using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Robust.Shared.Log;

namespace Robust.Shared.Network
{
    partial class NetManager
    {
        private static readonly TimeSpan AuthServerTimeout = TimeSpan.FromSeconds(10);

        private ECDsa? _authServerPubKey;
        private RSA? _authRsaPrivateKey;
        private byte[]? _authRsaPublicKey;

        private void GeneratePrivateKey()
        {
            _authRsaPrivateKey = RSA.Create(2048);
            _authRsaPublicKey = _authRsaPrivateKey.ExportRSAPublicKey();
        }

        public async Task FetchAuthKey()
        {
            var authServerBaseUrl = _config.GetCVar<string>("auth.server");
            var authUrl = authServerBaseUrl + "api/keys/sessionSign";

            var client = new HttpClient {Timeout = AuthServerTimeout};
            var keyResp = await client.GetAsync(authUrl);

            var keyJson = JsonConvert.DeserializeObject<SessionSignKeyResponse>(
                await keyResp.Content.ReadAsStringAsync());

            if (keyJson.Type != "secp256r1")
            {
                Logger.ErrorS("auth", "Unsupported key type: {0}", keyJson.Type);
                return;
            }

            var keyBytes = Convert.FromBase64String(keyJson.Key);
            _authServerPubKey = ECDsa.Create();
            _authServerPubKey.ImportSubjectPublicKeyInfo(keyBytes, out _);
        }

        public sealed class SessionSignKeyResponse
        {
            public string Key { get; set; } = default!;
            public string Type { get; set; } = default!;
        }
    }
}
