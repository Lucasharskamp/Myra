using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using XamlX.Ast;
using XamlX.Transform;

namespace Myra.Xaml.Transformers
{
    internal class StylesheetToLocalTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is not XamlValueWithManipulationNode manipulation)
                return node;

            if (manipulation.Manipulation is not XamlObjectInitializationNode initializer)
                return node;

            if (manipulation.Type.GetClrTypeReference().Type != TypesContainer.TextureRegionAtlas)
            {
                return node;
            }
            var local = new XamlAstCompilerLocalNode(node, new XamlAstClrTypeReference(node, TypesContainer.TextureRegionAtlas, false));
            var value = new XamlValueNodeWithBeginInit(new XamlAstLocalInitializationNodeEmitter(local, manipulation.Value, local));
            var deferred = new XamlAstManipulationImperativeNode(initializer,
                new XamlAstImperativeValueManipulation(initializer, local, initializer));

            return new XamlValueWithManipulationNode(node, value, 
                new XamlManipulationGroupNode(node)
                {
                    Children =
                        {
                            deferred
                        }
                }); 
        }
    }
}
