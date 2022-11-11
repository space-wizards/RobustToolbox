using System;
using System.Text;

namespace Robust.Shared.Utility
{
    internal static class Base64Helpers
    {
        /// <summary>
        /// Converts a byte array such as a hash to a Base64 representation that is URL safe.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A base64url string form of the byte array.</returns>
        public static string ConvertToBase64Url(byte[]? data)
        {
            return data == null ? "" : ConvertToBase64Url(Convert.ToBase64String(data));
        }

        /// <summary>
        /// Converts a a Base64 string to one that is URL safe.
        /// </summary>
        /// <returns>A base64url formed string.</returns>
        public static string ConvertToBase64Url(string b64Str)
        {
            if (b64Str is null)
            {
                throw new ArgumentNullException(nameof(b64Str));
            }

            var cut = b64Str[^1] == '=' ? b64Str[^2] == '=' ? 2 : 1 : 0;
            b64Str = new StringBuilder(b64Str).Replace('+', '-').Replace('/', '_').ToString(0, b64Str.Length - cut);
            return b64Str;
        }

        /// <summary>
        /// Converts a URL-safe Base64 string into a byte array.
        /// </summary>
        /// <param name="s">A base64url formed string.</param>
        /// <returns>The represented byte array.</returns>
        public static byte[] ConvertFromBase64Url(string s)
        {
            var l = s.Length % 3;
            var sb = new StringBuilder(s);
            sb.Replace('-', '+').Replace('_', '/');
            for (var i = 0; i < l; ++i)
            {
                sb.Append('=');
            }

            s = sb.ToString();
            return Convert.FromBase64String(s);
        }

    }
}
