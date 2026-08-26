using System.Text;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// Data structure containing the contents of a .xaml file
    /// </summary>
    public sealed class XamlFileSource : IFileSource
    {
        public XamlFileSource(string filePath, string fileContents) 
            : this(filePath, Encoding.UTF8.GetBytes(fileContents))
        { }

        public XamlFileSource(string filePath, byte[] fileContents)
        {
            FilePath = filePath;
            FileContents = fileContents;
        }

        /// <summary>
        /// Path of the .xaml file
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Contents of the .xaml file (as a data array)
        /// </summary>
        public byte[] FileContents { get; }
    }
}
