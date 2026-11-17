using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Myra.Classifier
{
    internal sealed class MyraXamlDocumentModel(ITextSnapshot snapshot,
                                 IReadOnlyList<MyraXamlElement> elements,
                                 IReadOnlyDictionary<string, string> namespaces,
                                 Compilation compilation,
                                 Dictionary<string, INamedTypeSymbol?> typeCache,
                                 INamedTypeSymbol? codeBehindType,
                                 FrozenSet<IMethodSymbol> codeBehindMethods,
                                 FrozenSet<IFieldSymbol> codeBehindFields,
                                 FrozenSet<IPropertySymbol> codeBehindProperties)
    {
        public ITextSnapshot Snapshot { get; } = snapshot;

        public IReadOnlyList<MyraXamlElement> Elements { get; } = elements;

        public IReadOnlyDictionary<string, string> Namespaces { get; } = namespaces;
        public Compilation Compilation { get; } = compilation;
        public FrozenDictionary<string, INamedTypeSymbol?> TypeCache { get; } = typeCache.ToFrozenDictionary();
        public INamedTypeSymbol? CodeBehindType { get; } = codeBehindType;
        public FrozenSet<IMethodSymbol> CodeBehindMethods { get; } = codeBehindMethods;
        public FrozenSet<IFieldSymbol> CodeBehindFields { get; } = codeBehindFields;
        public FrozenSet<IPropertySymbol> CodeBehindProperties { get; } = codeBehindProperties;

        public MyraXamlElement? FindElementAt(int position)
        {
            MyraXamlElement? previous = null;
            foreach (var element in Elements)
            {
                if (element.EntireOpeningSpan.Contains(position))
                    return element;

                if (previous != null && position <= element.EntireOpeningSpan.Start)
                    return previous;

                previous = element;
            }
            return null;
        }

    }
}
