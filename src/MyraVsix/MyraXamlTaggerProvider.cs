using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Myra.Classifier;
using Myra.Tagger;
using System.ComponentModel.Composition; 

namespace Myra
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("XML")]
    [TagType(typeof(IErrorTag))]
    internal sealed class MyraXamlTaggerProvider : ITaggerProvider
    { 
        private readonly ITextDocumentFactoryService _documents;
        private readonly SVsServiceProvider _vsServiceProvider;

        [ImportingConstructor]
        public MyraXamlTaggerProvider(ITextDocumentFactoryService documents, SVsServiceProvider vsServiceProvider)
        { 
            _documents = documents;
            _vsServiceProvider = vsServiceProvider;
        }

        public ITagger<T>? CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
        {
            if (typeof(T) != typeof(IErrorTag))
                return null;

            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Helpers.IsMyraXamlFile(textBuffer, _documents))
                return null;

            var componentModel = (IComponentModel)_vsServiceProvider.GetService(typeof(SComponentModel))!;
            var workspace = componentModel.GetService<VisualStudioWorkspace>();

            return new MyraXamlTagger(textBuffer, MyraXamlBufferServices.GetModelProvider(workspace, textBuffer)) as ITagger<T>;
        }
    }
}
