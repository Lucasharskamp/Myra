using Myra.Xaml.Helpers;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Myra.Xaml.Transformers
{
    /// <summary>
    /// For stylesheets; extracts "Id" property and replace it with a "x:Key" instead.
    /// </summary>
    internal class StylesheetIdTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is not XamlAstObjectNode objectNode)
                return node;
             
            var idProperty = objectNode.GetProperty("Id");

            if (idProperty == null)
                return node;

            string foundId = ""; 
            if (idProperty.Values.FirstOrDefault() is XamlAstTextNode propText)
            {
                foundId = propText.Text;

                // ID properties need to be removed, except for StylesheetFont
                if (objectNode.Type.GetTypeName() != "StylesheetFont")
                {
                    objectNode.Children.Remove(idProperty);
                }
            }

            objectNode.Children.Insert(0, new XamlAstXmlDirective(node,
                    XamlNamespaces.Xaml2006,
                    "Key",
                    [node.ToConstantNode(context, foundId)]));

            return objectNode;
        } 
    }
}
