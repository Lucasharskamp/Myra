using Microsoft.VisualStudio.LanguageServices;
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
        private readonly VisualStudioWorkspace _workspace;

        [ImportingConstructor]
        public MyraXamlClassifierProvider(
            ITextDocumentFactoryService documents,
            IClassificationTypeRegistryService classificationTypes,
            VisualStudioWorkspace workspace)
        {
            _documents = documents;
            _classificationTypes = classificationTypes;
            _workspace = workspace;
        }

        public IClassifier? GetClassifier(ITextBuffer textBuffer)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Helpers.IsMyraXamlFile(textBuffer, _documents))
                return null;

            var typeClassification = _classificationTypes.GetClassificationType("myraXamlType");
            var memberClassification = _classificationTypes.GetClassificationType("myraXamlMember");
            var featureClassification = _classificationTypes.GetClassificationType("myraXamlFeature");
            var errorClassification = _classificationTypes.GetClassificationType("myraXamlError");
            if (typeClassification == null || memberClassification == null|| errorClassification == null)
                return null;
             
            return new MyraXamlClassifier(typeClassification, 
                memberClassification,
                featureClassification,
                errorClassification,
                MyraXamlBufferServices.GetModelProvider(_workspace, textBuffer));
        }
    }
}
