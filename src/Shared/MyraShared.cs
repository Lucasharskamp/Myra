

using System.Linq;

namespace Myra
{
    /// <summary>
    /// Shared assets between VSIX and Compiler projects.
    /// </summary>
    public static class MyraShared
    {
        /// <summary>
        /// Namespace for default Myra widgets
        /// </summary>
        public const string DefaultWidgetsNamespace = "Myra.Graphics2D.UI";

        /// <summary>
        /// Namespace plus type name of the base Widget class.
        /// </summary>
        public const string WidgetAddress = $"{DefaultWidgetsNamespace}.Widget";

        /// <summary>
        /// Default Myra schema
        /// </summary>
        public const string XamlMyraSchema = "https://github.com/MyraUI/Myra";

        /// <summary>
        /// Default Xaml schema
        /// </summary>
        public const string Xaml2006Schema = "http://schemas.microsoft.com/winfx/2006/xaml";

        /// <summary>
        /// Prefix for "clr-namespace" to indicate where to load custom widget types from, defined by the programmer.
        /// </summary>
        public const string ClrNamespacePrefix = "clr-namespace:";


        /// <summary>
        /// Replication of the common C# error CS0426; shown if a type does not exist.
        /// </summary>
        public const string ErrorCodeTypeNotExist = "MYRA0001";
        
        /// <summary>
        /// Hint text for <see cref="ErrorCodeTypeNotExist"/>
        /// </summary>
        public const string ErrorHintTypeNotExist = "Type '{0}' does not exist. Are you missing a XAML namespace reference?";

        /// <summary>
        /// Error if an attribute name is used which has no corresponding property on the type.
        /// </summary>
        public const string ErrorTypeDoesNotHaveProperty = "MYRA0002";

        /// <summary>
        /// Hint text for <see cref="ErrorTypeDoesNotHaveProperty"/>
        /// </summary>
        public const string ErrorHintDoesNotHaveProperty = "The selected Widget type '{0}' does not have a property named '{1}'";



        public static bool StringMatchesAnyOf(this string value, params string[] values)
        {
            return values.Any(v => string.Equals(value, v, System.StringComparison.OrdinalIgnoreCase));
        }
    }

}