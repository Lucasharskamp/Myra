using Myra.Xaml.Compiler;
using System;
using XamlX.Ast;
using XamlX.TypeSystem;
using static XamlX.Parsers.CommaSeparatedParenthesesTreeParser;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// Container for the Stylesheet currently in use.
    /// </summary>
    public sealed class XamlStylesheetContainer
    {
        public IXamlAstValueNode Node { get;  }
        public XamlStylesheetContainer(XamlStaticExtensionNode node)
        {
            Node = node;
        }

        public XamlStylesheetContainer(IXamlLineInfo node,  XamlTypeWellKnownTypes wellKnownTypes, string fileName)
        {
            Node = new XamlStaticOrTargetedReturnMethodCallNode(node,
                    new XamlWrappedMethod(MyraBindingCompilationContext.GetStylesheet),
                    [new XamlConstantNode(node, wellKnownTypes.String, fileName)]);
        }
    }
}
