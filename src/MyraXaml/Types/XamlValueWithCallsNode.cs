using System.Collections.Generic;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL; 

namespace Myra.Xaml.Types
{
    internal class XamlCallsNode : XamlAstNode, IXamlAstManipulationNode,
        IXamlAstValueNode, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlAstTypeReference Type { get; }
        private ICollection<IXamlAstValueNode> Calls { get; }

        public XamlCallsNode(IXamlLineInfo lineInfo, IXamlAstTypeReference type, ICollection<IXamlAstValueNode> calls) : base(lineInfo)
        {
            Type = type;
            Calls = calls;
        }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            foreach (var call in Calls)
            {
                context.Emit(call, codeGen, call.Type.GetClrType());
            }

            return XamlILNodeEmitResult.Type(0, null);
        }
    }
}
