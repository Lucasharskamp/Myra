using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    internal sealed class XamlViewModelAssignmentTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        { 
            if (node is not XamlAstObjectNode valueNode)
                return node;

            // get "x:ViewModel" directive from the element.  
            if (!valueNode.ExtractXDirective("ViewModel", out var directive, out var typeReference))
                return node;

            var viewModelType = context.Configuration.TypeSystem.FindType(typeReference);
            if (viewModelType == null)
            {
                throw new XamlLoadException(
                    $"The specified ViewModel type '{typeReference}' could not be found. Please specifiy the full namespace and type name.", 
                    directive);
            }

            if (!viewModelType.GetAllInterfaces().Any(i => i == TypesContainer.INotifyPropertyChanged))
            {
                throw new XamlLoadException(
                    $"The specified ViewModel type '{typeReference} must implement 'System.ComponentModel.INotifyPropertyChanged'", 
                    directive);
            }

            var propertyName = directive.Values.Count > 1
                && directive.Values[1] is XamlAstXamlPropertyValueNode xamlPropertyNode
                && xamlPropertyNode.Values.Count == 1
                && xamlPropertyNode.Values[0] is XamlAstTextNode viewModelTextNode
                ? viewModelTextNode.Text
                : "ViewModel";

            var codeBehindClr = context.CodeBehindClrType();
            var targetProperty = codeBehindClr.GetAllProperties().FirstOrDefault(p => p.Name == propertyName);
            if (targetProperty == null)
            {
                throw new XamlLoadException(
                    $"Cannot find property '{propertyName}' in type '{codeBehindClr.FullName}' to use for the ViewModel!", 
                    directive);
            }

            if (targetProperty.PropertyType != viewModelType)
            {
                throw new XamlLoadException(
                     $"Property '{propertyName}' does not have the same type as the specified ViewModel.",
                     directive);
            }

            context.SetItem(new XamlViewModelContainer(viewModelType, targetProperty));

            return node;
        }
    }
}
