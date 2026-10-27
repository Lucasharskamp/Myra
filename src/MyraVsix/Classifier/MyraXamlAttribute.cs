using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace Myra.Classifier
{


    internal sealed class MyraXamlAttribute
    {
        public MyraXamlAttribute(string name, string? prefix, 
            SnapshotSpan nameSpan, SnapshotSpan valueSpan,
            bool isValid, bool isFeature)
        {
            Name = name;
            Prefix = prefix;
            NameSpan = nameSpan;
            ValueSpan = valueSpan;
            IsValid = isValid;
            IsFeature = isFeature;
        }
        public SnapshotSpan NameSpan { get; }

        public string? Prefix { get; }

        public string Name { get; }

        public bool? IsOwnerValid { get; set; }
        public SnapshotSpan? OwnerNameSpan { get; set; }

        public ITypeSymbol? TypeSymbol { get; set; }

        public bool IsAttachedProperty { get; set; }
        public bool IsFeature { get; }

        public bool IsValid { get; }

        public SnapshotSpan ValueSpan { get; }
    }
}
