using Myra.Xaml.TypeSystem;

namespace Myra.Xaml.Compiler
{
    public sealed class MyraXamlContext
    {
        public MyraTypeSystem TypeSystem { get; }

        public NamespaceRegistry Namespaces { get; }

        public MyraXamlConfiguration Configuration { get; }

        public MyraXamlContext()
        {
            TypeSystem = new MyraTypeSystem();

            Namespaces = new NamespaceRegistry();

            Configuration = new MyraXamlConfiguration();
        }
    }
}
