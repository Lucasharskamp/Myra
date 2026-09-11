 using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform; 

namespace Myra.Xaml.Types
{
    internal class XamlCallComponentParameterNode : XamlAstNode, IXamlAstValueNode,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlCallComponentParameterNode(IXamlLineInfo lineInfo, AstTransformationContext context) : base(lineInfo) 
        {
            Type = context.RootObject.Type;
        }
        public IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            codeGen.Ldarg(1);
            return XamlILNodeEmitResult.Type(0, Type.GetClrType()); 
        }
    }
}
