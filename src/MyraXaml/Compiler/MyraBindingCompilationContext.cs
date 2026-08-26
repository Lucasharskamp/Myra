using Mono.Cecil;
using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using XamlX.Ast;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Compiler
{
    public sealed class MyraBindingCompilationContext
    {
        private CecilTypeSystem TypeSystem { get; }
        public MyraBindingCompilationContext(CecilTypeSystem typeSystem)
        {
            TypeSystem = typeSystem;
        }

        
        private int _handlerIndex;
        private IXamlTypeBuilder<IXamlILEmitter>? _typeBuilder;
        private IXamlTypeBuilder<IXamlILEmitter> TypeBuilder {
            get => _typeBuilder ?? throw new InvalidOperationException("Setup(..) must have been called!");
        }

        public void Setup(IXamlTypeBuilder<IXamlILEmitter> typeBuilder)
        {
            _handlerIndex = 0;
            _typeBuilder = typeBuilder;
        }

        public IXamlMethodBuilder<IXamlILEmitter> DefineHandler(
               IXamlType eventHandlerType,
               IXamlType eventArgsType)
        {
            var name = $"__MyraBinding_{_handlerIndex++}";

            return TypeBuilder.DefineMethod(
                TypeSystem.WellKnownTypes.Void,
                [TypeSystem.WellKnownTypes.Object, eventArgsType],
                name,
                XamlVisibility.Private,
                false,
                false);
        }

        public MyraOneWayBinding CreateOneWayBinding( 
                IXamlType widgetType,
                IXamlProperty widgetProperty,
                IXamlProperty viewModelProperty,
                IXamlProperty viewModelSource, 
                string sourcePropertyName)
        { 
            if (widgetProperty.Setter == null)
                throw new InvalidOperationException(
                    $"Target property '{widgetProperty.Name}' has no setter.");

            if (viewModelSource.Getter == null)
                throw new InvalidOperationException(
                    $"Source property '{viewModelSource.Name}' has no getter.");

            if (viewModelSource.Getter == null)
                throw new InvalidOperationException(
                    $"ViewModel property '{viewModelSource.Name}' has no getter.");

            /*
             * Persistent reference to the XAML-created object.
             *
             * Example:
             *
             * private Button __MyraBindingTarget0;
             */
            var targetField = TypeBuilder.DefineField(
                widgetType,
                $"__MyraBindingTarget{_handlerIndex++}",
                XamlVisibility.Private,
                false);

            /*
             * private void __MyraBinding_0(
             *     object sender,
             *     PropertyChangedEventArgs e)
             */
            var handler = TypeBuilder.DefineMethod(
                TypeSystem.WellKnownTypes.Void,
                [TypeSystem.WellKnownTypes.Object, TypesContainer.PropertyChangedEventArgs],
                $"__MyraBinding_{_handlerIndex++}",
                XamlVisibility.Private,
                false,
                false);

            EmitOneWayBindingHandler(
                handler,
                targetField,
                widgetProperty,
                viewModelProperty,
                viewModelSource,
                sourcePropertyName);

            return new MyraOneWayBinding(targetField, handler);
        }

        private void EmitOneWayBindingHandler(
            IXamlMethodBuilder<IXamlILEmitter> method,
            IXamlField targetField,
            IXamlProperty widgetProperty,
            IXamlProperty viewModelProperty,
            IXamlProperty viewModelSource,
            string sourcePropertyName)
        {
            var il = method.Generator;

            var propertyName = TypesContainer.PropertyChangedEventArgs
                .GetAllProperties()
                .First(x =>
                    x.Name == nameof(PropertyChangedEventArgs.PropertyName));

            var propertyNameGetter = propertyName.Getter
                ?? throw new InvalidOperationException(
                    "PropertyChangedEventArgs.PropertyName has no getter.");

            var stringIsNullOrEmpty =
                TypeSystem.WellKnownTypes.String.FindMethod(
                    m => m.IsStatic &&
                         m.Name == nameof(string.IsNullOrEmpty) &&
                         m.Parameters.Count == 1)
                ?? throw new InvalidOperationException();

            var stringEquals =
                TypeSystem.WellKnownTypes.String.FindMethod(
                    m => m.IsStatic &&
                         m.Name == nameof(string.Equals) &&
                         m.Parameters.Count == 2)
                ?? throw new InvalidOperationException();

            var returnLabel = il.DefineLabel();

            // ---------------------------------------------------------
            // if (string.IsNullOrEmpty(e.PropertyName))
            //     return;
            // ---------------------------------------------------------

            il.Ldarg(2);
            il.EmitCall(propertyNameGetter, false);

            il.Emit(OpCodes.Call, stringIsNullOrEmpty);
            il.Brtrue(returnLabel);

            // ---------------------------------------------------------
            // if (e.PropertyName != "ButtonsEnabled")
            //     return;
            // ---------------------------------------------------------

            il.Ldarg(2);
            il.EmitCall(propertyNameGetter, false);

            il.Emit(OpCodes.Ldstr, sourcePropertyName);

            il.Emit(OpCodes.Call, stringEquals);
            il.Brfalse(returnLabel);

            // ---------------------------------------------------------
            // target
            //
            // this._MyraBindingTarget{index}
            // ---------------------------------------------------------
            il.Ldarg(0);
            il.Emit(OpCodes.Ldfld, targetField);

            // ---------------------------------------------------------
            // source
            //
            // this.ViewModel.ButtonsEnabled
            // ---------------------------------------------------------

            il.Ldarg(0);
            il.EmitCall(viewModelSource.Getter!, false); 
            il.EmitCall(viewModelProperty.Getter!, false);


            // ---------------------------------------------------------
            // target assignment
            //
            // this.__MyraBindingTarget0.{widgetProperty} =
            //     this.ViewModel.ButtonsEnabled;
            // ---------------------------------------------------------
            il.EmitCall(widgetProperty.Setter!, true);

            il.MarkLabel(returnLabel);
            il.Ret();
        }
    }
}
