using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    /// <summary>
    /// This transformer resolves x:Name directives, binding the element to the code-behind property/field 
    /// the x:Name property is targeting.
    /// </summary>
    public sealed class XamlNameDirectiveTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {  
            if (node is not XamlAstObjectNode valueNode)
                return node;

            // get "x:Name" directive from the element.  
            if (!valueNode.ExtractXDirective("Name", out var directive, out var text))
                return node;

            var rootClrType = context.CodeBehindClrType();
             
            // get the target property to aim at.
            var targetProperty = rootClrType
                                .GetAllProperties()
                                .FirstOrDefault(p => p.Name == text);

            if (targetProperty == null)
            {
                throw new XamlLoadException(
                    $"Property '{text}' does not exist in type '{rootClrType.FullName}'",
                    valueNode);
            }

            if (targetProperty.Setter == null)
            {
                throw new XamlLoadException(
                    $"Property '{targetProperty.Name}' from '{targetProperty.DeclaringType.FullName}' is not writable.",
                    valueNode);
            } 

            var assignment = new XamlAssignPropertyValueNode(
                directive,
                targetProperty.Setter, 
                valueNode,
                context.RootObject);

            return new XamlValueWithManipulationNode(
                directive,
                valueNode,
                assignment);
        }
    }
}
