using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared;

[Parallelizable(ParallelScope.All)]
internal sealed class TestRobustSerializerHash : RobustIntegrationTest
{
    /// <summary>
    /// Test that the serializer hash on client and server matches.
    /// </summary>
    [Test]
    public async Task Test()
    {
        var server = StartServer();
        var client = StartClient();

        var manifestServerStream = new MemoryStream();
        var manifestClientStream = new MemoryStream();

        await server.WaitPost(() =>
        {
            var serializer = (RobustSerializer) IoCManager.Resolve<IRobustSerializer>();
            serializer.GetHashManifest(manifestServerStream, writeNewline: true);
        });

        await client.WaitPost(() =>
        {
            var serializer = (RobustSerializer) IoCManager.Resolve<IRobustSerializer>();
            serializer.GetHashManifest(manifestClientStream, writeNewline: true);
        });

        var manifestServer = Encoding.UTF8.GetString(manifestServerStream.AsSpan());
        var manifestClient = Encoding.UTF8.GetString(manifestClientStream.AsSpan());

        Assert.That(manifestServer, NUnit.Framework.Is.EqualTo(manifestClient));
    }
}
