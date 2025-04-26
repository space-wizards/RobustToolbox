using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.LanguageServer;

internal static class Program
{
    static void Main(string[] args)
    {
        var deps = IoCManager.InitThread();
        IoCManager.Register<Validator>();

        Loader.Init(deps);

        // // var allErrors = protoMan.ValidateDirectory(new(@"/Prototypes"));
        // string filePath = "/Users/ciaran/code/ss14/space-station-14/Resources/Prototypes/Reagents/medicine.yml";
        // // string filePath = "/Users/ciaran/code/ss14/space-station-14/Resources/Prototypes/Flavors/flavors.yml";
        // using TextReader reader = new StreamReader(filePath);
        // // var allErrors = protoMan.ValidateSingleFile(reader, out _, filePath);
        //
        // var protoMan = IoCManager.Resolve<IPrototypeManager>();
        //
        // var allErrors = protoMan.ValidateDirectory(new(@"/Prototypes"), out _);
        // // foreach (var (file, errors) in allErrors)
        // // {
        // //     Console.WriteLine($"File: {file} - {errors.Count} errors");
        // // }
        // Console.WriteLine($"allErrors: {allErrors.Count}");
        //
        // foreach (var (path, nodeList) in allErrors)
        // {
        //     Console.Error.WriteLine($"Error in file: {path}");
        //
        //     foreach (var node in nodeList)
        //     {
        //         Console.Error.WriteLine(
        //             $"* {node.Node} - {node.ErrorReason} - {node.AlwaysRelevant} - {node.Node.Start} -> {node.Node.End}");
        //     }
        // }

        var validator = IoCManager.Resolve<Validator>();
        validator.ValidateSingleFile("/Users/ciaran/code/ss14/space-station-14/Resources/Prototypes/Reagents/medicine.yml");
    }
}
