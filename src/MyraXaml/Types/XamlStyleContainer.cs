using XamlX.Ast;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// Container for the style name currently in use.
    /// </summary>
    internal class XamlStyleContainer
    {
        public IXamlAstValueNode Node { get; }

        public XamlStyleContainer(IXamlAstValueNode node)
        {
            Node = node;
        }
    }
}
