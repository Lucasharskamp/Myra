using Mono.Cecil.Cil;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{ 

    public sealed class MyraCecilLocal : IXamlLocal
    {
        public VariableDefinition Variable { get; }

        public IXamlType Type { get; }


        public MyraCecilLocal(VariableDefinition variable, IXamlType type)
        {
            Variable = variable;
            Type = type;
        }
    }
}
