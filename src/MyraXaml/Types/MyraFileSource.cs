using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    public sealed class MyraFileSource : IFileSource
    {
        public MyraFileSource(string filePath, byte[] fileContents)
        {
            FilePath = filePath;
            FileContents = fileContents;
        }

        public string FilePath { get; }

        public byte[] FileContents { get; }
    }
}
