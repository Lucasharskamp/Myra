using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    internal sealed class XamlRootDirectivesTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        { 
            if (node is not XamlAstObjectNode valueNode)
                return node;

            // get "x:StyleSheet" directive from the element.
            // it must be a static field/property containing the stylesheet.
            if (valueNode.ExtractXDirective("Stylesheet", out var styleSheetDirective, out var styleSheetReference))
            {
                // separate field/property from the type.
                var lastDot = styleSheetReference.LastIndexOf('.');
                if (lastDot <= 0)
                {
                    throw new XamlLoadException("x:StyleSheet must refer to a static property or field!", styleSheetDirective);
                }

                var styleSheetType = styleSheetReference.Substring(0, lastDot);
                var styleSheetName = styleSheetReference.Substring(lastDot+1);

                var styleSheetContainer = context.Configuration.TypeSystem.FindType(styleSheetType);
                if (styleSheetContainer == null)
                {
                    throw new XamlLoadException(
                         $"The specified Container type '{styleSheetContainer}' for x:StyleSheet could not be found. Please specifiy the full namespace and type name.",
                         styleSheetDirective);
                }
                 
                var property = styleSheetContainer.GetAllProperties().FirstOrDefault(f => f.Name == styleSheetName && f.Getter != null && f.Getter.IsStatic)
                    ?? throw new XamlLoadException($"The specified property '{styleSheetName}' in type '{styleSheetContainer}' does not exist or is not static.",
                        styleSheetDirective); 

                if (property.PropertyType != TypesContainer.StyleSheet)
                {
                    throw new XamlLoadException($"The specified property '{styleSheetName}' in type '{styleSheetContainer}' is not of type 'Myra.Graphics2D.UI.Styles.StyleSheet'.",
                        styleSheetDirective);
                }

                context.SetItem(new XamlStyleSheetContainer(styleSheetContainer, property));
            }


            // get "x:ViewModel" directive from the element.  
            if (!valueNode.ExtractXDirective("ViewModel", out var viewModelDirective, out var viewModelReference))
                return node;

            var viewModelType = context.Configuration.TypeSystem.FindType(viewModelReference);
            if (viewModelType == null)
            {
                throw new XamlLoadException(
                    $"The specified ViewModel type '{viewModelReference}' could not be found. Please specifiy the full namespace and type name.", 
                    viewModelDirective);
            }

            if (!viewModelType.GetAllInterfaces().Any(i => i == TypesContainer.INotifyPropertyChanged))
            {
                throw new XamlLoadException(
                    $"The specified ViewModel type '{viewModelReference} must implement 'System.ComponentModel.INotifyPropertyChanged'", 
                    viewModelDirective);
            }

            var propertyName = viewModelDirective.Values.Count > 1
                && viewModelDirective.Values[1] is XamlAstXamlPropertyValueNode xamlPropertyNode
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
                    viewModelDirective);
            }

            if (targetProperty.PropertyType != viewModelType)
            {
                throw new XamlLoadException(
                     $"Property '{propertyName}' does not have the same type as the specified ViewModel.",
                     viewModelDirective);
            }

            context.SetItem(new XamlViewModelContainer(viewModelType, targetProperty));

            return node;
        }
    }
}
