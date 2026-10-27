using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Myra.Classifier
{ 

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "myraXamlType")]
    [Name("Myra XAML Type")]
    [UserVisible(true)]
    [Order(After = Priority.Default)]
    internal sealed class MyraXamlTypeFormat : ClassificationFormatDefinition
    {
        public MyraXamlTypeFormat()
        {
            DisplayName = "Myra XAML Type";
            ForegroundColor = Colors.Goldenrod;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "myraXamlMember")]
    [Name("Myra XAML Type member")]
    [UserVisible(true)]
    internal sealed class MyraXamlMemberFormat : ClassificationFormatDefinition
    {
        public MyraXamlMemberFormat()
        {
            DisplayName = "Myra XAML Member";
            ForegroundColor = Colors.LightGoldenrodYellow;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "myraXamlError")]
    [Name("Myra XAML Error")]
    [UserVisible(true)]
    internal sealed class MyraXamlBrokenFormat : ClassificationFormatDefinition
    {
        public MyraXamlBrokenFormat()
        {
            DisplayName = "Myra XAML Broken format";
            ForegroundColor = Colors.Red;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "myraXamlFeature")]
    [Name("Myra Feature")]
    [UserVisible(true)]
    internal sealed class MyraFeatureFormat : ClassificationFormatDefinition
    {
        public MyraFeatureFormat()
        {
            DisplayName = "Myra Feature format";
            ForegroundColor = Colors.PaleGoldenrod;
        }
    }


    internal static class MyraXamlClassificationTypes
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("myraXamlType")]
        internal static ClassificationTypeDefinition Type = null!;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("myraXamlMember")]
        internal static ClassificationTypeDefinition Member = null!;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("myraXamlFeature")]
        internal static ClassificationTypeDefinition Feature = null!;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("myraXamlError")]
        internal static ClassificationTypeDefinition Error = null!;
    }
}
