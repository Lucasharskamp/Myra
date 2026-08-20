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

            var nameDirectives = objectNode.Children
                .OfType<XamlAstXmlDirective>()
                .Where(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Name")
                .ToArray();

            if (nameDirectives.Length == 0)
                return node;

            if (nameDirectives.Length > 1)
                throw new XamlLoadException("x:Name can only exists once on a type!", objectNode);

            var directive = nameDirectives[0];
               
            if (directive.Values.Count != 1 ||
                directive.Values[0] is not XamlAstTextNode text)
            {
                throw new XamlLoadException(
                    "x:Name must have a single string value.", directive);
            }

            var name = text.Text;

            // Remove x:Name from the normal XAML property/directive
            // processing.
            objectNode.Children.Remove(directive);
             
            if (TypesContainer.CurrentClass == null)
                throw new InvalidOperationException("This should never happen!");


            // get the target property to aim at.
            var targetProperty = TypesContainer.CurrentClass
                                .GetAllProperties()
                                .FirstOrDefault(p => p.Name == name);

            if (targetProperty == null)
            {
                throw new XamlLoadException(
                    $"Property '{name}' does not exist in type '{TypesContainer.CurrentClass.FullName}'",
                    objectNode);
            }

            if (targetProperty.Setter == null)
            {
                throw new XamlLoadException(
                    $"Property '{targetProperty.Name}' from '{targetProperty.DeclaringType.FullName}' is not writable.",
                    objectNode);
            }

             TransformerHelpers.EnsureAssignability(directive, targetProperty, directive.Name, objectNode.Type.GetClrType());
            var targetClrProperty = new XamlAstClrProperty(directive, targetProperty, context.Configuration);
            var thisNode = new XamlAstContextLocalNode(directive, TypesContainer.CurrentClass);
            return new XamlPropertyAssignmentNode(directive, targetClrProperty, targetClrProperty.Setters, [objectNode]);
 
        }
    }
}
