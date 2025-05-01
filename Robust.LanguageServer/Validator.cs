using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.LanguageServer;

public sealed class Validator
{
    [Dependency] private readonly IPrototypeManagerInternal _protoMan = default!;

    public void ValidateSingleFile(string filePath)
    {
        // var allErrors = protoMan.ValidateDirectory(new(@"/Prototypes"));
        // string filePath = "/Users/ciaran/code/ss14/space-station-14/Resources/Prototypes/Reagents/medicine.yml";
        // string filePath = "/Users/ciaran/code/ss14/space-station-14/Resources/Prototypes/Flavors/flavors.yml";
        using TextReader reader = new StreamReader(filePath);
        var allErrors = _protoMan.AnalyzeSingleFile(reader, out _, out _, filePath);

        // var allErrors = _protoMan.ValidateDirectory(new(@"/Prototypes"), out _);
        // foreach (var (file, errors) in allErrors)
        // {
        //     Console.WriteLine($"File: {file} - {errors.Count} errors");
        // }
        Console.WriteLine($"allErrors: {allErrors.Count}");

        foreach (var errorNode in allErrors)
        {
            Console.Error.WriteLine($"Error in file: {filePath}");

            Console.Error.WriteLine(
                    $"* {errorNode.Node} - {errorNode.ErrorReason} - {errorNode.AlwaysRelevant} - {errorNode.Node.Start} -> {errorNode.Node.End}");
        }
    }
}
