using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System;
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
    /// Converts event attributes such as <c>&lt;Button Click="OnClick" /&gt;</c>
    /// into an XamlX delegate value targeting the event's add method. <br/>
    /// The resulting AST is emitted by the normal XamlILCompiler.<br/>
    /// <br/>
    /// The properties set via Binding are tied to their code-behind properties.
    /// </summary>
    public sealed class CodeBehindReferenceTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context,  IXamlAstNode node)
        {
            if (node is not XamlAstXamlPropertyValueNode valueNode)
                return node;

            if (valueNode.Property is not XamlAstClrProperty property)
                return node;

            if (valueNode.Values.Count != 1)
                return node;

            var value = valueNode.Values[0];

            if (TypesContainer.CurrentClass == null)
                throw new InvalidOperationException("This should never happen!");
             
            var thisNode = new XamlAstThisNode(property, TypesContainer.CurrentClass);

            // check for event
            if (value is XamlAstTextNode text)
            {
                var invokedValue = text.Text?.Trim();

                if (string.IsNullOrEmpty(invokedValue))
                {
                    throw new XamlLoadException($"Property '{property.Name}' requires a value.", property);
                }

                // Find an event with the same name as the "property".
                var sourceEvent = property.DeclaringType
                    .GetAllEvents()
                    .FirstOrDefault(e => e.Name == property.Name);

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

                var eventHandler = FindEventHandler(TypesContainer.CurrentClass, invokedValue!, invoke);

                if (eventHandler == null)
                {
                    throw new XamlLoadException(
                        $"Unable to find event handler '{invokedValue}' on " +
                        $"'{TypesContainer.CurrentClass.GetFqn()}' compatible with event " +
                        $"'{sourceEvent.Name}'.",
                        valueNode);
                }

                var delegateNode = new XamlLoadMethodDelegateNode(valueNode, thisNode, delegateType, eventHandler);
                return new XamlPropertyAssignmentNode(valueNode,
                    new XamlAstClrProperty(text, property.Name, TypesContainer.CurrentClass, eventHandler),
                    [new XamlDirectCallPropertySetter(sourceEvent.Add)],
                    [delegateNode]);
            }

            // x:Binding
            if (value is not XamlAstObjectNode objectNode)
                return node;

            if (objectNode.Arguments.FirstOrDefault() is not XamlAstTextNode propertyNode)
                return node;

            var parameters = objectNode.Children.Cast<XamlAstXamlPropertyValueNode>()
                .ToDictionary(
                    p => ((XamlAstClrProperty)p.Property).Name, 
                    p => ((XamlAstTextNode)p.Values.First()).Text);

            // By default, binding goes one-way, unless specified otherwise
            // if the override is not correctly filled in, we throw.
            BindingMode mode = BindingMode.OneWay;
            if (parameters.TryGetValue("Mode", out var b) && !Enum.TryParse(b, out mode))
            {
                throw new XamlLoadException($"{b}' is not a valid value of type 'BindingMode'",
                    propertyNode);
            }

            // check if we're dealing with a code-behind property.
            var sourceProperty = TypesContainer.CurrentClass
                .GetAllProperties()
                .FirstOrDefault(e => e.Name == propertyNode.Text && e.Getter != null);

            if (sourceProperty == null)
            {
                throw new XamlLoadException(
                    $"Property '{propertyNode.Text}' was not found in code-behind class '{TypesContainer.CurrentClass.FullName}'",
                    propertyNode);
            }

            var targetProperty = property.DeclaringType
                                .GetAllProperties()
                                .FirstOrDefault(p => p.Name == property.Name);

            if (targetProperty == null)
            {
                throw new XamlLoadException(
                    $"Property '{property.Name}' does not exist in type '{property.DeclaringType.FullName}'",
                    objectNode);
            }

            if (targetProperty.Setter == null)
            {
                throw new XamlLoadException(
                   $"Property '{targetProperty.Name}' on '{targetProperty.DeclaringType.FullName}' is not writable.",
                   objectNode);
            }

            var clrUiProperty = new XamlAstClrProperty(property, targetProperty, context.Configuration);
            var clrCodeBehindProperty = new XamlAstClrProperty(property, sourceProperty, context.Configuration);
            var codeBehindValue = new XamlStaticOrTargetedReturnMethodCallNode(
                                        property,
                                        clrCodeBehindProperty.Getter!,
                                        [thisNode]);

            var initialAssignment = new XamlPropertyAssignmentNode(
                property,
                clrCodeBehindProperty,
                clrUiProperty.Setters,
                [codeBehindValue]);

            return initialAssignment;
        }

        private static IXamlMethod? FindEventHandler(
            IXamlType rootType,
            string name,
            IXamlMethod delegateInvoke)
        {
            foreach (var method in rootType.FindMethods(m => m.Name == name))
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
    }
}
