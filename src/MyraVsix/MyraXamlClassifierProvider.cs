using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Myra.Classifier;
using System;
using System.ComponentModel.Composition; 

namespace Myra
{
    [Export(typeof(IClassifierProvider))]
    [ContentType("XML")]
    internal sealed class MyraXamlClassifierProvider : IClassifierProvider
    {
        private readonly IServiceProvider _provider;
        private readonly ITextDocumentFactoryService _documents;
        private readonly IClassificationTypeRegistryService _classificationTypes;
        private readonly VisualStudioWorkspace _workspace;

        [ImportingConstructor]
        public MyraXamlClassifierProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ITextDocumentFactoryService documents,
            IClassificationTypeRegistryService classificationTypes,
            VisualStudioWorkspace workspace)
        {
            _provider = serviceProvider;
            _documents = documents;
            _classificationTypes = classificationTypes;
            _workspace = workspace;
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
             
            return new MyraXamlClassifier(elementClassification, 
                attributeClassification, 
                MyraXamlBufferServices.GetModelProvider(_provider, _workspace, textBuffer));
        }
    }
}
