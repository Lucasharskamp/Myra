using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{ 
    internal sealed class MyraAssembly : IXamlAssembly
    {
        private readonly Assembly _assembly;

        private readonly Dictionary<string, IXamlType> _types = new();

        public string Name =>
            _assembly.GetName().Name!;

        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
            Array.Empty<IXamlCustomAttribute>();

        public Assembly ReflectionAssembly => _assembly;

        public IEnumerable<IXamlType> Types => _types.Values;


        public MyraAssembly(Assembly assembly)
        {
            _assembly = assembly;

            var asm = AssemblyDefinition.ReadAssembly(assembly.Location);
            foreach (var type in assembly.GetTypes())
            {
                if (type.FullName != null && !type.IsNested)
                {
                    _types[type.FullName] = new MyraCecilType(type, asm, this);
                }
            }
        }


        public IXamlType? FindType(string fullName)
        {
            _types.TryGetValue(fullName, out var type);

            return type;
        }


        public bool Equals(IXamlAssembly? other)
        {
            return other is MyraAssembly ma &&
                   ma._assembly == _assembly;
        }


        public override bool Equals(object? obj)
        {
            return obj is IXamlAssembly other &&
                   Equals(other);
        }


        public override int GetHashCode()
        {
            return _assembly.GetHashCode();
        }

        public override string ToString()
        {
            return _assembly.FullName;
        }
    }
}
