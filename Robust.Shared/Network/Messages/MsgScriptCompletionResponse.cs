using System.Collections.Immutable;
using System.IO;
using Lidgren.Network;
using Microsoft.CodeAnalysis.Completion;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Messages
{
    public class MsgScriptCompletionResponse : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public int ScriptSession { get; set; }
        public ImmutableArray<LiteResult> Results;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ScriptSession = buffer.ReadInt32()!;

            var n = buffer.ReadInt32()!;
            var cli = ImmutableArray.CreateBuilder<LiteResult>();
            for (var i = 0; i < n; i++)
            {
                var lr = new LiteResult(buffer);
                cli.Add(lr);
            }
            Results = cli.ToImmutable();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ScriptSession);

            buffer.Write(Results.Length);
            foreach (var res in Results)
                res.WriteToBuffer(buffer);
        }

        public class LiteResult {
            public string DisplayText;
            public string DisplayTextPrefix;
            public string DisplayTextSuffix;

            public string InlineDescription;

            public ImmutableArray<string> Tags;
            public ImmutableDictionary<string, string> Properties;

            private LiteResult(
                string displayText,
                string displayTextPrefix,
                string displayTextSuffix,
                string inlineDescription,
                ImmutableArray<string> tags,
                ImmutableDictionary<string, string> properties
            )
            {
                DisplayText = displayText;
                DisplayTextPrefix = displayTextPrefix;
                DisplayTextSuffix = displayTextSuffix;
                InlineDescription = inlineDescription;

                Tags = tags;
                Properties = properties;
            }

            public LiteResult(NetIncomingMessage buffer)
            {
                DisplayText = buffer.ReadString()!;
                DisplayTextPrefix = buffer.ReadString()!;
                DisplayTextSuffix = buffer.ReadString()!;
                InlineDescription = buffer.ReadString()!;

                var n = buffer.ReadInt32()!;
                var iab = ImmutableArray.CreateBuilder<string>();
                for (var i = 0; i < n; i++)
                    iab.Add(buffer.ReadString()!);

                Tags = iab.ToImmutable()!;

                n = buffer.ReadInt32()!;
                var idb = ImmutableDictionary.CreateBuilder<string, string>();
                for (var i = 0; i < n; i++)
                    idb.Add(buffer.ReadString()!, buffer.ReadString()!);

                Properties = idb.ToImmutable();
            }

            public void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(DisplayText);
                buffer.Write(DisplayTextPrefix);
                buffer.Write(DisplayTextSuffix);
                buffer.Write(InlineDescription);

                buffer.Write(Tags.Length);
                foreach (var e in Tags)
                    buffer.Write(e);

                buffer.Write(Properties.Count);
                foreach (var e in Properties)
                {
                    buffer.Write(e.Key);
                    buffer.Write(e.Value);
                }
            }

            public static explicit operator LiteResult(CompletionItem ci) => new(
                displayText: ci.DisplayText,
                displayTextPrefix: ci.DisplayTextPrefix,
                displayTextSuffix: ci.DisplayTextSuffix,
                inlineDescription: ci.InlineDescription,
                tags: ci.Tags,
                properties: ci.Properties
            );
        }
    }
}
