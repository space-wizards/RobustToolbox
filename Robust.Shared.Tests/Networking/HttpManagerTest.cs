using NUnit.Framework;
using Robust.Shared.Network;

namespace Robust.Shared.Tests.Networking;

[TestFixture]
[TestOf(typeof(HttpManager))]
public sealed class HttpManagerTest
{
    [Test]
    [TestCase("http://www.google.com")]
    [TestCase("https://www.google.com")]
    [TestCase("http://google.com")]
    [TestCase("https://google.com")]
    public void TestNoError(string url)
    {
        var manager = new HttpManager();
        var uri = new Uri(url);
        var stream = new MemoryStream();
        Assert.DoesNotThrowAsync(() => manager.GetStreamAsync(uri));
        Assert.DoesNotThrowAsync(() => manager.GetStringAsync(uri));
        Assert.DoesNotThrowAsync(() => manager.GetByteArrayAsync(uri));
        Assert.DoesNotThrowAsync(() => manager.CopyToAsync(uri, stream));
    }

    [Test]
    [TestCase("http://192.168.0.0")]
    [TestCase("http://192.168.0.1")]
    [TestCase("http://192.168.255.255")]
    [TestCase("http://10.0.0.0")]
    [TestCase("http://10.255.255.255")]
    [TestCase("http://172.16.0.0")]
    [TestCase("http://172.31.255.255")]
    public void TestLocalIPError(string url)
    {
        var manager = new HttpManager();
        var uri = new Uri(url);
        Assert.ThrowsAsync<InvalidAddressException>(() => manager.ThrowIfLocalUri(uri));
    }
}
