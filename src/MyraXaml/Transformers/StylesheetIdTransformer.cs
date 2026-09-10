using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            var idProperty = objectNode.Children.FirstOrDefault(c => c is XamlAstXamlPropertyValueNode propNode
                                && propNode.Property is XamlAstNamePropertyReference propRef
                                && propRef.Name == "Id");

            if (idProperty == null)
                return node;

            string foundId = "";
            if (((XamlAstXamlPropertyValueNode)idProperty).Values.FirstOrDefault() is XamlAstTextNode propText)
            {
                foundId = propText.Text;
                objectNode.Children.Remove(idProperty);
            }

            objectNode.Children.Insert(0, new XamlAstXmlDirective(node,
                    XamlNamespaces.Xaml2006,
                    "Key",
                    [new XamlConstantNode(node, context.Configuration.WellKnownTypes.String, foundId)]));

            return objectNode;
        }
    }
}
