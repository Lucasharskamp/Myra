using XamlX;
using XamlX.Ast;
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
    }
}
