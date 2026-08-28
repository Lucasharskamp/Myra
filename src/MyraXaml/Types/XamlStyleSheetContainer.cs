using XamlX.Ast;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// Container for the Stylesheet currently in use.
    /// </summary>
    public sealed class XamlStylesheetContainer
    {
        public IXamlAstValueNode Node { get;  }
        public XamlStylesheetContainer(IXamlAstValueNode node)
        {
            Node = node;
        }
    }
}
