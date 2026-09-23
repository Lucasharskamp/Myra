using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Myra
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("XML")]
    internal sealed class MyraXamlCompletionSourceProvider : ICompletionSourceProvider
    {
        private readonly ITextDocumentFactoryService _documents;

        [ImportingConstructor]
        public MyraXamlCompletionSourceProvider(ITextDocumentFactoryService documents)
        {
            _documents = documents;
        }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            if (!Helpers.IsMyraXamlFile(textBuffer, _documents))
                return null!;

            return null!;
        }
    }
}
