using System.Text;

namespace Robust.Roslyn.Shared;

public struct IndentWriter(StringBuilder builder, int depth = 0)
{
    public readonly StringBuilder Builder = builder;
    public int Depth = depth;

    public readonly void AppendLine()
    {
        Builder.AppendLine();
    }

    public readonly void AppendLine(string str)
    {
        Builder.AppendLine(str);
    }

    public void AppendLineIndented(string str)
    {
        AppendIndents();
        Builder.AppendLine(str);
    }

    public void AppendOpeningBrace()
    {
        AppendLineIndented("{");
        PushDepth();
    }

    public void AppendClosingBrace()
    {
        PopDepth();
        AppendLineIndented("}");
    }

    public readonly void AppendIndents()
    {
        Builder.Append(' ', 4 * Depth);
    }

    public readonly void Append(string str)
    {
        Builder.Append(str);
    }

    public void PushDepth()
    {
        Depth += 1;
    }

    public void PopDepth()
    {
        if (Depth == 0)
            return;

        Depth -= 1;
    }

    public override string ToString()
    {
        return Builder.ToString();
    }
}
