using System.Reflection;
using System.Xml;
using LoxSmoke.DocXml;

namespace Robust.LanguageServer;

public sealed class DocsManager
{
    private DocXmlReader _reader = new();

    public TypeComments GetComments(Type type)
    {
        return _reader.GetTypeComments(type);
    }

    public CommonComments GetComments(MemberInfo member)
    {
        return _reader.GetMemberComments(member);
    }
}
