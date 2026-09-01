using System.Text;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// Data structure containing the contents of a XML file
    /// </summary>
    public sealed class XmlFileSource : IFileSource
    {
        public XmlFileSource(string filePath, string fileContents)  
        {
            FilePath = filePath;
            FileContents = Encoding.UTF8.GetBytes(fileContents);
        }

        /// <summary>
        /// Path of the XML file
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Contents of the XML file (as a data array)
        /// </summary>
        public byte[] FileContents { get; }
    }
}
