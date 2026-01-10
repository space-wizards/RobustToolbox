using System;
using XamlX;

namespace Robust.Client.NameGenerator;

internal static class DiagnosticsCodes
{
    internal const string XamlNotFound = "RXN0001";
    internal const string MultipleXamlFound = "RXN0002";
    internal const string UnhandledException = "RXN0003";
    internal const string EmptyXamlFound = "RXN0004";
    internal const string InvalidRootType = "RXN0005";
    internal const string MissingPartialDeclaration = "RXN0006";

    internal const string TransformError = "RXN0007";
    internal const string TypeSystemError = "RXN0008";
    internal const string ParseError = "RXN0009";
    internal const string EmitError = "RXN0010";
    internal const string Obsolete = "RXN0011";

    internal static string XamlXCodeMappings(object exception)
    {
    	return exception switch
    	{
            XamlXWellKnownDiagnosticCodes wellKnownDiagnosticCodes => wellKnownDiagnosticCodes switch
            {
                XamlXWellKnownDiagnosticCodes.Obsolete => Obsolete,
                _ => throw new ArgumentOutOfRangeException()
            },

            XamlTransformException => TransformError,
            XamlTypeSystemException => TypeSystemError,
            XamlLoadException => EmitError,
            XamlParseException => ParseError,

    		_ => "RXN_UNKNOWN",
    	};
    }
}
