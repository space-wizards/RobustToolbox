using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using Robust.Roslyn.Shared;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Robust.Analyzers.AdminLogSemanticAuthoringAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
[TestOf(typeof(AdminLogSemanticAuthoringAnalyzer))]
public sealed class AdminLogSemanticAuthoringAnalyzerTest
{
    private const string Support = """
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
                public static AdminLogExplicitSemantics GetActorSemantics(object player, EntityUid actor, EntityUid? tool = null) => default;
                public static AdminLogExplicitSemantics GetActorVictimToolSemantics(object player, EntityUid actor, EntityUid victim, EntityUid tool) => default;
                public static AdminLogExplicitSemantics GetActorVictimsSemantics(object player, EntityUid actor, IReadOnlyCollection<EntityUid> victims, EntityUid? tool = null) => default;
                public static AdminLogExplicitSemantics GetActorVictimsToolSemantics(object player, EntityUid actor, IReadOnlyCollection<EntityUid> victims, EntityUid tool) => default;
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

    private static Task Verify(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<AdminLogSemanticAuthoringAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { Support, code },
            },
        };

        test.TestState.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    [Test]
    public async Task RedundantExplicitEntitiesTrigger()
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

        await Verify(code,
            VerifyCS.Diagnostic(Diagnostics.IdAdminLogRedundantExplicitEntities).WithLocation(0));
    }

    [Test]
    public async Task ManualSelfActionSemanticsTriggerHelperDiagnostic()
    {
        const string code = """
            using System;
            using System.Collections.Generic;
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;

