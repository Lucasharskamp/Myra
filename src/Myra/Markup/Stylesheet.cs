using Myra.Graphics2D.UI.Styles; 

namespace Myra.Markup
{
    /// <summary>
    /// x:StyleSheet reference
    /// </summary>
    public sealed class StyleSheet
    {
        /// <summary>
         /// Default constructor
         /// </summary>   
        public StyleSheet(string fullName)
        {
            FullName = fullName;
        }

        /// <summary>
        /// the full namespace and field/property name of the reffered <see cref="Stylesheet"/>
        /// </summary>
        public string FullName { get; }
    }
}
