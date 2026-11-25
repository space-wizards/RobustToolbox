using System.Text;
using Robust.Shared.Network.Transfer;

namespace Robust.Shared.Upload;

/// <summary>
///     Wire format for prototype dumps sent over <see cref="ITransferManager"/>.
///     The stream starts with a little-endian ack key, prototype count and sequential flag,
///     followed by length-prefixed prototypes.
/// </summary>
internal static class PrototypeDumpFormat
{
    public const int LengthSize = sizeof(int);
    public const int HeaderSize = LengthSize * 3;

    public static long GetPayloadSize(long currentBytes, string data)
    {
        return currentBytes + LengthSize + Encoding.UTF8.GetByteCount(data);
    }
}
