using System;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.Validation;

internal enum SymbolType
{
    Prototype,
}

internal record DocumentSymbol(string Name, SymbolType Type, NodeMark NodeStart, NodeMark NodeEnd);
