using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Classifier
{
    internal sealed class MyraXamlDocumentModel
    {
        public ITextSnapshot Snapshot { get; set; }

        public IReadOnlyList<MyraXamlElement> Elements { get; set; }

        public IReadOnlyDictionary<string, string> Namespaces { get; set; }

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
