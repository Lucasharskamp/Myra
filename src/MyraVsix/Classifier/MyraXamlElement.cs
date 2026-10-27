using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic; 

namespace Myra.Classifier
{

    internal sealed class MyraXamlElement : IMyraValueType
    {
        public MyraXamlElement(ITextSnapshot snapshot, string name, string? prefix, bool isValid, SnapshotSpan tagSpan)
        {
            Snapshot = snapshot;
            Name = name;
            Prefix = prefix;
            IsValid = isValid;

            TagSpan = tagSpan;
            ValueSpan = tagSpan; // overridden later if need be.
        }

        public ITextSnapshot Snapshot { get; }

        public SnapshotSpan NameSpan { get; set; }
        public SnapshotSpan? ClosingNameSpan { get; set; }

        public string Name { get; }

        public string? Prefix { get; }

        public ITypeSymbol? TypeSymbol { get; set; }
        public bool IsValid { get; }

        public bool IsPropertyElement { get; set; }

        public SnapshotSpan? OwnerNameSpan { get; set; }
        public SnapshotSpan? ClosingOwnerNameSpan { get; set; }
        public SnapshotSpan? MemberNameSpan { get; set; }
        public SnapshotSpan? ClosingMemberNameSpan { get; set; }

        public MyraXamlElement? Parent { get; set; }

        public SnapshotSpan TagSpan { get; }

        public SnapshotSpan ValueSpan { get; set; }
        public string Value { get; set; }

        public List<MyraXamlAttribute> Attributes { get; } = [];

        public List<MyraXamlElement> Children { get; } = [];
    }
}
