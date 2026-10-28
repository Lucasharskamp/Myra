using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace Myra.Classifier
{
    internal sealed class MyraXamlDocumentModel(ITextSnapshot snapshot,
                                 IReadOnlyList<MyraXamlElement> elements,
                                 IReadOnlyDictionary<string, string> namespaces,
                                 Compilation compilation)
    {
        public ITextSnapshot Snapshot { get; } = snapshot;

        public IReadOnlyList<MyraXamlElement> Elements { get; } = elements;

        public IReadOnlyDictionary<string, string> Namespaces { get; } = namespaces;
        public Compilation Compilation { get; } = compilation;

        public MyraXamlElement? FindElementAt(int position)
        {
            foreach (var element in Elements)
            {
                if (element.NameSpan.Contains(position))
                    return element;

                if (element.MemberNameSpan.HasValue && element.MemberNameSpan.Value.Contains(position))
                    return element;

                if (element.ClosingNameSpan.HasValue && element.ClosingNameSpan.Value.Contains(position))
                    return element;
            }
            return null;
        }

    }
}
