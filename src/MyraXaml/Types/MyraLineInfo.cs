using XamlX.Ast;

namespace Myra.Xaml.Types
{ 
    internal sealed class MyraLineInfo : IXamlLineInfo
    {
        public int Line { get; set; }

        public int Position { get; set; } 

        public MyraLineInfo(int line, int position)
        {
            Line = line;
            Position = position;
        }
    }
}
