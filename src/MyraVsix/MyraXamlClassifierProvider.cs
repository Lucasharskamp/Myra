using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Myra.Classifier;
using System.ComponentModel.Composition; 

namespace Myra
{
    [Export(typeof(IClassifierProvider))]
    [ContentType("XML")]
    internal sealed class MyraXamlClassifierProvider : IClassifierProvider
    {
        private readonly ITextDocumentFactoryService _documents;
        private readonly IClassificationTypeRegistryService _classificationTypes;

        [ImportingConstructor]
        public MyraXamlClassifierProvider(
            ITextDocumentFactoryService documents,
            IClassificationTypeRegistryService classificationTypes)
        {
            _documents = documents;
            _classificationTypes = classificationTypes;
        }

        public IClassifier? GetClassifier(ITextBuffer textBuffer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Helpers.IsMyraXamlFile(textBuffer, _documents))
                return null;

            var elementClassification = _classificationTypes.GetClassificationType("myraXamlElement");
            var attributeClassification = _classificationTypes.GetClassificationType("myraXamlAttribute");
            if (elementClassification == null || attributeClassification == null)
                return null;

            return new MyraXamlClassifier(elementClassification, attributeClassification);
        }
    }
}
