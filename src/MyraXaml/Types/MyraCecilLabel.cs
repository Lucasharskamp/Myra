using Mono.Cecil.Cil;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    public sealed class MyraCecilLabel : IXamlLabel
    {
        public Instruction Label { get; }

        public MyraCecilLabel(Instruction label)
        {
            Label = label;
        }
    }
}
