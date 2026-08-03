namespace Myra.Xaml
{
    /// <summary>
    /// Stores the result of a compilation
    /// </summary>
    public sealed class CompileResult
    {
        /// <summary>
        /// Whether it succeeded.
        /// </summary>
        public bool Success { get; }
        
        /// <summary>
        /// Whether the contents have been written to a file. 
        /// </summary>
        public bool WrittenFile { get; }

        public CompileResult(bool success, bool writtenFile = false)
        {
            Success = success;
            WrittenFile = writtenFile;
        }
    }
}
