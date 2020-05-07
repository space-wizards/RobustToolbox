using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Analyzer for checking string literals correspond to a known prototype name.
    /// </summary>
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
                // IMPORTANT: There are two levels of reflection going on here - one for
                // the compilation in progress, and another for *this code* as it runs
                // in *this assembly*.
                //
                // There are several things that need to happen at the level of the compilation
                //   - we need to find the compilation-level version of the [PrototypeName(...)]
                //     attribute, to check for its usage in methods in referenced assemblies
                //   - we need to load all the assemblies referenced at the compilation level into
                //     the prototype and reflection managers, to let them find all the prototype
                //     symbols necessary.
                var assemblySymbols = compilationContext.Compilation.References
                    .Select(compilationContext.Compilation.GetAssemblyOrModuleSymbol)
                    .OfType<IAssemblySymbol>();

                var prototypeNameAttributes = assemblySymbols
                    .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName("Robust.Shared.Prototypes.PrototypeNameAttribute"))
                    .Where(t => t != null);
                INamedTypeSymbol prototypeNameAttribute = prototypeNameAttributes.FirstOrDefault();
                if (prototypeNameAttribute == null)
                {
                    throw new Exception("Could not load the PrototypeName attribute.");
                }

                // Get the methods for which at least one argument has a [PrototypeName] attribute
                // then remember them - this speeds up later code
                var usageFinder = new PrototypeNameUsageFinderVisitor(prototypeNameAttribute);
                foreach (var assemblySymbol in assemblySymbols)
                {
                    usageFinder.Visit(assemblySymbol);
                }

                var prototypeManager = LoadPrototypes(compilationContext);

                // We pass the prototype manager in, rather than loading it from AnalyzerIoC,
                // because these actions might run in different threads
                compilationContext.RegisterOperationAction(operationContext => AnalyzePrototypeNameArguments(operationContext, prototypeNameAttribute, usageFinder.RelevantMethods, prototypeManager), new OperationKind[] { OperationKind.Invocation });
            });
        }

        /// <summary>
        /// Symbol graph visitor that pre-loads all the annotated method symbols
        /// </summary>
        internal class PrototypeNameUsageFinderVisitor : SymbolVisitor
        {

            private readonly INamedTypeSymbol _prototypeNameAttribute;
            private List<IMethodSymbol> _relevantMethods = new List<IMethodSymbol>();

            public List<IMethodSymbol> RelevantMethods => _relevantMethods;

            public PrototypeNameUsageFinderVisitor(INamedTypeSymbol prototypeNameAttribute)
            {
                _prototypeNameAttribute = prototypeNameAttribute;
            }

            public override void VisitAssembly(IAssemblySymbol symbol)
            {
                foreach (var memberSymbol in symbol.GlobalNamespace.GetMembers())
                {
                    memberSymbol.Accept(this);
                }
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (var memberSymbol in symbol.GetMembers())
                {
                    memberSymbol.Accept(this);
                }
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                foreach (var memberSymbol in symbol.GetMembers())
                {
                    memberSymbol.Accept(this);
                }
            }

            public override void VisitMethod(IMethodSymbol symbol)
            {
                if (symbol.Parameters.Any(parameterSymbol => parameterSymbol.GetAttributes().Any(attributeData => attributeData.AttributeClass.Equals(_prototypeNameAttribute))))
                {
                    _relevantMethods.Add(symbol);
                }

                base.VisitMethod(symbol);
            }
        }

        // This warning is spurious - the context does have actions added above
#pragma warning disable RS1012
        private static IPrototypeManager LoadPrototypes(CompilationStartAnalysisContext compilationContext)
        {
            // TODO: Get this array from the analyzer config
            AnalyzerIoC.RegisterIoC(new[] { "Robust.Shared.", "Robust.Client." });

            var reflectionManager = AnalyzerIoC.Resolve<IReflectionManager>();

            // Load the assemblies being used for the compilation into the reflection manager
            // so that the analyzer can see what the project being compiled sees.
            var assemblies = compilationContext.Compilation.References
                    .Select(compilationContext.Compilation.GetAssemblyOrModuleSymbol)
                    .OfType<IAssemblySymbol>()
                    .Select(assemblySymbol => Assembly.Load(assemblySymbol.Identity.ToString()));

            reflectionManager.LoadAssemblies(assemblies);

            var prototypeManager = AnalyzerIoC.Resolve<IPrototypeManager>();

            // TODO: Check these from the analyzer config
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
# pragma warning restore RS1012

        private static bool IsPrototype(string prototypeName, string prototypeType, IPrototypeManager prototypeManager)
        {
            try
            {
                return prototypeManager.HasIndex(prototypeType, prototypeName);
            }
            catch (UnknownPrototypeException e)
            {
                throw new Exception(
                    String.Format(
                        "Exception prevented querying of \"{0}\" prototypes for existence of \"{1}\".",
                        prototypeType, prototypeName),
                    e);
            }
        }

        /// <summary>
        /// Check any method arguments which are [PrototypeName]s to see if they exist
        /// </summary>
        /// <param name="context"></param>
        /// <param name="prototypeNameAttribute">The symbol of the [PrototypeName] attribute.</param>
        /// <param name="methodsToCheck">A collection of methods which are known to have a [PrototypeName] argument.</param>
        /// <param name="prototypeManager"></param>
        private static void AnalyzePrototypeNameArguments(
            OperationAnalysisContext context,
            INamedTypeSymbol prototypeNameAttribute,
            IEnumerable<IMethodSymbol> methodsToCheck,
            IPrototypeManager prototypeManager)
        {
            // Safe cast, we only target method calls
            var invocation = (IInvocationOperation)context.Operation;

            // Quick filter in advance - because we pre-loaded all the method symbols
            // with some [PrototypeName] attribute on an argument, we know all the rest
            // are uninteresting
            if (!methodsToCheck.Any(methodSymbol => methodSymbol.Equals(invocation.TargetMethod)))
            {
                return;
            }

            foreach (var (argument, index) in invocation.Arguments.Select((arg, i) => (arg, i)))
            {
                var prototypeNameAttributeData = invocation.TargetMethod.Parameters[index].GetAttributes().FirstOrDefault(attributeData => attributeData.AttributeClass.Equals(prototypeNameAttribute));
                if (prototypeNameAttributeData == null)
                {
                    continue;
                }

                var firstAttributeArg = prototypeNameAttributeData.ConstructorArguments.FirstOrDefault();
                // First argument is the prototype type
                if (firstAttributeArg.IsNull || !(firstAttributeArg.Value is string prototypeType))
                {
                    throw new Exception("Invalid use of 'PrototypeName' attribute - expected the first argument to be a string literal.");
                }

                var argLiteral = argument.Value.ConstantValue;
                if (!argLiteral.HasValue || !(argLiteral.Value is string prototypeName))
                {
                    continue;
                }

                // Now we finally have a string which should refer to a prototype - so
                // check it is one
                if (!IsPrototype(prototypeName, prototypeType, prototypeManager))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), prototypeType, prototypeName));
                }
            }

            
        }

    }
}
