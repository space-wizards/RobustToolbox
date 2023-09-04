using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using System;

namespace Robust.Client.Serialization;

[InjectDependencies]
internal sealed partial class ClientRobustSerializer : RobustSerializer, IClientRobustSerializer
{
    [Dependency] private IBaseClient _client = default!;

    public void SetStringSerializerPackage(byte[] hash, byte[] package)
    {
        if (_client.RunLevel != ClientRunLevel.SinglePlayerGame)
            throw new NotSupportedException("Directly setting string serializer data is only supported in single-player games.");

        MappedStringSerializer.SetPackage(hash, package);
    }
}
