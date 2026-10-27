
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;

namespace Myra.Classifier
{  
    internal static class MyraXamlBufferServices
    {
        private static readonly object ModelProviderKey = new();

        public static MyraXamlDocumentModelProvider GetModelProvider(VisualStudioWorkspace workspace, ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(
                ModelProviderKey,
                () => new MyraXamlDocumentModelProvider(buffer, new MyraXamlParser(workspace)));
        }
    }
} 
