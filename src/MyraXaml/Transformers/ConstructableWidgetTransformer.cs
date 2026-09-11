using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System.Collections.Generic;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    public sealed class ConstructableWidgetTransformer() : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode ni)
            {  
                var t = ni.Type.GetClrType();
                if (t.Assembly == null || t.Assembly.Name != "Myra")
                    return node;

                if (!TypesContainer.Widget.IsAssignableFrom(t))
                    return node;

                var ctor = t.FindConstructor([TypesContainer.Stylesheet, context.Configuration.WellKnownTypes.String]);
                if (ctor is not null)
                {
                    var parameters = new List<IXamlAstValueNode>()
                    {
                         new XamlStylesheetProperty(node, context, null),
                         node.ToConstantNode(context, "")
                    };
                     
                    return new XamlAstConstructableObjectNode(ni,
                        ni.Type.GetClrTypeReference(), ctor, parameters, ni.Children);
                } 
            }

            return node;
        }
    }
}