                public void Run(EntityUid user, EntityUid tool, Guid id)
                {
                    _adminLogger.AddStructured(LogType.Ingestion, LogImpact.Low,
                        $"{user:actor} ate with {tool:tool}",
                        players: new[] { id },
                        {|#0:entities: new[]
                        {
                            new AdminLogEntityRef(user, AdminLogEntityRole.Actor),
                            new AdminLogEntityRef(user, AdminLogEntityRole.Victim),
                            new AdminLogEntityRef(tool, AdminLogEntityRole.Tool),
                        }|},
                        playerRoles: new Dictionary<Guid, AdminLogEntityRole>
                        {
                            [id] = AdminLogEntityRole.Actor,
                        });
                }
            }
            """;

        await Verify(code,
            VerifyCS.Diagnostic(Diagnostics.IdAdminLogUseSharedSemanticsHelper).WithLocation(0));
    }

    [Test]
    public async Task ManualActorVictimToolSemanticsTriggerHelperDiagnostic()
    {
        const string code = """
            using System;
            using System.Collections.Generic;
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;

                public void Run(EntityUid user, EntityUid target, EntityUid tool, Guid actorId, Guid targetId)
                {
                    _adminLogger.AddStructured(LogType.ForceFeed, LogImpact.Medium,
                        $"{user:actor} forced {target:victim} to eat with {tool:tool}",
                        players: new[] { actorId, targetId },
                        {|#0:entities: new[]
                        {
                            new AdminLogEntityRef(user, AdminLogEntityRole.Actor),
                            new AdminLogEntityRef(target, AdminLogEntityRole.Victim),
                            new AdminLogEntityRef(tool, AdminLogEntityRole.Tool),
                        }|},
                        playerRoles: new Dictionary<Guid, AdminLogEntityRole>
                        {
                            [actorId] = AdminLogEntityRole.Actor,
                            [targetId] = AdminLogEntityRole.Victim,
                        });
                }
            }
            """;

        await Verify(code,
            VerifyCS.Diagnostic(Diagnostics.IdAdminLogUseSharedSemanticsHelper).WithLocation(0));
    }

    [Test]
    public async Task ManualActorToolSemanticsTriggerHelperDiagnostic()
    {
        const string code = """
            using System;
            using System.Collections.Generic;
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;

                public void Run(EntityUid user, EntityUid tool, Guid actorId)
                {
                    _adminLogger.AddStructured(LogType.Unknown, LogImpact.Low,
                        $"{user:actor} triggered {tool:tool}",
                        players: new[] { actorId },
                        {|#0:entities: new[]
                        {
                            new AdminLogEntityRef(user, AdminLogEntityRole.Actor),
                            new AdminLogEntityRef(tool, AdminLogEntityRole.Tool),
                        }|},
                        playerRoles: new Dictionary<Guid, AdminLogEntityRole>
                        {
                            [actorId] = AdminLogEntityRole.Actor,
                        });
                }
            }
            """;

        await Verify(code,
            VerifyCS.Diagnostic(Diagnostics.IdAdminLogUseSharedSemanticsHelper).WithLocation(0));
    }

    [Test]
    public async Task HelperBackedTier3UsageStaysClean()
    {
        const string code = """
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;
                private readonly object _player = default!;

                public void Run(EntityUid user, EntityUid tool)
                {
                    var semantics = AdminLogHelpers.GetActorVictimToolSemantics(_player, user, user, tool);
                    _adminLogger.AddStructured(LogType.Ingestion, LogImpact.Low,
                        $"{user:actor} ate with {tool:tool}",
                        players: semantics.Players,
                        entities: semantics.Entities,
                        playerRoles: semantics.PlayerRoles);
                }
            }
            """;

        await Verify(code);
    }

    [Test]
    public async Task HelperBackedActorToolUsageStaysClean()
    {
        const string code = """
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;
                private readonly object _player = default!;

                public void Run(EntityUid user, EntityUid tool)
                {
                    var semantics = AdminLogHelpers.GetActorSemantics(_player, user, tool);
                    _adminLogger.AddStructured(LogType.Unknown, LogImpact.Low,
                        $"{user:actor} triggered {tool:tool}",
                        players: semantics.Players,
                        entities: semantics.Entities,
                        playerRoles: semantics.PlayerRoles);
                }
            }
            """;

        await Verify(code);
    }

    [Test]
    public async Task ManualActorManyVictimsToolSemanticsTriggerHelperDiagnostic()
    {
        const string code = """
            using System;
            using System.Collections.Generic;
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;

                public void Run(EntityUid user, EntityUid target1, EntityUid target2, EntityUid tool, Guid actorId, Guid target1Id, Guid target2Id)
                {
                    _adminLogger.AddStructured(LogType.Unknown, LogImpact.Low,
                        $"{user:actor} hit 2 targets with {tool:tool}",
                        players: new[] { actorId, target1Id, target2Id },
                        {|#0:entities: new[]
                        {
                            new AdminLogEntityRef(user, AdminLogEntityRole.Actor),
                            new AdminLogEntityRef(tool, AdminLogEntityRole.Tool),
                            new AdminLogEntityRef(target1, AdminLogEntityRole.Victim),
                            new AdminLogEntityRef(target2, AdminLogEntityRole.Victim),
                        }|},
                        playerRoles: new Dictionary<Guid, AdminLogEntityRole>
                        {
                            [actorId] = AdminLogEntityRole.Actor,
                            [target1Id] = AdminLogEntityRole.Victim,
                            [target2Id] = AdminLogEntityRole.Victim,
                        });
                }
            }
            """;

        await Verify(code,
            VerifyCS.Diagnostic(Diagnostics.IdAdminLogUseSharedSemanticsHelper).WithLocation(0));
    }

    [Test]
    public async Task HelperBackedMultiTargetTier3UsageStaysClean()
    {
        const string code = """
            using System.Collections.Generic;
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;
                private readonly object _player = default!;

                public void Run(EntityUid user, EntityUid target1, EntityUid target2, EntityUid tool)
                {
                    IReadOnlyCollection<EntityUid> victims = new[] { target1, target2 };
                    var semantics = AdminLogHelpers.GetActorVictimsToolSemantics(_player, user, victims, tool);
                    _adminLogger.AddStructured(LogType.Unknown, LogImpact.Low,
                        $"{user:actor} hit 2 targets with {tool:tool}",
                        players: semantics.Players,
                        entities: semantics.Entities,
                        playerRoles: semantics.PlayerRoles);
                }
            }
            """;

        await Verify(code);
    }

    [Test]
    public async Task SubjectBasedBatchSemanticsStayClean()
    {
        const string code = """
            using System;
            using System.Collections.Generic;
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;

                public void Run(EntityUid user, EntityUid subject, EntityUid target1, EntityUid target2, Guid actorId)
                {
                    _adminLogger.AddStructured(LogType.Unknown, LogImpact.Low,
                        $"{user:actor} sent {subject:subject} into 2 targets",
                        players: new[] { actorId },
                        entities: new[]
                        {
                            new AdminLogEntityRef(user, AdminLogEntityRole.Actor),
                            new AdminLogEntityRef(subject, AdminLogEntityRole.Subject),
                            new AdminLogEntityRef(target1, AdminLogEntityRole.Victim),
                            new AdminLogEntityRef(target2, AdminLogEntityRole.Victim),
                        },
                        playerRoles: new Dictionary<Guid, AdminLogEntityRole>
                        {
                            [actorId] = AdminLogEntityRole.Actor,
                        });
                }
            }
            """;

        await Verify(code);
    }

    [Test]
    public async Task ActorToolSubjectSemanticsStayClean()
    {
        const string code = """
            using System;
            using System.Collections.Generic;
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;

                public void Run(EntityUid user, EntityUid tool, EntityUid subject, Guid actorId)
                {
                    IReadOnlyCollection<AdminLogEntityRef> semantics = new[]
                    {
                        new AdminLogEntityRef(user, AdminLogEntityRole.Actor),
                        new AdminLogEntityRef(subject, AdminLogEntityRole.Subject),
                        new AdminLogEntityRef(tool, AdminLogEntityRole.Tool),
                    };

                    _adminLogger.AddStructured(LogType.Unknown, LogImpact.Low,
                        $"{user:actor} saved {subject:subject} to {tool:tool}",
                        players: new[] { actorId },
                        entities: semantics,
                        playerRoles: new Dictionary<Guid, AdminLogEntityRole>
                        {
                            [actorId] = AdminLogEntityRole.Actor,
                        });
                }
            }
            """;

        await Verify(code);
    }

    [Test]
    public async Task MessageFirstTier2UsageStaysClean()
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
                    _adminLogger.AddStructured(LogType.ForceFeed, LogImpact.Medium,
                        $"{user:actor} forced {target:victim} to eat {tool:tool}",
                        new { stage = "success" });
                }
            }
            """;

        await Verify(code);
    }

    [Test]
    public async Task JustifiedMultiTargetTier3UsageStaysClean()
    {
        const string code = """
            using System.Collections.Generic;
            using Content.Shared.Administration.Logs;
            using Content.Shared.Database;
            using Robust.Shared.GameObjects;

            public sealed class Test
            {
                private readonly ISharedAdminLogManager _adminLogger = default!;

                public void Run(EntityUid user, EntityUid tool, IReadOnlyCollection<AdminLogEntityRef> targetRefs, int count)
                {
                    _adminLogger.AddStructured(LogType.Unknown, LogImpact.Low,
                        $"{user:actor} hit {count} targets with {tool:tool}",
                        entities: targetRefs);
                }
            }
            """;

        await Verify(code);
    }
}
