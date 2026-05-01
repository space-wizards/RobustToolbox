using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using Robust.Roslyn.Shared;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class AdminLogSemanticAuthoringFixerTest
{
    private const string Support = AdminLogSemanticAuthoringAnalyzerTestSupport.Source;

    private static Task Verify(string code, string fixedCode, params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<AdminLogSemanticAuthoringAnalyzer, AdminLogSemanticAuthoringFixer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { Support, code },
            },
            FixedState =
            {
                Sources = { Support, fixedCode },
            },
        };

        test.TestState.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    [Test]
    public async Task RemovesInlineRedundantEntitiesArgument()
    {
        const string code = """
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;

                public void Run(EntityUid user, EntityUid target, EntityUid tool)
                {
                    _adminLogger.AddStructured(LogType.Unknown, LogImpact.Low,
                        $"{user:actor} hit {target:victim} with {tool:tool}",
                        {|#0:entities: new[]
                        {
                            new AdminLogEntityRef(user, AdminLogEntityRole.Actor),
                            new AdminLogEntityRef(target, AdminLogEntityRole.Victim),
                            new AdminLogEntityRef(tool, AdminLogEntityRole.Tool),
                        }|});
                }
            }
            """;

        const string fixedCode = """
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;

                public void Run(EntityUid user, EntityUid target, EntityUid tool)
                {
                    _adminLogger.AddStructured(LogType.Unknown, LogImpact.Low,
                        $"{user:actor} hit {target:victim} with {tool:tool}");
                }
            }
            """;

        await Verify(code, fixedCode,
            new DiagnosticResult(Diagnostics.IdAdminLogRedundantExplicitEntities, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning).WithLocation(0));
    }
}

public static class AdminLogSemanticAuthoringAnalyzerTestSupport
{
    public const string Source = """
        using System;
        using System.Collections.Generic;

        namespace Robust.Shared.GameObjects
        {
            public readonly struct EntityUid;
            public readonly struct NetEntity;
        }

        namespace System.Runtime.CompilerServices
        {
            public sealed class IsExternalInit { }
        }

        namespace Robust.Shared.Player
        {
            public interface ICommonSession { }
        }

        namespace Content.Shared.Database
        {
            public enum LogType { Unknown, Ingestion, ForceFeed, InteractActivate }
            public enum LogImpact { Low, Medium }
            public enum AdminLogEntityRole { Actor, Target, Tool, Victim, Subject, Other }
        }

        namespace Content.Shared.Administration.Logs
        {
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public readonly record struct AdminLogEntityRef(EntityUid Entity, AdminLogEntityRole Role, string? PrototypeId = null, string? EntityName = null);
            public readonly record struct AdminLogExplicitSemantics(
                IReadOnlyCollection<Guid>? Players = null,
                IReadOnlyCollection<AdminLogEntityRef>? Entities = null,
                IReadOnlyDictionary<Guid, AdminLogEntityRole>? PlayerRoles = null);

            public static class AdminLogHelpers
            {
                public static AdminLogExplicitSemantics GetActorVictimToolSemantics(object player, EntityUid actor, EntityUid victim, EntityUid tool) => default;
            }

            public interface ISharedAdminLogManager
            {
                void AddStructured(
                    LogType type,
                    LogImpact impact,
                    string handler,
                    object? payload = null,
                    IReadOnlyCollection<Guid>? players = null,
                    IReadOnlyCollection<AdminLogEntityRef>? entities = null,
                    IReadOnlyDictionary<Guid, AdminLogEntityRole>? playerRoles = null);
            }
        }
        """;
}
