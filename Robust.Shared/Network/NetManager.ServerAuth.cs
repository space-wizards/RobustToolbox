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
        private const int VerifyTokenSize = 4; // Literally just what MC does idk.

        private RSA? _authRsaPrivateKey;

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

        private byte[] SADecryptSharedSecret(byte[] keyData)
        {
            DebugTools.AssertNotNull(_authRsaPrivateKey);

            return _authRsaPrivateKey!.Decrypt(keyData, RSAEncryptionPadding.OaepSHA256);
        }

        private sealed class HasJoinedResponse
        {
            public bool IsValid;
            public HasJoinedUserData? UserData;

            public sealed class HasJoinedUserData
            {
                public string UserName = default!;
                public Guid UserId = default!;
            }
        }
    }
}
