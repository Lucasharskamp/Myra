using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Myra.Classifier
{  
    internal static class MyraXamlClassificationTypes
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("myraXamlElement")]
        internal static ClassificationTypeDefinition Element = null!;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("myraXamlAttribute")]
        internal static ClassificationTypeDefinition Attribute = null!;
    }
}
