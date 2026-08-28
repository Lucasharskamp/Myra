using Myra.Xaml.Types;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem; 

namespace Myra.Xaml.Helpers
{
    internal static class TransformerHelpers
    {
        public static void EnsureAssignability(IXamlLineInfo lineInfo, XamlAstClrProperty targetProperty, string sourceFieldName, IXamlType sourceType)
        {
            if (!targetProperty.Getter!.ReturnType.IsAssignableFrom(sourceType))
            {
                throw new XamlLoadException(
                    $"Property '{targetProperty.Name}' from '{targetProperty.DeclaringType.FullName}' is not " +
                    $"assignable to '{sourceFieldName}' from '{targetProperty.DeclaringType.FullName}'",
                    lineInfo);
            }
        }


        public static IXamlAstValueNode GetStylesheet(AstTransformationContext context, IXamlAstValueNode node)
        {

            if (!context.TryGetItem<XamlStylesheetContainer>(out var stylesheetContainer))
            {
                stylesheetContainer = new XamlStylesheetContainer(
                    new XamlStaticOrTargetedReturnMethodCallNode(node, 
                    TypesContainer.StyleSheet.GetAllProperties().First(c => c.Name == "Current").Getter!, 
                    null));
                context.SetItem(stylesheetContainer);
            }

            return stylesheetContainer.Node;
        }

        public static IXamlAstValueNode GetStyleName(AstTransformationContext context, XamlAstObjectNode node)
        { 

            if (!context.TryGetItem<XamlStyleContainer>(out var styleContainer))
            {
                styleContainer = new XamlStyleContainer(new XamlConstantNode(node, context.Configuration.WellKnownTypes.String, ""));
                context.SetItem<XamlStyleContainer>(styleContainer);
            }

            return styleContainer.Node;
        }

        /// <summary>
        /// Retrieves the code-behind's CLR type the transformers are currently working on.
        /// </summary> 
        public static IXamlType CodeBehindClrType(this AstTransformationContext context)
        {
            return context.RootObject.Type.GetClrType();
        }

        public static bool FindXDirectiveAsAny(this XamlAstObjectNode valueNode, string xDirectiveName,
             [NotNullWhen(true)] out XamlAstXmlDirective? directive,
             [NotNullWhen(true)] out IXamlAstValueNode? foundValue)
        {
            var allDirectives = valueNode.Children
                .OfType<XamlAstXmlDirective>()
                .Where(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == xDirectiveName)
                .ToArray();

            if (allDirectives.Length == 0)
            {
                directive = null;
                foundValue = null;
                return false;
            }

            if (allDirectives.Length > 1)
                throw new XamlLoadException($"x:{xDirectiveName} can only exists once on a type!", valueNode);

            directive = allDirectives[0];
            if (directive.Values.Count == 0)
            {
                throw new XamlLoadException(
                    $"x:{xDirectiveName} must have a single value at least.", directive);
            }
            foundValue = directive.Values[0];
            return true;
        }

        /// <summary>
        /// Extracts a x: directive from a node in text form.
        /// </summary>
        /// <param name="valueNode">Node to extract from</param>
        /// <param name="xDirectiveName">Directive to find</param>
        /// <param name="foundValue">Found value (if present, otherwise null)</param>
        /// <returns>If the directive was present</returns>
        /// <exception cref="XamlLoadException">THrown if multiple nodes are found or if a node has multiple values.</exception>
        public static bool ExtractXDirectiveAsText(this XamlAstObjectNode valueNode, string xDirectiveName,
            [NotNullWhen(true)] out XamlAstXmlDirective? directive,
            [NotNullWhen(true)] out string? foundValue)
        {
            if (!FindXDirectiveAsAny(valueNode, xDirectiveName, out directive, out var foundNode))
            {
                foundValue = null;
                return false;
            }

            // There should be at least 1 value, namely the main value
            // other optional values can be retrieved by this method's caller.
            if (foundNode is not XamlAstTextNode text)
            {
                throw new XamlLoadException(
                    $"x:{xDirectiveName} must have a single string value.", directive);
            }

            // Remove x:directive, as we no longer need it.
            valueNode.Children.Remove(directive);

            foundValue = text.Text; 
            return true;
        }

        /// <summary>
        /// Finds a x: directive with a x:Static value from a node.
        /// </summary>
        /// <param name="valueNode">Node to extract from</param>
        /// <param name="xDirectiveName">Directive to find</param>
        /// <param name="foundValue">Found value (if present, otherwise null)</param>
        /// <returns>If the directive was present</returns>
        /// <exception cref="XamlLoadException">THrown if multiple nodes are found or if a node has multiple values.</exception>
        public static bool FindXDirectiveAsStatic(this XamlAstObjectNode valueNode, string xDirectiveName,
            [NotNullWhen(true)] out XamlAstXmlDirective? directive,
            [NotNullWhen(true)] out XamlStaticExtensionNode? foundValue)
        {
            if (!FindXDirectiveAsAny(valueNode, xDirectiveName, out directive, out var foundNode))
            {
                foundValue = null;
                return false;
            }

            // There should be at least 1 value, namely the main value
            // other optional values can be retrieved by this method's caller.
            if (foundNode is not XamlStaticExtensionNode xStatic)
            {
                throw new XamlLoadException(
                    $"x:{xDirectiveName} must have a single x:Static value.", directive);
            }
              
            foundValue = xStatic; 
            return true;
        } 
    }
}
