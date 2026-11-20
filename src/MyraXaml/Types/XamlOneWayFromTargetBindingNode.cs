using Myra.Xaml.Helpers;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    public sealed class XamlOneWayFromTargetBindingNode :
        XamlValueWithSideEffectNodeBase,
        IXamlAstManipulationNode,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {  
        public IXamlField TargetField { get; }
        public IXamlMethod Handler { get; }

        public XamlOneWayFromTargetBindingNode(
            IXamlAstValueNode value, 
            IXamlField targetField,
            IXamlMethod handler)
            : base(value, value)
        { 
            TargetField = targetField;
            Handler = handler;

            Type = value.Type;
        }

        public override IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter il)
        {
            /*
             * this.__MyraBindingTarget0.PropertyChanged += this.__MyraBinding_0;
             */

            // Get widget instance
            il.Ldarg(1);
            il.Emit(OpCodes.Ldfld, TargetField);

            // Create delegate
            il.Ldarg(1);
            il.Emit(OpCodes.Ldftn, Handler);
            il.Emit(OpCodes.Newobj, TypesContainer.PropertyChangedEventHandler);

            // Subscribe
            var eventAdd = TargetField.FieldType
                .GetAllEvents()
                .First(e => e.Name == nameof(INotifyPropertyChanged.PropertyChanged))
                .Add!;

            il.EmitCall(eventAdd, true);

            return XamlILNodeEmitResult.Type(0, null);
        }
    }
}
