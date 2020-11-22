using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection.Metadata;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Robust.Shared.ContentPack
{
    internal sealed partial class AssemblyTypeChecker
    {
        // Contains primary parsing code for method and field declarations in the sandbox whitelist.
        private static readonly Parser<char, string> NamespacedIdentifier =
            Token(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '`')
                .AtLeastOnceString()
                .Labelled("valid identifier");

        private static readonly Parser<char, MType> BasicTypeParser = SkipWhitespaces.Then(NamespacedIdentifier.Select(
            p => p switch
            {
                "void" => new MTypePrimitive(PrimitiveTypeCode.Void),
                "bool" => new MTypePrimitive(PrimitiveTypeCode.Boolean),
                "char" => new MTypePrimitive(PrimitiveTypeCode.Char),
                "sbyte" => new MTypePrimitive(PrimitiveTypeCode.SByte),
                "byte" => new MTypePrimitive(PrimitiveTypeCode.Byte),
                "short" => new MTypePrimitive(PrimitiveTypeCode.Int16),
                "ushort" => new MTypePrimitive(PrimitiveTypeCode.UInt32),
                "int" => new MTypePrimitive(PrimitiveTypeCode.Int32),
                "uint" => new MTypePrimitive(PrimitiveTypeCode.UInt32),
                "long" => new MTypePrimitive(PrimitiveTypeCode.Int64),
                "ulong" => new MTypePrimitive(PrimitiveTypeCode.UInt64),
                "nint" => new MTypePrimitive(PrimitiveTypeCode.IntPtr),
                "nuint" => new MTypePrimitive(PrimitiveTypeCode.UIntPtr),
                "float" => new MTypePrimitive(PrimitiveTypeCode.Single),
                "double" => new MTypePrimitive(PrimitiveTypeCode.Double),
                "string" => new MTypePrimitive(PrimitiveTypeCode.String),
                "object" => new MTypePrimitive(PrimitiveTypeCode.Object),
                "typedref" => new MTypePrimitive(PrimitiveTypeCode.TypedReference),
                _ => (MType) new MTypeParsed(p)
            })).Labelled("basic type");

        private static readonly Parser<char, IEnumerable<MType>> GenericParametersParser =
            Rec(() => ConstructedTypeParser!)
                .Between(SkipWhitespaces)
                .Separated(Char(','))
                .Between(Char('<'), Char('>'));

        private static readonly Parser<char, MType> GenericMethodPlaceholderParser =
            String("!!")
                .Then(Digit.AtLeastOnceString())
                .Select(p => (MType) new MTypeGenericMethodPlaceHolder(int.Parse(p, CultureInfo.InvariantCulture)));

        private static readonly Parser<char, MType> GenericTypePlaceholderParser =
            String("!")
                .Then(Digit.AtLeastOnceString())
                .Select(p => (MType) new MTypeGenericMethodPlaceHolder(int.Parse(p, CultureInfo.InvariantCulture)));

        private static readonly Parser<char, MType> ConstructedTypeParser =
            Parser.Map((arg1, arg2, arg3) =>
                {
                    var type = arg1;
                    if (arg2.HasValue)
                    {
                        type = new MTypeGeneric(type, arg2.Value.ToImmutableArray());
                    }

                    foreach (var _ in arg3)
                    {
                        type = new MTypeSZArray(type);
                    }

                    return type;
                },
                Try(GenericMethodPlaceholderParser)
                    .Or(Try(GenericTypePlaceholderParser))
                    .Or(BasicTypeParser),
                GenericParametersParser.Optional(),
                String("[]").Many());

        private static readonly Parser<char, MType> ByRefTypeParser =
            String("ref").Then(SkipWhitespaces).Then(ConstructedTypeParser).Labelled("ByRef type");

        private static readonly Parser<char, MType> TypeParser = Try(ByRefTypeParser).Or(ConstructedTypeParser);

        private static readonly Parser<char, ImmutableArray<MType>> MethodParamsParser =
            TypeParser
                .Between(SkipWhitespaces)
                .Separated(Char(','))
                .Between(Char('('), Char(')'))
                .Select(p => p.ToImmutableArray());

        internal static readonly Parser<char, WhitelistMethodDefine> MethodParser =
            Parser.Map(
                (a, b, c) => new WhitelistMethodDefine(b, a, c),
                TypeParser,
                SkipWhitespaces.Then(NamespacedIdentifier),
                SkipWhitespaces.Then(MethodParamsParser));

        internal static readonly Parser<char, WhitelistFieldDefine> FieldParser = Parser.Map(
            (a, b) => new WhitelistFieldDefine(b, a),
            ConstructedTypeParser.Between(SkipWhitespaces),
            NamespacedIdentifier);

        internal sealed class WhitelistMethodDefine
        {
            public string Name { get; }
            public MType ReturnType { get; }
            public ImmutableArray<MType> ParameterTypes { get; }

            public WhitelistMethodDefine(string name, MType returnType, ImmutableArray<MType> parameterTypes)
            {
                Name = name;
                ReturnType = returnType;
                ParameterTypes = parameterTypes;
            }
        }

        internal sealed class WhitelistFieldDefine
        {
            public string Name { get; }
            public MType FieldType { get; }

            public WhitelistFieldDefine(string name, MType fieldType)
            {
                Name = name;
                FieldType = fieldType;
            }
        }
    }
}
