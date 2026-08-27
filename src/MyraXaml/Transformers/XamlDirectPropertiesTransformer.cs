using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    /// <summary>
    /// Handles child elements of an element that are specified as properties, not underlying widgets.
    /// </summary>
    public sealed class XamlDirectPropertiesTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is not XamlAstObjectNode objectNode)
                return node;

            var parentType = objectNode.Type.GetClrType();

            for (var i = 0; i < objectNode.Children.Count; i++)
            {
                if (objectNode.Children[i] is not XamlAstXamlPropertyValueNode propertyNode)
                    continue;

                if (propertyNode.Property is not XamlAstClrProperty property)
                    continue;

                var separator = property.Name.IndexOf('.');
                if (separator <= 0 ||
                    separator == property.Name.Length - 1)
                {
                    continue;
                }

                var ownerName = property.Name.Substring(0, separator);
                var propertyName = property.Name.Substring(separator + 1);

                // Only handle Owner.Property syntax belonging to this object.
                if (!string.Equals(
                        ownerName,
                        parentType.Name,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var targetProperty = parentType
                    .GetAllProperties()
                    .FirstOrDefault(p => p.Name == propertyName);

                if (targetProperty == null)
                {
                    throw new XamlLoadException(
                        $"Property '{propertyName}' was not found on " +
                        $"'{parentType.GetFqn()}'.",
                        propertyNode);
                }

                if (targetProperty.Setter == null)
                {
                    throw new XamlLoadException(
                        $"Property '{parentType.GetFqn()}.{propertyName}' " +
                        $"does not have a setter.",
                        propertyNode);
                }

                var clrProperty = new XamlAstClrProperty(
                    propertyNode,
                    targetProperty,
                    context.Configuration);

                var values = propertyNode.Values;

                objectNode.Children[i] =
                    new XamlPropertyAssignmentNode(
                        propertyNode,
                        clrProperty,
                        clrProperty.Setters,
                        values);
            }

            return objectNode;
        }
    }
}
