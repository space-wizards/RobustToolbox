using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NetSerializer;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting
{
    internal sealed class IntegrationMappedStringSerializer : IRobustMappedStringSerializer
    {
        [Dependency] private readonly INetManager _net = default!;

        public bool Locked => false;

        public ITypeSerializer TypeSerializer { get; } = new TypeSerializerImpl();

        public Task Handshake(INetChannel channel)
        {
            var message = new MsgMapStrServerHandshake();
            message.Hash = _hash;
            _net.ServerSendMessage(message, channel);

            return Task.CompletedTask;
        }

        private readonly byte[] _hash = new byte[64];
        public ReadOnlySpan<byte> MappedStringsHash => _hash;

        public bool EnableCaching { get; set; }

        public void AddString(string str)
        {
            // Nada.
        }

        public void AddStrings(Assembly asm)
        {
            // Nada.
        }

        public void AddStrings(YamlStream yaml)
        {
            // Nada.
        }

        public void AddStrings(DataNode dataNode)
        {
            // Nada.
        }

        public void AddStrings(IEnumerable<string> strings)
        {
            // Nada.
        }

        public event Action? ClientHandshakeComplete;

        public void LockStrings()
        {
            // Nada.
        }

        public void Initialize()
        {
            _net.RegisterNetMessage<MsgMapStrServerHandshake>(HandleServerHandshake, NetMessageAccept.Client);
        }

        private void HandleServerHandshake(MsgMapStrServerHandshake message)
        {
            ClientHandshakeComplete?.Invoke();
        }

        public (byte[] mapHash, byte[] package) GeneratePackage()
        {
            return (Array.Empty<byte>(), Array.Empty<byte>());
        }

        public void SetPackage(byte[] hash, byte[] package)
        {
            throw new NotSupportedException();
        }

        private sealed class TypeSerializerImpl : IStaticTypeSerializer
        {
            public bool Handles(Type type)
            {
                return false;
            }

            public IEnumerable<Type> GetSubtypes(Type type)
            {
                throw new NotSupportedException();
            }

            public MethodInfo GetStaticWriter(Type type)
            {
                throw new NotSupportedException();
            }

            public MethodInfo GetStaticReader(Type type)
            {
                throw new NotSupportedException();
            }
        }
    }
}
