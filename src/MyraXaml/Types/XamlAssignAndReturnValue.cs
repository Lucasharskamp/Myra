using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// This value node is used for x:Name assignments, where we want the newly created object in the Population method to be tied 
    /// to a code-behind reference. However, existing IL Emitters break the entire flow.
    /// </summary>
    public sealed class XamlAssignAndReturnValueNode : XamlAstNode, IXamlAstManipulationNode,
         IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlMethod AssignMethod { get; }

        public IXamlAstTypeReference Type { get; }
        public IXamlAstValueNode SourceObject { get; }
        public IXamlAstValueNode TargetObject { get; }

        public XamlAssignAndReturnValueNode(
            IXamlLineInfo lineInfo,
            IXamlMethod assignmentMethod,
            IXamlAstValueNode sourceObject,
            IXamlAstValueNode targetObject)
            : base(lineInfo)
        {
            AssignMethod = assignmentMethod;
            SourceObject = sourceObject;
            TargetObject = targetObject;
            Type = new XamlAstClrTypeReference(lineInfo, assignmentMethod.ReturnType, false);
        } 

        public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            // Define source object as value to assign without breaking the existing stack
            var temp = codeGen.DefineLocal(SourceObject.Type.GetClrType());
            codeGen.Emit(OpCodes.Stloc, temp);
            // Get target object to assign to
            context.Emit(TargetObject, codeGen, TargetObject.Type.GetClrType());
            codeGen.Emit(OpCodes.Ldloc, temp);

            // TargetObject.Assignment(SourceObject)
            codeGen.Emit(OpCodes.Callvirt, AssignMethod); 

            // return void
            return XamlILNodeEmitResult.Type(0, null);
        }
    }
}
