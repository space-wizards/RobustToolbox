using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Lidgren.Network;
using static SpaceWizards.Sodium.Interop.Libsodium;

namespace Robust.Shared.Network;

internal sealed class NetEncryption
{
    // Use a counter for nonces. The counter is 64-bit, I will be impressed if you ever manage to run it out.
    // 64-bit counter (incl over the wire) is fine, don't need the whole 192-bit.
    // Server starts at 0, client starts at 1, increment by two.
    // This means server and client never use eachother's nonces (one side odd, one side even).
    // Keep in mind, our keys are only valid for one session.
    private ulong _nonce;
    private readonly byte[] _key;

    public NetEncryption(byte[] key, bool isServer)
    {
        if (key.Length != crypto_aead_xchacha20poly1305_ietf_KEYBYTES)
            throw new ArgumentException("Key is of wrong size!");

        _nonce = isServer ? 0ul : 1ul;
        _key = key;
    }

    public unsafe void Encrypt(NetOutgoingMessage message)
    {
        var nonce = Interlocked.Add(ref _nonce, 2);

        var lengthBytes = message.LengthBytes;
        var encryptedSize = checked((int)(crypto_aead_xchacha20poly1305_ietf_ABYTES + lengthBytes + sizeof(ulong)));

        var data = message.Data.AsSpan(0, lengthBytes);

        Span<byte> plaintext;
        Span<byte> ciphertext;
        byte[]? returnPool = null;

        if (message.Data.Length >= encryptedSize)
        {
            // Since we have enough space in the existing message data,
            // we copy plaintext to an ArrayPool buffer and write ciphertext into existing message.
            // This avoids an allocation at the cost of an extra copy operation.

            returnPool = ArrayPool<byte>.Shared.Rent(lengthBytes);
            plaintext = returnPool.AsSpan(0, lengthBytes);
            data.CopyTo(plaintext);

            ciphertext = message.Data.AsSpan(0, encryptedSize);
        }
        else
        {
            // Otherwise, an allocation is unavoidable,
            // so we swap the data buffer in the message with a fresh allocation and don't do an extra copy of the data.

            plaintext = data;
            ciphertext = message.Data = new byte[encryptedSize];
        }

        // TODO: this is probably broken for big-endian machines.
        Span<byte> nonceData = stackalloc byte[(int)crypto_aead_xchacha20poly1305_ietf_NPUBBYTES];
        nonceData.Fill(0);
        MemoryMarshal.Write(nonceData, ref nonce);
        MemoryMarshal.Write(ciphertext, ref nonce);

        fixed (byte* cPtr = ciphertext[sizeof(ulong)..])
        fixed (byte* pPtr = plaintext)
        fixed (byte* kPtr = _key)
        fixed (byte* nPtr = nonceData)
        {
            crypto_aead_xchacha20poly1305_ietf_encrypt(
                // ciphertext
                cPtr, null,
                // plaintext
                pPtr, (ulong)plaintext.Length,
                // additional data (unused)
                null, 0,
                // always null
                null,
                // nonce
                nPtr,
                // key
                kPtr);
        }

        message.LengthBytes = encryptedSize;

        if (returnPool != null)
            ArrayPool<byte>.Shared.Return(returnPool);
    }

    public unsafe void Decrypt(NetIncomingMessage message)
    {
        var nonce = message.ReadUInt64();
        var cipherText = message.Data.AsSpan(sizeof(ulong), message.LengthBytes-sizeof(ulong));

        var buffer = ArrayPool<byte>.Shared.Rent(cipherText.Length);
        cipherText.CopyTo(buffer);

        try
        {
            // TODO: this is probably broken for big-endian machines.
            Span<byte> nonceData = stackalloc byte[(int)crypto_aead_xchacha20poly1305_ietf_NPUBBYTES];
            nonceData.Fill(0);
            MemoryMarshal.Write(nonceData, ref nonce);

            fixed (byte* mPtr = message.Data)
            fixed (byte* cPtr = buffer)
            fixed (byte* kPtr = _key)
            fixed (byte* nPtr = nonceData)
            {
                ulong mlen;
                var result = crypto_aead_xchacha20poly1305_ietf_decrypt(
                    // plaintext
                    mPtr, &mlen,
                    // always null
                    null,
                    // ciphertext
                    cPtr, (ulong)cipherText.Length,
                    // additional data (unused)
                    null, 0,
                    // nonce
                    nPtr,
                    // key
                    kPtr);

                if (result == -1)
                    throw new InvalidDataException("Decryption verification failed");

                message.Position = 0;
                message.LengthBytes = (int) mlen;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
