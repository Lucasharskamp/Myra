using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System;
using System.Diagnostics;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    public sealed class XamlNameDirectiveTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(
            AstTransformationContext context,
            IXamlAstNode node)
        {  
            if (node is not XamlAstObjectNode objectNode)
                return node;

            var allDirectives = objectNode.Children
                .OfType<XamlAstXmlDirective>()
                .Where(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Name")
                .ToArray();

            if (allDirectives.Length == 0)
                return node;

            if (allDirectives.Length > 1)
                throw new XamlLoadException("x:Name can only exists once on a type!", objectNode);

            var nameDirective = allDirectives[0];
               
            if (nameDirective.Values.Count != 1 ||
                nameDirective.Values[0] is not XamlAstTextNode text)
            {
                throw new XamlLoadException(
                    "x:Name must have a single string value.", nameDirective);
            }

            var name = text.Text;
            var rootClrType = context.RootObject.Type.GetClrType();

            // Remove x:Name from the normal XAML property/directive
            // processing.
            objectNode.Children.Remove(nameDirective); 

            // get the target property to aim at.
            var targetProperty = rootClrType
                                .GetAllProperties()
                                .FirstOrDefault(p => p.Name == name);

            if (targetProperty == null)
            {
                throw new XamlLoadException(
                    $"Property '{name}' does not exist in type '{rootClrType.FullName}'",
                    objectNode);
            }

            if (targetProperty.Setter == null)
            {
                throw new XamlLoadException(
                    $"Property '{targetProperty.Name}' from '{targetProperty.DeclaringType.FullName}' is not writable.",
                    objectNode);
            } 

            var assignment = new XamlAssignAndReturnValueNode(
                nameDirective,
                targetProperty.Setter, 
                objectNode,
                context.RootObject);

            return new XamlValueWithManipulationNode(
                nameDirective,
                objectNode,
                assignment);
        }
    }
}
