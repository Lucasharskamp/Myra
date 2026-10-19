using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Myra.Classifier;
using Myra.Tagger;
using System;
using System.ComponentModel.Composition; 

namespace Myra
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("XML")]
    [TagType(typeof(ClassificationTag))]
    internal sealed class MyraXamlTaggerProvider : ITaggerProvider
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ITextDocumentFactoryService _documents;
        private readonly IClassificationTypeRegistryService _classificationRegistry;
        private readonly SVsServiceProvider _vsServiceProvider;

        [ImportingConstructor]
        public MyraXamlTaggerProvider(
             [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ITextDocumentFactoryService documents, 
            IClassificationTypeRegistryService classificationRegistry,
            SVsServiceProvider vsServiceProvider)
        {
            this.serviceProvider = serviceProvider;
            _documents = documents;
            _classificationRegistry = classificationRegistry;
            _vsServiceProvider = vsServiceProvider;
        }

        public ITagger<T>? CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
        {
            if (typeof(T) != typeof(IClassificationTag))
                return null;

            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Helpers.IsMyraXamlFile(textBuffer, _documents))
                return null;

            var componentModel = (IComponentModel)_vsServiceProvider.GetService(typeof(SComponentModel))!;

            var workspace =
                componentModel.GetService<VisualStudioWorkspace>();

            return new MyraXamlTagger(textBuffer, workspace,
                MyraXamlBufferServices.GetModelProvider(serviceProvider, workspace, textBuffer), _classificationRegistry) as ITagger<T>;
        }
    }
}
