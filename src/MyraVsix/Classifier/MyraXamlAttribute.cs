using Microsoft.VisualStudio.Text;
using System; 

namespace Myra.Classifier
{


    internal sealed class MyraXamlAttribute
    {
        public MyraXamlAttribute(string name)
        {
            Name = name;
        }
        public SnapshotSpan NameSpan { get; set; }

        public string? Prefix { get; set; }

        public string Name { get; }

        public SnapshotSpan? OwnerNameSpan { get; set; }

        public SnapshotSpan? MemberNameSpan { get; set; }

        public MyraXamlType? OwnerType { get; set; }

        public bool IsAttachedProperty { get; set; }

        public bool IsValid { get; set; }
    }
}
