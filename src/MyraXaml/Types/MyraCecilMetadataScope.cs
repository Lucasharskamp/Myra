using Mono.Cecil;

namespace Myra.Xaml.Types
{
    internal sealed class MyraCecilMetadataScope : IMetadataScope
    {
        public MyraCecilMetadataScope(MetadataScopeType metadataScopeType, string name, MetadataToken metadataToken)
        {
            MetadataScopeType = metadataScopeType;
            Name = name;
            MetadataToken = metadataToken;
        }

        public MetadataScopeType MetadataScopeType { get; }

        public string Name { get; set; }  
        public MetadataToken MetadataToken { get; set; }
    }
}
