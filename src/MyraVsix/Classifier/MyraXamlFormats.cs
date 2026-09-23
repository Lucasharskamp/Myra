
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Myra.Classifier
{ 

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "myraXamlElement")]
    [Name("Myra XAML Element")]
    [UserVisible(true)]
    [Order(After = Priority.Default)]
    internal sealed class MyraXamlElementFormat : ClassificationFormatDefinition
    {
        public MyraXamlElementFormat()
        {
            DisplayName = "Myra XAML Element";
            ForegroundColor = Colors.Goldenrod;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "myraXamlAttribute")]
    [Name("Myra XAML Attribute")]
    [UserVisible(true)]
    internal sealed class MyraXamlAttributeFormat : ClassificationFormatDefinition
    {
        public MyraXamlAttributeFormat()
        {
            DisplayName = "Myra XAML Attribute";
            ForegroundColor = Colors.LightGoldenrodYellow;
        }
    }
}
