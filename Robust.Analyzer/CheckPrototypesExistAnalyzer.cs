using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Prototypes;

namespace Robust.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CheckPrototypesExistAnalyzer : DiagnosticAnalyzer
    {
        // Robust Toolbox Error #1
        public const string DiagnosticId = "RTE001";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            // So long as the compilation start action runs exactly once, then this is safe
            // TODO: should double-check that, the API docs don't seem to guarantee it
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Gets the representation of IEntityManager at runtime
                var entityManagerInterfaces = compilationContext.Compilation.References
                    .Select(compilationContext.Compilation.GetAssemblyOrModuleSymbol)
                    .OfType<IAssemblySymbol>()
                    .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("Robust.Shared.Interfaces.GameObjects.IEntityManager"))
                    .Where(t => t != null);
                INamedTypeSymbol entityManagerInterface = entityManagerInterfaces.FirstOrDefault();
                if (entityManagerInterface == null)
                {
                    throw new Exception("Could not load the IEntityManager interface.");
                }

                var prototypeManager = LoadPrototypes(compilationContext);

                // We pass the prototype manager in, rather than loading it from AnalyzerIoC,
                // because these actions might run in different threads
                compilationContext.RegisterOperationAction(operationContext => AnalyzeSpawnEntity(operationContext, entityManagerInterface, prototypeManager), new OperationKind[] { OperationKind.Invocation });
            });
        }

        private static IPrototypeManager LoadPrototypes(CompilationStartAnalysisContext compilationContext)
        {
            // Load the prototypes fed in to AdditionalFiles at compile-time
            // TODO: think about if this is the best design choice
            AnalyzerIoC.RegisterIoC();

            var reflectionManager = AnalyzerIoC.Resolve<IReflectionManager>();

            reflectionManager.LoadAssemblies(new Assembly[] { typeof(IPrototypeManager).Assembly });

            var prototypeManager = AnalyzerIoC.Resolve<IPrototypeManager>();

            foreach (var file in compilationContext.Options.AdditionalFiles)
            {
                if (Path.GetExtension(file.Path) != ".yaml")
                {
                    continue;
                }
                prototypeManager.LoadFromStream(new StringReader(file.GetText(compilationContext.CancellationToken).ToString()));
            }
            prototypeManager.Resync();

            return prototypeManager;
        }

        private static bool IsPrototype(string prototypeName, IPrototypeManager prototypeManager)
        {
            try
            {
                return prototypeManager.HasIndex<EntityPrototype>(prototypeName);
            }
            catch (UnknownPrototypeException e)
            {
                throw new Exception(String.Format("Exception prevented querying of entity prototypes for existence of \"{0}\".", prototypeName), e);
            }
        }

        private static void AnalyzeSpawnEntity(OperationAnalysisContext context, INamedTypeSymbol entityManagerInterface, IPrototypeManager prototypeManager)
        {
            // Safe cast, we only target method calls
            var invocation = (IInvocationOperation)context.Operation;

            // Ignore everything that isn't this method call
            if (invocation.TargetMethod.Name != "SpawnEntity")
            {
                return;
            }

            // Try and figure out if it's being called on an entity manager
            if (!(invocation.TargetMethod.ReceiverType is INamedTypeSymbol receiverType))
            {
                return;
            }

            // The call could be on an `IEntityManager`, or something implementing that
            // interface
            if (!(receiverType.Equals(entityManagerInterface) || !receiverType.AllInterfaces.Contains(entityManagerInterface)))
            {
                return;
            }

            // Fish out the first argument - if it doesn't exist, ignore the call
            var firstArgument = invocation.Arguments.FirstOrDefault();
            if (firstArgument == null)
            {
                return;
            }

            var argLiteral = firstArgument.Value.ConstantValue;
            if (!argLiteral.HasValue || !(argLiteral.Value is string prototypeName))
            {
                return;
            }

            // Now we finally have a string which should refer to a prototype - so
            // check it is one
            if (!IsPrototype(prototypeName, prototypeManager))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), prototypeName));
            }
        }
    }
}
