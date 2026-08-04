using Myra.Xaml.TypeSystem; 
using XamlX.Transform;

namespace Myra.Xaml.Compiler
{ 
    public sealed class MyraXamlConfiguration
    {
        internal XamlLanguageTypeMappings TypeMappings { get; }

        public MyraXamlConfiguration()
        {
            TypeMappings = new XamlLanguageTypeMappings(new MyraTypeSystem());
        } 
    }
}
