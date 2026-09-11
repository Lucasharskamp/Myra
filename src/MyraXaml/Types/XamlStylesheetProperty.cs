using Myra.Xaml.Helpers;
using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// Gets the stylesheet (or a property of the stylesheet) and returns the result.
    /// </summary>
    internal sealed class XamlStylesheetProperty : XamlAstNode, IXamlAstManipulationNode,
        IXamlAstValueNode, IXamlLocal, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlAstTypeReference Type { get; } 
        private IXamlProperty? Property { get; }
        private bool IsXaml { get; } 

        public XamlStylesheetProperty(IXamlLineInfo lineInfo, AstTransformationContext context, IXamlType? type) : base(lineInfo)
        {
            IsXaml = context.RootObject.Type.GetTypeName() != "Stylesheet"; 
            Property = type == null ? null : TypesContainer.Stylesheet.GetAllProperties().First(p => p.PropertyType == type);
            Type = new XamlAstClrTypeReference(lineInfo, type ?? TypesContainer.Stylesheet, false);
        }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        { 
            if (IsXaml)
            {
                codeGen.Ldarg(2); // Components builder: get P_2 (Stylesheet)
            }
            else
            {
                codeGen.Ldarg(1); // Stylesheet builder: get P_1 (stylesheet)
            }

            if (Property != null)
            {
                codeGen.EmitCall(Property.Getter!, false);
            }
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
