using System.Diagnostics.CodeAnalysis;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;
using static XamlX.Parsers.CommaSeparatedParenthesesTreeParser;

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

        /// <summary>
        /// Retrieves the code-behind's CLR type the transformers are currently working on.
        /// </summary> 
        public static IXamlType CodeBehindClrType(this AstTransformationContext context)
        {
            return context.RootObject.Type.GetClrType();
        }

        /// <summary>
        /// Extracts a x: directive from a node.
        /// </summary>
        /// <param name="valueNode">Node to extract from</param>
        /// <param name="xDirectiveName">Directive to find</param>
        /// <param name="foundValue">Found value (if present, otherwise null)</param>
        /// <returns>If the directive was present</returns>
        /// <exception cref="XamlLoadException">THrown if multiple nodes are found or if a node has multiple values.</exception>
        public static bool ExtractXDirective(this XamlAstObjectNode valueNode, string xDirectiveName,
            [NotNullWhen(true)] out XamlAstXmlDirective? directive,
            [NotNullWhen(true)] out string? foundValue)
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

            var foundDirective = allDirectives[0];

            // There should be at least 1 value, namely the main value
            // other optional values can be retrieved by this method's caller.
            if (foundDirective.Values.Count == 0 ||
                foundDirective.Values[0] is not XamlAstTextNode text)
            {
                throw new XamlLoadException(
                    $"x:{xDirectiveName} must have a single string value.", foundDirective);
            }

            // Remove x:directive, as we no longer need it.
            valueNode.Children.Remove(foundDirective);

            foundValue = text.Text;
            directive = foundDirective;
            return true;
        }
    }
}
