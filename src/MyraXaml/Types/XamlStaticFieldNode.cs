using System;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// Used for retrieving the value of a static field.
    /// </summary>
    public sealed class XamlStaticFieldNode : XamlAstNode, IXamlAstValueNode,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlField Field { get; }

        public XamlStaticFieldNode(IXamlLineInfo lineInfo, IXamlField field)
            : base(lineInfo)
        {
            if (!field.IsStatic)
                throw new ArgumentException(
                    $"Field '{field.Name}' must be static.",
                    nameof(field));

            Field = field;
            Type = new XamlAstClrTypeReference(lineInfo, field.FieldType, false);
        }

        public IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter codeGen)
        {
            codeGen.Emit(OpCodes.Ldsfld, Field);
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
