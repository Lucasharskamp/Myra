using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic; 

namespace Myra.Classifier
{

    internal sealed class MyraXamlElement
    {
        public ITextSnapshot Snapshot { get; set; }

        public SnapshotSpan NameSpan { get; set; }
        public SnapshotSpan? ClosingNameSpan { get; set; }

        public string Name { get; set; }

        public string? Prefix { get; set; }

        public Type? Type { get; set; }
        public Type? MemberType { get; set; }

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
