using Mono.Cecil;
using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit; 
using XamlX;
using XamlX.Ast;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Compiler
{
    public sealed class MyraBindingCompilationContext
    {
        private IXamlTypeSystem TypeSystem { get; }

        /// <summary>
        /// the Get(string) method to retrieve a stylesheet.
        /// </summary>
        public static IXamlMethod GetStylesheet { get; set; } = default!;

        /// <summary>
        /// The Get(string) method from a module reference.
        /// </summary>
        public static MethodReference GetStylesheetDefinition { get; set; } = default!;

        /// <summary>
        /// Nodes which have x:Uid assigned to them (for use for localization and other tools)
        /// todo: use them for localization
        /// </summary>
        private Dictionary<string, XamlAstObjectNode> IdentifiedNodes { get; } = [];

        /// <summary>
        /// Nodes which have x:FieldModifier assigned to them (For use when declaring a node as a field, if need be)
        /// Todo: use these created fields.
        /// </summary>
        private Dictionary<string, XamlVisibility> FieldModifiers { get; } = [];


        public MyraBindingCompilationContext(IXamlTypeSystem typeSystem)
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
            IdentifiedNodes.Clear();
            _handlerIndex = 0;
            _typeBuilder = typeBuilder;
        }

        public void RegisterNodeIdentity(string identity, XamlAstObjectNode node)
        {
            if (IdentifiedNodes.TryGetValue(identity, out _))
            {
                throw new XamlLoadException($"A node with identity {identity} already exists!", node);
            }

            IdentifiedNodes.Add(identity, node);
        }

        public void RegisterNodeFieldModifier(string line, XamlVisibility xamlVisibility)
        {
            FieldModifiers.Add(line, xamlVisibility);
        }

        public MyraOneWayBinding CreateOneWayBinding(
                XamlAstNode source,
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
             * Persistent reference to the XAML-created Widget.
             */
            var targetField = TypeBuilder.DefineField(
                widgetType,
                $"__MyraBindingTarget{_handlerIndex}",
                FieldModifiers.TryGetValue($"{source.Line}-{source.Position}", out var visibility) ? visibility : XamlVisibility.Private,
                false);

            /*
             * private void __MyraBinding_{index}(
             *     object sender,
             *     PropertyChangedEventArgs e)
             */
            var handler = TypeBuilder.DefineMethod(
                TypeSystem.WellKnownTypes.Void,
                [TypeSystem.WellKnownTypes.Object, TypesContainer.PropertyChangedEventArgs],
                $"__MyraBinding_{_handlerIndex}",
                XamlVisibility.Private,
                false,
                false);

            _handlerIndex++;

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
