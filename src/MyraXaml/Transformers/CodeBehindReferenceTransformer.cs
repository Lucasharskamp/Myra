using Mono.Cecil;
using Mono.Cecil.Cil;
using Myra.Xaml.Compiler;
using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic; 
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    /// <summary>
    /// This transformer does two things: <br/>
    /// <br/>
    /// 1. Converts event attributes such as <c>&lt;Button Click="OnClick" /&gt;</c>
    /// into an XamlX delegate value targeting the event's add method. <br/>
    /// The resulting AST is emitted by the normal XamlILCompiler and targets the code-behind method "OnClick".<br/>
    /// <br/>
    /// 2. The properties set via x:Bind are tied to their ViewModel properties.
    /// </summary>
    public sealed class CodeBehindReferenceTransformer(MyraBindingCompilationContext bindings) : IXamlAstTransformer
    {
        private readonly MyraBindingCompilationContext _bindings = bindings;

        public IXamlAstNode Transform(AstTransformationContext context,  IXamlAstNode node)
        {
            if (node is not XamlAstXamlPropertyValueNode valueNode)
                return node;

            if (valueNode.Property is not XamlAstClrProperty source)
                return node;

            if (valueNode.Values.Count != 1)
                return node;
             
            var value = valueNode.Values[0];
            var rootClrType = context.CodeBehindClrType();


            // check for event
            if (value is XamlAstTextNode text)
            {
                var invokedValue = text.Text?.Trim();

                if (string.IsNullOrEmpty(invokedValue))
                {
                    throw new XamlLoadException($"Property '{source.Name}' requires a value.", source);
                }

                // Find an event with the same name as the "property".
                var sourceEvent = source.DeclaringType
                    .GetAllEvents()
                    .FirstOrDefault(e => e.Name == source.Name);

                if (sourceEvent == null || sourceEvent.Add == null)
                    return node;

                var delegateType = context.Configuration.TypeSystem.FindType("Myra.Events.MyraEventHandler")
                    ?? throw new XamlLoadException(
                        $"Unable to determine the delegate type for event '{sourceEvent.Name}'.",
                        valueNode);

                var invoke = delegateType
                    .FindMethod(m => m.Name == "Invoke");

                if (invoke == null)
                {
                    throw new XamlLoadException(
                        $"Unable to find Invoke method on event delegate '{delegateType.GetFqn()}'.",
                        valueNode);
                }

                var eventHandler = FindEventHandler(rootClrType, invokedValue!, invoke);

                if (eventHandler == null)
                {
                    throw new XamlLoadException(
                        $"Unable to find event handler '{invokedValue}' on " +
                        $"'{rootClrType.GetFqn()}' compatible with event " +
                        $"'{sourceEvent.Name}'.",
                        valueNode);
                }

                var delegateNode = new XamlLoadMethodDelegateNode(valueNode, context.RootObject, delegateType, eventHandler);
                return new XamlPropertyAssignmentNode(valueNode, source,
                    [new XamlDirectCallPropertySetter(sourceEvent.Add)],
                    [delegateNode]);
            }


            // now we do {x:Bind} directives, which we can place on properties or fields. 
            if (value is not XamlAstObjectNode bindNode)
                return node;
             
            if (bindNode.Arguments.FirstOrDefault() is not XamlAstTextNode propertyTextNode)
                return node;

            // get view model. It must be specified already in the root if one wants to use x:Bind
            if (!context.TryGetItem<XamlViewModelContainer>(out var viewModel))
            {
                throw new XamlLoadException("A x:ViewModel must be assigned to the document for x:Bind bindings!", propertyTextNode);
            }

            var parameters = bindNode.Children.Cast<XamlAstXamlPropertyValueNode>()
                .ToDictionary(
                    p => ((XamlAstClrProperty)p.Property).Name, 
                    p => ((XamlAstTextNode)p.Values.First()).Text);
           

            // By default, binding goes one-way, unless specified otherwise
            // if the override is not correctly filled in, we throw.
            BindingMode bindingMode = BindingMode.OneWay;
            if (parameters.TryGetValue("Mode", out var b) && !Enum.TryParse(b, out bindingMode))
            {
                throw new XamlLoadException($"{b}' is not a valid value of type 'BindingMode'",
                    propertyTextNode);
            }

            // get the source property in the code-behind (or a field as fallback)
            var targetProperty = viewModel.Type
                .GetAllProperties()
                .FirstOrDefault(e => e.Name == propertyTextNode.Text && e.Getter != null);

            if (targetProperty == null)
            {
                throw new XamlLoadException(
                        $"No property named '{propertyTextNode.Text}' was not found in code-behind class '{rootClrType.FullName}'",
                        propertyTextNode); 
            }

            TransformerHelpers.EnsureAssignability(propertyTextNode, source, targetProperty.Name, targetProperty.PropertyType);
             
            var sourceClrProperty = new XamlAstClrProperty(source, targetProperty, context.Configuration);
            var viewModelCall = viewModel.GetPropertyCall(source, context.RootObject);
            var sourceCall = new XamlStaticOrTargetedReturnMethodCallNode(source, sourceClrProperty.Getter!, [viewModelCall]);

            var initialAssignment = new XamlPropertyAssignmentNode(source, source, source.Setters, [sourceCall]);

            if (bindingMode == BindingMode.OneTime)
            {
                return initialAssignment;
            }

            var result = new List<IXamlAstManipulationNode>()
            {
                initialAssignment
            }; 

            // if one-way or two-way, add functionality to listen to property changes.
            if (bindingMode == BindingMode.OneWay || bindingMode == BindingMode.TwoWay)
            { 
                var widgetProperty = source.DeclaringType.Properties.First(f => f.Name == source.Name);

                var binding = _bindings.CreateOneWayBinding(
                       source: valueNode,
                       widgetType: source.DeclaringType,
                       widgetProperty: widgetProperty,
                       viewModelProperty: targetProperty,
                       viewModelSource: viewModel.Property, 
                       sourcePropertyName: propertyTextNode.Text);

                result.Add(new XamlAssignFieldValueNode(value, valueNode.Property.GetClrProperty().DeclaringType, binding.TargetField));

                result.Add(new XamlOneWayBindingNode(
                        propertyTextNode, 
                        viewModel.Property,
                        sourceClrProperty.Name,
                        binding.TargetField,
                        binding.Handler));
            }

            return new XamlManipulationGroupNode(source, result);
        }


        private static IXamlMethod? FindEventHandler(IXamlType rootType, string name,  IXamlMethod delegateInvoke)
        {
            var methods = rootType.FindMethods(m => m.Name == name).OrderByDescending(m => m.Parameters.Count).ToArray();
            if (methods.Length == 0)
                return null;
            
            // first, we try to find a method that has the parameters of the delegate.
            // If not, we use a method without parameters as fallback.
            foreach (var method in methods)
            { 
                if (method.Parameters.Count !=
                    delegateInvoke.Parameters.Count)
                    continue;

                var compatible = true;

                for (var i = 0; i < method.Parameters.Count; i++)
                {
                    var handlerParameter = method.Parameters[i];
                    var delegateParameter = delegateInvoke.Parameters[i];

                    if (!IsCompatible(handlerParameter, delegateParameter))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (compatible)
                    return method;
            }

            // invoke fallback (if it has no parameters)
            var lastMethod = methods.Last();
            if (lastMethod.Parameters.Count == 0)
                return lastMethod;

            return null;
        }

        private static bool IsCompatible(
            IXamlType handlerParameter,
            IXamlType delegateParameter)
        {
            return handlerParameter.Equals(delegateParameter)
                || handlerParameter.IsAssignableFrom(delegateParameter)
                || delegateParameter.IsAssignableFrom(handlerParameter);
        }

        private static MethodDefinition CreateEventAccessor(
                TypeDefinition type,
                ModuleDefinition module,
                string name,
                TypeReference handlerType,
                bool add)
        {
            var method = new MethodDefinition(
                name,
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig |
                MethodAttributes.Virtual,
                module.TypeSystem.Void);

            method.Parameters.Add(
                new ParameterDefinition(
                    "value",
                    ParameterAttributes.None,
                    handlerType));

            var field = type.Fields.First(f => f.Name == "<PropertyChanged>k__BackingField");

            var il = method.Body.GetILProcessor();

            var combine = module.ImportReference(
                add
                    ? typeof(Delegate).GetMethod(
                        nameof(Delegate.Combine),
                        [typeof(Delegate), typeof(Delegate)])!
                    : typeof(Delegate).GetMethod(
                        nameof(Delegate.Remove),
                        [typeof(Delegate), typeof(Delegate)])!);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, combine);

            il.Emit(OpCodes.Castclass, handlerType);

            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);

            type.Methods.Add(method);

            return method;
        }
    }
}
