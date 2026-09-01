using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    public sealed class XamlAssignLocalValueNode : XamlAstNode, IXamlAstManipulationNode,
        IXamlLocal, IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlAstTypeReference Type { get; }
        public IXamlType SourceType { get; }
        public IXamlField TargetField { get; }

        public XamlAssignLocalValueNode(
            IXamlLineInfo lineInfo,
            IXamlType sourceType,
            IXamlField targetField)
            : base(lineInfo)
        {
            SourceType = sourceType;
            TargetField = targetField;
            Type = new XamlAstClrTypeReference(lineInfo, targetField.FieldType, false);
        }

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            var temp = codeGen.DefineLocal(SourceType);
            codeGen.Stloc(temp);
            codeGen.Ldarg(1);
            codeGen.Ldloc(temp);
            codeGen.Stfld(TargetField);
            codeGen.Pop();
            return XamlILNodeEmitResult.Type(0, null);
        }
    }
}
