using System;
using System.Collections.Generic;
using System.Text;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// Node to force a '<see langword="this"/>' reference in IL code,
    /// when <see cref="XamlAstContextLocalNode"/> does not cut it.
    /// </summary>
    public sealed class XamlAstThisNode :
            XamlAstNode,
            IXamlAstValueNode,
            IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlAstThisNode(IXamlLineInfo lineInfo, IXamlType type)
            : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(this, type, false);
        }
         
        public IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter codeGen)
        {
            codeGen.Ldarg_0();
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
