using NUnit.Framework;
using Robust.Packaging.AssetProcessing;

namespace Robust.Packaging.Tests;

/// <summary>
/// A simple asset pass that stores all files it receives, for introspection by tests.
/// </summary>
public sealed class AssetPassTestCollector : AssetPass
{
    public readonly List<AssetFile> Files = [];

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        lock (Files)
        {
            Files.Add(file);
        }

        return AssetFileAcceptResult.Consumed;
    }

    /// <summary>
    /// Assert that the only files collected are an exact set of test files.
    /// </summary>
    public void AssertTextFiles(params (string path, string data)[] files)
    {
        lock (Files)
        {
            Assert.That(Files, Has.Count.EqualTo(files.Length));

            Assert.Multiple(() =>
            {
                foreach (var file in files)
                {
                    var matchingFile = Files.SingleOrDefault(f => f.Path == file.path);
                    if (matchingFile == null)
                    {
                        Assert.Fail($"Unable to find file {file.path}");
                        continue;
                    }

                    using var fileData = matchingFile.Open();
                    using var reader = new StreamReader(fileData);
                    var fileText = reader.ReadToEnd();

                    Assert.That(fileText, Is.EqualTo(file.data));
                }
            });
        }
    }
}
