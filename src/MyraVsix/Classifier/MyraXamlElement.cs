using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic; 

namespace Myra.Classifier
{

    internal sealed class MyraXamlElement
    {
        public MyraXamlElement(ITextSnapshot snapshot, string name, string? prefix, bool isValid)
        {
            Snapshot = snapshot;
            Name = name;
            Prefix = prefix;
            IsValid = isValid;
        }

        public ITextSnapshot Snapshot { get; }

        public SnapshotSpan NameSpan { get; set; }
        public SnapshotSpan? ClosingNameSpan { get; set; }

        public string Name { get; }

        public string? Prefix { get; }

        public INamedTypeSymbol? Type { get; set; }
        public bool IsValid { get; }

        public bool IsPropertyElement { get; set; }

        public SnapshotSpan? OwnerNameSpan { get; set; }
        public SnapshotSpan? ClosingOwnerNameSpan { get; set; }
        public SnapshotSpan? MemberNameSpan { get; set; }
        public SnapshotSpan? ClosingMemberNameSpan { get; set; }

        public MyraXamlElement? Parent { get; set; }

        public SnapshotSpan TagSpan { get; set; }

        public List<MyraXamlAttribute> Attributes { get; } = [];

        public List<MyraXamlElement> Children { get; } = [];
    }
}
