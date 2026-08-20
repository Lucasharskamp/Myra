namespace Myra.Markup
{
    /// <summary>
    /// For binding in UI element in Xaml to a code-behind property/field reference.
    /// </summary>
    public sealed class Name
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Name(string codeBehindReference)
        {
            CodeBehindReference = codeBehindReference;
        }

        /// <summary>
        /// The name of the field or property to bind the UI element to.
        /// </summary>
        public string CodeBehindReference { get; }
    }
}
