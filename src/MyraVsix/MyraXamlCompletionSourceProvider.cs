using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Myra.Classifier;
using Myra.Completion;
using System.ComponentModel.Composition;

namespace Myra
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name(nameof(MyraXamlCompletionSourceProvider))]
    [ContentType("XML")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class MyraXamlCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        private readonly ITextDocumentFactoryService _documents;
        private readonly VisualStudioWorkspace _workspace;

        [ImportingConstructor]
        public MyraXamlCompletionSourceProvider(ITextDocumentFactoryService documents,
            VisualStudioWorkspace workspace)
        {
            _documents = documents;
            _workspace = workspace;
        }

        public IAsyncCompletionSource? GetOrCreate(ITextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!Helpers.IsMyraXamlFile(textView.TextBuffer, _documents))
                return null;

            return textView.Properties.GetOrCreateSingletonProperty(
                typeof(MyraXamlCompletionSource),
                () => new MyraXamlCompletionSource(
                    textView,
                    MyraXamlBufferServices.GetModelProvider(_workspace, textView.TextBuffer)));
        } 
    }
}
