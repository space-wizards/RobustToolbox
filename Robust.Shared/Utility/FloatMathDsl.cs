using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using StringToExpression;

namespace Robust.Shared.Utility
{

    /// <summary>
    /// Represents a language to that handles basic mathematics out-of-game.
    /// </summary>
    public sealed class FloatMathDsl
        : FloatMathDslBase
    {

        [Dependency] private readonly IGameTiming _timing = default!;

        [Dependency] private readonly IRobustRandom _random = default!;

        /// <summary>
        /// Returns the definitions for types used within the language.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<GrammerDefinition> TypeDefinitions()
        {
            foreach (var gd in base.TypeDefinitions())
            {
                yield return gd;
            }

            yield return new OperandDefinition(
                "INTRIN_TIME_S",
                @"(?i)ts\(\)",
                x =>
                {
                    return Expression.Call(
                        Expression.Constant(this),
                        Type<object>.Method(o => GetSeconds()));
                });
            yield return new OperandDefinition(
                "INTRIN_TIME_MS",
                @"(?i)tms\(\)",
                x =>
                {
                    return Expression.Call(
                        Expression.Constant(this),
                        Type<object>.Method(o => GetMilliseconds()));
                });
            yield return new OperandDefinition(
                "INTRIN_RANDOM",
                @"(?i)rng\(\)",
                x =>
                {
                    return Expression.Call(
                        Expression.Constant(this),
                        Type<object>.Method(o => GetRandom()));
                });

            ;
        }

        private float GetSeconds()
            => (float) _timing.RealTime.TotalSeconds;

        private float GetMilliseconds()
            => (float) _timing.RealTime.TotalMilliseconds;

        private float GetRandom()
            => _random.NextFloat();

        /// <summary>
        /// Returns the definitions for whitespace used within the language.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<GrammerDefinition> WhitespaceDefinitions()
            => new[]
            {
                new GrammerDefinition("SPACE", @"\s+", true)
            };

    }

}
