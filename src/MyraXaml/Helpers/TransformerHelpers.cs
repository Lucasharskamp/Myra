using XamlX;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace Myra.Xaml.Helpers
{
    internal static class TransformerHelpers
    {
        public static void EnsureAssignability(IXamlLineInfo lineInfo, IXamlProperty targetProperty, string sourceFieldName, IXamlType sourceType)
        {
            if (!targetProperty.PropertyType.IsAssignableFrom(sourceType))
            {
                throw new XamlLoadException(
                    $"Property '{targetProperty.Name}' from '{targetProperty.DeclaringType.FullName}' is not " +
                    $"assignable to '{sourceFieldName}' from '{targetProperty.DeclaringType.FullName}'",
                    lineInfo);
            }
        }
    }
}
