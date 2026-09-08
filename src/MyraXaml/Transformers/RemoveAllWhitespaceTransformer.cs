using System;
using System.Collections.Generic;
using System.Text;
using XamlX.Ast;
using XamlX.Transform;

namespace Myra.Xaml.Transformers
{
    internal sealed class RemoveAllWhitespaceTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni)
            {
                for (var c = ni.Children.Count - 1; c >= 0; c--)
                {
                    var child = ni.Children[c];
                    if (child is XamlAstTextNode textNode)
                    {
                        if (String.IsNullOrWhiteSpace(textNode.Text))
                        {
                            ni.Children.RemoveAt(c);
                        }
                    }
                }
            }
            else if (node is XamlAstXamlPropertyValueNode propertyNode)
            {
                for (var c = propertyNode.Values.Count - 1; c >= 0; c--)
                {
                    var child = propertyNode.Values[c];
                    if (child is XamlAstTextNode textNode)
                    {
                        if (String.IsNullOrWhiteSpace(textNode.Text))
                        {
                            propertyNode.Values.RemoveAt(c);
                        }
                    }
                }
            }

            return node;
        }
    }
}
