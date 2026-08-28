using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    internal sealed class XamlRootDirectivesTransformer() : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        { 
            if (node is not XamlAstObjectNode valueNode)
                return node;

            var isRoot = valueNode.Type.GetClrType() == context.RootObject.Type.GetClrType();
             
            // get "x:Style" directive from the element
            // this shows which style of the stylesheet to employ
            if (valueNode.FindXDirectiveAsAny("Style", out var styleDirective, out var styleNode))
            {
                if (!isRoot)
                {
                    throw new XamlLoadException(
                       $"x:Style can only be specified on the root node!",
                       styleDirective);
                }
                if (styleNode is XamlAstTextNode textNode)
                {
                    styleNode = new XamlConstantNode(node, context.Configuration.WellKnownTypes.String, textNode.Text);
                }
                context.SetItem(new XamlStyleContainer(styleNode));
                valueNode.Children.Remove(styleDirective);
            }

            // get "x:StyleSheet" directive from the element.
            // it must be a static field/property containing the stylesheet.
            if (valueNode.FindXDirectiveAsStatic("Stylesheet", out var styleSheetDirective, out var stylesheetNode))
            {
                if (!isRoot)
                {
                    throw new XamlLoadException(
                       $"x:Stylesheet can only be specified on the root node!",
                       styleSheetDirective);
                }
                context.SetItem(new XamlStylesheetContainer(stylesheetNode));
                valueNode.Children.Remove(styleSheetDirective);
            }

            // get "x:ViewModel" directive from the element.  
            if (!valueNode.ExtractXDirectiveAsText("ViewModel", out var viewModelDirective, out var viewModelReference))
                return node;
              
            if (!isRoot)
            {
                throw new XamlLoadException(
                   $"x:ViewModel can only be specified on the root node!",
                   viewModelDirective);
            }

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
