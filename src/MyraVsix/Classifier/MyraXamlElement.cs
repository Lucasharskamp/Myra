using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic; 

namespace Myra.Classifier
{

    internal sealed class MyraXamlElement(ITextSnapshot snapshot, string name, string? prefix, bool isValid, SnapshotSpan tagSpan) : IMyraValueType
    {
        public ITextSnapshot Snapshot { get; } = snapshot;
        public SnapshotSpan NameSpan { get; set; }
        public SnapshotSpan? ClosingNameSpan { get; set; }
        public string Name { get; } = name;

        public string? Prefix { get; } = prefix;

        public ITypeSymbol? TypeSymbol { get; set; }
        public bool IsValid { get; } = isValid;

        public bool IsPropertyElement { get; set; }

        public SnapshotSpan? OwnerNameSpan { get; set; }
        public SnapshotSpan? ClosingOwnerNameSpan { get; set; }
        public SnapshotSpan? MemberNameSpan { get; set; }
        public SnapshotSpan? ClosingMemberNameSpan { get; set; }

        public MyraXamlElement? Parent { get; set; }

        public SnapshotSpan TagSpan { get; } = tagSpan;

        public SnapshotSpan ValueSpan { get; set; } = tagSpan; // overridden later if need be.
        public string Value { get; set; }

        public List<MyraXamlAttribute> Attributes { get; } = [];

        public List<MyraXamlElement> Children { get; } = [];
    }
}
