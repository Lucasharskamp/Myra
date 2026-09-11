using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    public sealed class MyraMethodWrapperNode : XamlAstNode, IXamlAstValueNode,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public MyraMethodWrapperNode(
            IXamlLineInfo lineInfo,
            IXamlMethod method,
            IXamlType declaringType,
            IXamlType senderType,
            IXamlType argsType)
            : base(lineInfo)
        {
            Method = method;
            DeclaringType = declaringType;
            SenderType = senderType;
            ArgsType = argsType;
            Type = new XamlAstClrTypeReference(lineInfo, DeclaringType, false);
        }

        public IXamlMethod Method { get; }

        public IXamlType DeclaringType { get; }

        public IXamlType SenderType { get; }

        public IXamlType ArgsType { get; }

        public IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            context.Emitter
            .Ldarg(1)
            .EmitCall(Method);

            return XamlILNodeEmitResult.Type(0, SenderType);
        }
    }
}
