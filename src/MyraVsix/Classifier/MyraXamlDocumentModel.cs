using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace Myra.Classifier
{
    internal sealed class MyraXamlDocumentModel
    {
        public MyraXamlDocumentModel(ITextSnapshot snapshot, 
                                     IReadOnlyList<MyraXamlElement> elements, 
                                     IReadOnlyDictionary<string, string> namespaces)
        {
            Snapshot = snapshot;
            Elements = elements;
            Namespaces = namespaces;
        }

        public ITextSnapshot Snapshot { get; }

        public IReadOnlyList<MyraXamlElement> Elements { get; }

        public IReadOnlyDictionary<string, string> Namespaces { get; }
         
    }
}
