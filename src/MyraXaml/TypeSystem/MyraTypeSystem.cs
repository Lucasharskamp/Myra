using Myra.Attributes; 
using System.Collections.Generic; 
using XamlX.TypeSystem;

namespace Myra.Xaml.TypeSystem
{
    public sealed class MyraTypeSystem : IXamlTypeSystem
    {
        private readonly ReflectionCache _cache;

        public XamlTypeWellKnownTypes WellKnownTypes { get; }

        public IEnumerable<IXamlAssembly> Assemblies => _cache.Assemblies;

        public MyraTypeSystem()
        {
            _cache = new ReflectionCache();
            _cache.RegisterAssembly(typeof(System.ComponentModel.ITypeDescriptorContext).Assembly);
            _cache.RegisterAssembly(typeof(System.ComponentModel.TypeConverterAttribute).Assembly);
            _cache.RegisterAssembly(typeof(ContentAttribute).Assembly);
            _cache.RegisterAssembly(typeof(System.ComponentModel.TypeConverterAttribute).Assembly);
            WellKnownTypes = new XamlTypeWellKnownTypes(this);
        }

        public IXamlAssembly? FindAssembly(string substring)
        {
            return _cache.FindAssembly(substring);
        }

        public IXamlType FindType(string name)
        {
            return _cache.FindType(name);
        }

        public IXamlType FindType(string name, string assembly)
        {
            return _cache.FindType(name);
        }
    }
}
