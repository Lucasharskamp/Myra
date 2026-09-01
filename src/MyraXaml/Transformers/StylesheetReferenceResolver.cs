using Myra.Xaml.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    class StylesheetReferenceResolver : IXamlAstTransformer
    {

        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni)
            {
                for (var c = ni.Children.Count - 1; c >= 0; c--)
                {
                    var child = ni.Children[c];
                    if (child is XamlAstObjectNode objectNode && objectNode.Type is XamlAstXmlTypeReference xmlref)
                    {
                        var stylesheetProperty = TypesContainer.StyleSheet.Properties.FirstOrDefault(p => p.Name == xmlref.Name);
                        if (stylesheetProperty != null) {
                            ni.Children.RemoveAt(c);
                            ni.Children.Insert(c, new XamlAstXamlPropertyValueNode(objectNode,
                                new XamlAstClrProperty(node, stylesheetProperty, context.Configuration),
                                objectNode.Children.OfType<IXamlAstValueNode>(),
                                false));
                            }
                    }
                }
            }

            return node;
        } 
    }
}
