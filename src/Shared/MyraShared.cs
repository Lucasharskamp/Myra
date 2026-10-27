

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
        /// x namespace for Xaml files, to indicate we're using Myra assets
        /// </summary>
        public const string XamlMyraNamespace = "https://github.com/MyraUI/Myra";

        /// <summary>
        /// Prefix for "clr-namespace" to indicate where to load custom widget types from, defined by the programmer.
        /// </summary>
        public const string ClrNamespacePrefix = "clr-namespace:";

    }

}