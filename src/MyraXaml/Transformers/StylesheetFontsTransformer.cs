using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    internal class StylesheetFontsTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is not XamlManipulationGroupNode groupNode)
                return node;

            // check first child. 
            var lastChild = groupNode.Children.FirstOrDefault();
            if (lastChild is not XamlPropertyAssignmentNode propertyNode)
                return node;

            if (propertyNode.Property is not XamlAstClrProperty clrProperty)
                return node;

            // if the conversion already happened, we don't need to do it again 
            if (propertyNode.Values.OfType<XamlAstObjectNode>().First().Children.Count > 3)
                return node;

            if (clrProperty.Name != "Fonts" || clrProperty.DeclaringType != TypesContainer.Stylesheet)
                return node;

            int groupSize = groupNode.Children.Count;
            var calls = new List<IXamlAstValueNode>();
            for (int i = 0; i < groupSize; i++)
            {
                var property = (XamlPropertyAssignmentNode)groupNode.Children[i];
                // Register font with IResolver
                var properties = property.Values.OfType<XamlAstObjectNode>().First();
                var idNode = properties.GetPropertyAssignment("Id");
                if (idNode?.Values.FirstOrDefault() is not XamlAstTextNode idText || string.IsNullOrWhiteSpace(idText.Text))
                {
                    throw new XamlLoadException("StylesheetFont requires an 'ID' node!", node);
                }

                var fileNode = properties.GetPropertyAssignment("File");
                if (fileNode?.Values.FirstOrDefault() is not XamlAstTextNode fileText || string.IsNullOrWhiteSpace(fileText.Text))
                {
                    throw new XamlLoadException("StylesheetFont requires a 'Path' node!", node);
                }
                var sizeNode = properties.GetPropertyAssignment("Size");
                if (sizeNode?.Values.FirstOrDefault() is not XamlConstantNode size)
                {
                    throw new XamlLoadException("StylesheetFont requires a 'Size' node!", node);
                } 

                // Stylesheet fonts need to be loaded in on the resolver, so this can be added right here. 
                var loadMethod = TypesContainer.IFileResolver.GetMethod(m => m.Name == "RegisterFont");
                var fontProperty = new XamlAstClrProperty(node, TypesContainer.StylesheetFont.GetTypeProperty(node, "Font"), context.Configuration);

                var methodCall =
                    new XamlPropertyAssignmentNode(node, fontProperty, fontProperty.Setters,
                    [new XamlStaticOrTargetedReturnMethodCallNode(node, loadMethod,
                        [
                            new XamlStaticOrTargetedReturnMethodCallNode(node,
                                TypesContainer.MyraEnvironment.GetAllProperties().First(p => p.Name == "Resolver").Getter!,
                                null),
                            new XamlStaticOrTargetedReturnMethodCallNode(node,
                                TypesContainer.MyraEnvironment.GetAllProperties().First(p => p.Name == "GraphicsDevice").Getter!,
                                null),
                            idText.ToConstantNode(context),
                            fileText.ToConstantNode(context, Path.Combine(TransformerHelpers.CurrentRelativePath, fileText.Text)),
                            size
                        ])
                    ]);
                properties.Children.Add(methodCall);
            }
             
            return node;
        }
    }
}
