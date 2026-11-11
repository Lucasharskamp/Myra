

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
        public const string ErrorCodeTypeDoesNotHaveProperty = "MYRA0002";

        /// <summary>
        /// Hint text for <see cref="ErrorCodeTypeDoesNotHaveProperty"/>
        /// </summary>
        public const string ErrorHintTypeDoesNotHaveProperty = "The selected Widget type '{0}' does not have a property named '{1}'";


        /// <summary>
        /// Error if an attribute name is used whose corresponding property does not have a public setter.
        /// </summary>
        public const string ErrorCodePropertyDoesNotHaveSetter = "MYRA0003";

        /// <summary>
        /// Hint text for <see cref="ErrorCodePropertyDoesNotHaveSetter"/>
        /// </summary>
        public const string ErrorHintPropertyDoesNotHaveSetter = "property '{0}' in type '{1}' does not have a setter!";


        /// <summary>
        /// Error if constructor usage doesn't have the correct amount of parameters.
        /// </summary>
        public const string ErrorCodeConstructorInvalidParameters = "MYRA004";

        public const string ErrorHintConstructorInvalidParameters = "No constructor for '{0}' has {1} parameters!";

        /// <summary>
        /// Error if a number must is not 6 or 8 hexadecimal characters
        /// </summary>
        public const string ErrorCodeHexLengthInvalid = "MYRA0005";

        /// <summary>
        /// Hint text for <see cref="ErrorCodeHexLengthInvalid"/>
        /// </summary>
        public const string ErrorHintHexLengthInvalid = "Hex number must have 6 or 8 hexadecimal characters!";

        /// <summary>
        /// Error if a hex is invalid
        /// </summary>
        public const string ErrorCodeHexInvalid = "MYRA0006";

        /// <summary>
        /// Hint text for <see cref="ErrorCodeHexInvalid"/>
        /// </summary>
        public const string ErrorHintHexInvalid = "Hex number is not valid!";


        public const string ErrorCodeEventMethodMissingParameters = "MYRA0007";

        public const string ErrorHintErrorEventMethodMissingParameters = "Method '{0}' is used for an event, but lacks the 'object sender, MyraEventArgs e' parameters.";
        public const string ErrorHintWarningEventMethodMissingParameters =
            "Method '{0}' is used for an event, but lacks the 'object sender, MyraEventArgs e' parameters." +
            "This will prevent AOT compilations from being built!";



        public static bool StringMatchesAnyOf(this string value, params string[] values)
        {
            return values.Any(v => string.Equals(value, v, System.StringComparison.OrdinalIgnoreCase));
        }
    }

}