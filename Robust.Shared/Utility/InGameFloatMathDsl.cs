using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;
using System.Collections.Generic;
using System.Linq.Expressions;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Robust.Shared.Utility
{

    /// <summary>
    /// Represents a language to that handles basic mathematics in-game.
    /// </summary>
    public sealed class InGameFloatMathDsl : FloatMathDslBase
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
        }

        private float GetSeconds()
            => (float) _timing.CurTime.TotalSeconds;

        private float GetMilliseconds()
            => (float) _timing.CurTime.TotalMilliseconds;

        private float GetRandom()
            => _random.NextFloat();

    }

}
