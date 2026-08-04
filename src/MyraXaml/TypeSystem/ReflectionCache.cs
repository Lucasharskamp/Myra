using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XamlX.TypeSystem;

namespace Myra.Xaml.TypeSystem
{
    internal sealed class ReflectionCache
    {
        private Dictionary<string, IXamlType> Types { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<MyraAssembly> Assemblies { get; } = [];
         
        public ReflectionCache()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            { 
                RegisterAssembly(asm);
            }
        }

        public void RegisterAssembly(Assembly asm)
        {
            var name = asm.GetName().Name;
            if (Assemblies.Any(a => String.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)))
                return;

            var wrapper = new MyraAssembly(asm);

            Assemblies.Add(wrapper);

            foreach (var type in wrapper.Types)
            {  
                Types[type.FullName] = type;
                Types[type.Name] = type; 
            } 
        }

        public MyraAssembly? FindAssembly(string name)
        {
            return Assemblies.FirstOrDefault(x =>
                x.ReflectionAssembly.GetName().Name!.Contains(name));
        }

        public IXamlType FindType(string name)
        {
            return Types[name];
        }
    }
}
