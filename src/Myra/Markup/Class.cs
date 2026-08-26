
namespace Myra.Markup
{
    /// <summary>
    /// the x:Class property can be used to direct to a different code-behind than the 
    /// (assembly's root namespace) + (folder structure) + (xaml file name) might suggest.
    /// </summary>
    public sealed class Class
    {
        /// <summary>
        /// Default constructor
        /// </summary>   
        public Class(string fullName)
        {
            FullName = fullName;
        } 

        /// <summary>
        /// the full namespace and classname of the reffered to class.
        /// </summary>
        public string FullName { get; }
    }
}
