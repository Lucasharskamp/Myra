using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Myra.Tagger;
using System.ComponentModel.Composition; 

namespace Myra
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("XML")]
    internal sealed class MyraXamlTaggerProvider : ITaggerProvider
    {
        private readonly ITextDocumentFactoryService _documents;

        [ImportingConstructor]
        public MyraXamlTaggerProvider(ITextDocumentFactoryService documents)
        {
            _documents = documents;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer textBuffer) where T : ITag
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Helpers.IsMyraXamlFile(textBuffer, _documents))
                return null!;

            return new MyraXamlTagger<T>(textBuffer);
        }
    }
}
