using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// This value node is used for x:Name and bindings assignments, where we want the newly created object in the Population method to be tied 
    /// to a code-behind reference. However, existing IL Emitters break the entire flow.
    /// </summary>
    public sealed class XamlAssignPropertyValueNode : XamlAstNode, IXamlAstManipulationNode,
         IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlMethod AssignMethod { get; }

        public IXamlAstTypeReference Type { get; }
        public IXamlAstValueNode SourceObject { get; }
        public IXamlAstValueNode TargetObject { get; }

        public XamlAssignPropertyValueNode(
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
            // We need to assign the value (the source object) onto the target property, but without breaking the current CIL stack
            // (The source object is the widget currently being initialized in the build method. "Temp" is assigned this source object)
            // we do this by setting the source and target objects onto the CIL stack. These CIL stack items are then consumed,
            // and the original CIL stack can continue without being interfered with.
            var temp = codeGen.DefineLocal(SourceObject.Type.GetClrType());
            codeGen.Emit(OpCodes.Stloc, temp);
            // Get target object to assign to (the class instance whose property x:Name refers to).
            // Normally this is the code-behind object, but we're leaving the door open
            // for the future in case we want to assign to different objects as well.
            context.Emit(TargetObject, codeGen, TargetObject.Type.GetClrType());
            codeGen.Emit(OpCodes.Ldloc, temp);

            // TargetObject.Assignment(SourceObject)
            codeGen.Emit(OpCodes.Callvirt, AssignMethod); 

            // return void
            return XamlILNodeEmitResult.Type(0, null);
        }
    }
}
