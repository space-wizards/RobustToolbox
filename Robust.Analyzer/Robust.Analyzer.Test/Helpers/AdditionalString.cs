using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Robust.Analyzer.Test
{
    /// <summary>
    /// In-memory "file" for passing AdditionalDocuments to an analyzer.
    /// </summary>
    internal class AdditionalString : AdditionalText
    {
        private readonly string _path;
        private readonly string _contents;

        public override string Path { get => _path; }

        public AdditionalString(string path, string contents)
        {
            _path = path;
            _contents = contents;
        }

        public override SourceText GetText(CancellationToken _cancellationToken = default)
        {
            return SourceText.From(_contents);
        }
    }
}
