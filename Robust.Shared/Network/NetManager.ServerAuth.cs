using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Network
{
    partial class NetManager
    {
        private const int RsaKeySize = 2048;
        private static readonly TimeSpan AuthServerTimeout = TimeSpan.FromSeconds(10);

        private ECDsa? _authServerPubKey;
        private RSA? _authRsaPrivateKey;
        private TokenValidationParameters? _authTokenValidationParams;
        private readonly JwtSecurityTokenHandler _authTokenHandler = new JwtSecurityTokenHandler();
        private Task? _authKeysTask;

        public byte[]? RsaPublicKey { get; private set; }
        public AuthMode Auth { get; private set; }

        private void SAGenerateRsaKeys()
        {
            _authRsaPrivateKey = RSA.Create(RsaKeySize);
            RsaPublicKey = _authRsaPrivateKey.ExportRSAPublicKey();

            /*
            Logger.DebugS("auth", "Private RSA key is {0}",
                Convert.ToBase64String(_authRsaPrivateKey.ExportRSAPrivateKey()));
            */
            Logger.DebugS("auth", "Public RSA key is {0}", Convert.ToBase64String(RsaPublicKey));
        }

        public async Task SAFetchAuthKey()
        {
            try
            {
                var authServerBaseUrl = _config.GetCVar<string>("auth.server");
                var authUrl = authServerBaseUrl + "api/keys/sessionSign";

                var client = new HttpClient {Timeout = AuthServerTimeout};

                async Task<string> GetKeyJson()
                {
                    // Do this in a separate method that does ConfigureAwait(false).
                    // The synchronization context only starts getting pumped when the server finishes starting,
                    // so if we don't do any .ConfigureAwait(false), the actual request to the auth server
                    // will stay open the entire time the server is starting.
                    // This isn't very nice for the auth server so if we .ConfigureAwait(false)
                    // we can get it over quickly, and then do it in a separate method so we return to the main thread.
                    var sw = Stopwatch.StartNew();
                    var keyResp = await client.GetAsync(authUrl).ConfigureAwait(false);
                    keyResp.EnsureSuccessStatusCode();

                    var msg = await keyResp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return msg;
                }

                var keyJson = JsonConvert.DeserializeObject<SessionSignKeyResponse>(await GetKeyJson());

                if (keyJson.Type != "secp256r1")
                {
                    Logger.ErrorS("auth", "Unsupported key type: {0}", keyJson.Type);
                    return;
                }

                var keyBytes = Convert.FromBase64String(keyJson.Key);
                _authServerPubKey = ECDsa.Create();
                _authServerPubKey.ImportSubjectPublicKeyInfo(keyBytes, out _);

                var sha = SHA256.Create();
                var hash = sha.ComputeHash(RsaPublicKey!);

                _authTokenValidationParams = new TokenValidationParameters
                {
                    ValidAudience = Convert.ToBase64String(hash),
                    IssuerSigningKey = new ECDsaSecurityKey(_authServerPubKey),
                    ValidateIssuer = false
                };

                Logger.DebugS("auth", "Fetched public key from auth server: {0}", keyJson.Key);
            }
            catch (Exception e)
            {
                Logger.ErrorS("auth", "Exception while fetching signing key from auth server:\n{0}", e);
                _authBroken = true;
                if (Auth == AuthMode.Required)
                {
                    Logger.FatalS("auth",
                        "Authentication is BROKEN. NOBODY will be able to connect to the server!!!");
                }
            }
        }

        private byte[] SADecryptAuthToken(byte[] data)
        {
            DebugTools.Assert(_authRsaPrivateKey != null);

            var sa = Aes.Create();

            var keyEx = new byte[_authRsaPrivateKey!.KeySize >> 3];
            data.AsSpan(..keyEx.Length).CopyTo(keyEx);
            var def = new RSAPKCS1KeyExchangeDeformatter(_authRsaPrivateKey);
            var key = def.DecryptKeyExchange(keyEx);

            var iv = new byte[sa.IV.Length];
            var keyPlusIvLength = keyEx.Length + iv.Length;
            data.AsSpan(keyEx.Length..keyPlusIvLength).CopyTo(iv);

            var decrypt = sa.CreateDecryptor(key, iv);
            return decrypt.TransformFinalBlock(data,
                keyEx.Length + iv.Length,
                data.Length - keyPlusIvLength);
        }

        private (string userName, Guid userId)? SAReadToken(byte[] tokenData)
        {
            var text = Encoding.UTF8.GetString(tokenData);
            ClaimsPrincipal principal;
            try
            {
                principal = _authTokenHandler!.ValidateToken(text, _authTokenValidationParams, out _);
            }
            catch (SecurityTokenException e)
            {
                Logger.InfoS("auth", "Failed to validate security token:\n{0}", e);
                // Token failed to validate.
                return null;
            }

            var sub = principal.Claims.FirstOrDefault(p => p.Type == ClaimTypes.NameIdentifier)?.Value;
            var name = principal.Claims.FirstOrDefault(p => p.Type == "name")?.Value;

            if (sub == null || name == null)
            {
                // Bad token??
                return null;
            }

            return (name, new Guid(sub));
        }

        private byte[] SADecryptAesKey(byte[] keyData)
        {
            DebugTools.AssertNotNull(_authRsaPrivateKey);

            return _authRsaPrivateKey!.Decrypt(keyData, RSAEncryptionPadding.OaepSHA256);
        }

        private sealed class SessionSignKeyResponse
        {
            public string Key { get; set; } = default!;
            public string Type { get; set; } = default!;
        }
    }
}
