using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Shared.IoC;
using StringToExpression;
using StringToExpression.GrammerDefinitions;
using StringToExpression.Util;

namespace Robust.Shared.Utility
{

    public abstract class FloatMathDslBase
    {

        protected readonly Language Language;

        protected FloatMathDslBase()
        {
            IoCManager.InjectDependencies(this);
            Language = new Language(AllDefinitions().ToArray());
        }

        /// <summary>
        /// Parses the specified text converting it into a expression action.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns></returns>
        public Expression<Func<float>> Parse(string text)
        {
            var body = Language.Parse(text);
            body = ExpressionConversions.Convert(body, typeof(float));
            return Expression.Lambda<Func<float>>(body);
        }

        /// <summary>
        /// Parses the specified text converting it into an expression. The expression can take a single parameter
        /// </summary>
        /// <typeparam name="T">the type of the parameter.</typeparam>
        /// <param name="text">The text to parse.</param>
        /// <returns></returns>
        public Expression<Func<T, float>> Parse<T>(string text)
        {
            var parameters = new[] {Expression.Parameter(typeof(T))};
            var body = Language.Parse(text, parameters);
            body = ExpressionConversions.Convert(body, typeof(float));
            return Expression.Lambda<Func<T, float>>(body, parameters);
        }

        /// <summary>
        /// Returns all the definitions used by the language.
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<GrammerDefinition> AllDefinitions()
        {
            IEnumerable<FunctionCallDefinition> functions;
            var definitions = new List<GrammerDefinition>();
            definitions.AddRange(TypeDefinitions());
            definitions.AddRange(functions = FunctionDefinitions());
            definitions.AddRange(BracketDefinitions(functions));
            definitions.AddRange(OperatorDefinitions());
            definitions.AddRange(PropertyDefinitions());
            definitions.AddRange(WhitespaceDefinitions());
            return definitions;
        }

        /// <summary>
        /// Returns the definitions for types used within the language.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<GrammerDefinition> TypeDefinitions()
        {
            //Only have float to make things easier for casting
            yield return new OperandDefinition(
                "NUMBER",
                @"(?i)(?<![\w\)])[-+]?[0-9]*\.?[0-9]+(?:e[-+]?[0-9]+)?",
                x => Expression.Constant(float.Parse(x)));
            yield return new OperandDefinition(
                "CONST_PI",
                @"(?i)pi",
                x => Expression.Constant(Math.PI));
            yield return new OperandDefinition(
                "CONST_PINF",
                @"(?i)(?<![\w\)])\+inf",
                x => Expression.Constant(float.PositiveInfinity));
            yield return new OperandDefinition(
                "CONST_NINF",
                @"(?i)(?<![\w\)])\-inf",
                x => Expression.Constant(float.NegativeInfinity));
            yield return new OperandDefinition(
                "CONST_NAN",
                @"(?i)nan",
                x => Expression.Constant(float.NaN));
            yield return new OperandDefinition(
                "CONST_E",
                @"(?i)e",
                x => Expression.Constant(Math.E));
        }

        /// <summary>
        /// Returns the definitions for arithmetic operators used within the language.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<GrammerDefinition> OperatorDefinitions()
        {
            yield return new BinaryOperatorDefinition(
                "ADD", @"\+", 2, Expression.Add);

            yield return new BinaryOperatorDefinition(
                "SUB", @"\-", 2, Expression.Subtract);

            yield return new BinaryOperatorDefinition(
                "MUL", @"\*", 1, Expression.Multiply);

            yield return new BinaryOperatorDefinition(
                "DIV", @"\/", 1, Expression.Divide);

            yield return new BinaryOperatorDefinition(
                "MOD", @"%", 1, Expression.Modulo);

            ;
        }

        /// <summary>
        /// Returns the definitions for brackets used within the language.
        /// </summary>
        /// <param name="functionCalls">The function calls in the language. (used as opening brackets)</param>
        /// <returns></returns>
        protected virtual IEnumerable<GrammerDefinition> BracketDefinitions(IEnumerable<FunctionCallDefinition> functionCalls)
        {
            BracketOpenDefinition openBrace;
            ListDelimiterDefinition delim;

            yield return openBrace = new BracketOpenDefinition(
                "OPEN_BRACE",
                @"\(");

            yield return delim = new ListDelimiterDefinition(
                "COMMA",
                ",");

            yield return new BracketCloseDefinition(
                "CLOSE_BRACE",
                @"\)",
                new[] {openBrace}.Concat(functionCalls),
                delim);
        }

        /// <summary>
        /// Returns the definitions for functions used within the language.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<FunctionCallDefinition> FunctionDefinitions()
        {
            yield return new FunctionCallDefinition(
                "FN_ABS",
                @"(?i)abs\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Abs(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_SIN",
                @"(?i)sin\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Sin(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_ASIN",
                @"(?i)asin\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Asin(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_COS",
                @"(?i)cos\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Cos(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_ACOS",
                @"(?i)acos\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Acos(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_TAN",
                @"(?i)tan\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Tan(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_ATAN2",
                @"(?i)atan2\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Atan2(0, 0)),
                    parameters[0], parameters[1]));

            yield return new FunctionCallDefinition(
                "FN_POW",
                @"(?i)pow\(",
                new[] {typeof(float), typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Pow(0, 0)),
                    parameters[0], parameters[1]));

            yield return new FunctionCallDefinition(
                "FN_SQRT",
                @"(?i)sqrt\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Sqrt(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_EXP",
                @"(?i)exp\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Exp(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_LOG",
                @"(?i)log\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Log(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_ROUND",
                @"(?i)round\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Round(0, default(MidpointRounding))),
                    parameters[0], Expression.Constant(MidpointRounding.AwayFromZero)));

            yield return new FunctionCallDefinition(
                "FN_FLOOR",
                @"(?i)floor\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Floor(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_CEILING",
                @"(?i)ceil\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Ceiling(0)),
                    parameters[0]));

            yield return new FunctionCallDefinition(
                "FN_TRUNC",
                @"(?i)trunc\(",
                new[] {typeof(float)},
                parameters => Expression.Call(
                    null,
                    Type<object>.Method(x => MathF.Truncate(0)),
                    parameters[0]));
        }

        /// <summary>
        /// Returns the definitions for property names used within the language.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<GrammerDefinition> PropertyDefinitions()
        {
            yield return new OperandDefinition(
                "PROPERTY_PATH",
                @"(?<![0-9])([A-Za-z_][A-Za-z0-9_]*\.?)+",
                (value, parameters) => value.Split('.')
                    .Aggregate((Expression) parameters[0], (exp, prop)
                        => Expression.MakeMemberAccess(exp, exp.Type.GetRuntimeProperties()
                            .First(x => x.Name.Equals(prop,
                                StringComparison.OrdinalIgnoreCase)))));
        }

        /// <summary>
        /// Returns the definitions for whitespace used within the language.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<GrammerDefinition> WhitespaceDefinitions()
        {
            yield return new GrammerDefinition("SPACE", @"\s+", true);
        }

    }

}
