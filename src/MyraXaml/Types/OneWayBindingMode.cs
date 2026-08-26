using Mono.Cecil;
using Myra.Xaml.Helpers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    public sealed class MyraOneWayBindingNode :
        XamlValueWithSideEffectNodeBase,
        IXamlAstManipulationNode,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlAstValueNode Source { get; }
        public IXamlProperty ViewModelCall { get; }
        public string SourcePropertyName { get; }

        public IXamlField TargetField { get; }
        public IXamlMethod Handler { get; }

        public MyraOneWayBindingNode( 
            IXamlAstValueNode value,
            IXamlAstValueNode source,
            IXamlProperty viewModel,
            string sourcePropertyName,
            IXamlField targetField,
            IXamlMethod handler)
            : base(source, value)
        {
            Source = source;
            ViewModelCall = viewModel;
            SourcePropertyName = sourcePropertyName;
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
             * Subscribe:
             *
             * this.ViewModel.PropertyChanged +=
             *     this.__MyraBinding_0;
             */

            il.Ldarg(1);
            il.EmitCall(ViewModelCall.Getter!, false); 

            il.Ldarg(1);
            il.Emit(OpCodes.Ldftn, Handler);

            il.Emit(
                OpCodes.Newobj,
                TypesContainer
                    .PropertyChangedEventHandler
                    .Constructors
                    .First(c => c.Parameters.Count == 2));


            // get PropertyChanged from ViewModelCall
            var eventAdd = ViewModelCall.Getter!.ReturnType.GetAllEvents()
                    .First(e => e.Name == nameof(INotifyPropertyChanged.PropertyChanged)).Add!;
             
            il.EmitCall(eventAdd, true);

            return XamlILNodeEmitResult.Type(0, null);
        }
    }

    public sealed class MyraOneWayBinding
    {
        public IXamlField TargetField { get; }
        public IXamlMethodBuilder<IXamlILEmitter> Handler { get; }

        public MyraOneWayBinding(
            IXamlField targetField,
            IXamlMethodBuilder<IXamlILEmitter> handler)
        {
            TargetField = targetField;
            Handler = handler;
        }
    }
}
