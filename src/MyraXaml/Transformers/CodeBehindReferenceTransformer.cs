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

            if (valueNode.Property is not XamlAstClrProperty targetProperty)
                return node;

            if (valueNode.Values.Count != 1)
                return node;

            var value = valueNode.Values[0];
            var rootClrType = context.RootObject.Type.GetClrType();

            // check for event
            if (value is XamlAstTextNode text)
            {
                var invokedValue = text.Text?.Trim();

                if (string.IsNullOrEmpty(invokedValue))
                {
                    throw new XamlLoadException($"Property '{targetProperty.Name}' requires a value.", targetProperty);
                }

                // Find an event with the same name as the "property".
                var sourceEvent = targetProperty.DeclaringType
                    .GetAllEvents()
                    .FirstOrDefault(e => e.Name == targetProperty.Name);

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
                return new XamlPropertyAssignmentNode(valueNode, targetProperty,
                    [new XamlDirectCallPropertySetter(sourceEvent.Add)],
                    [delegateNode]);
            }

            // now we do {x:Binding} directives, which we can place on properties or fields.
            // (Note: Fields only support onetime assignments!)
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
            BindingMode bindingMode = BindingMode.OneWay;
            if (parameters.TryGetValue("Mode", out var b) && !Enum.TryParse(b, out bindingMode))
            {
                throw new XamlLoadException($"{b}' is not a valid value of type 'BindingMode'",
                    propertyNode);
            }

            // get the source property in the code-behind (or a field as fallback)
            var sourceProperty = rootClrType
                .GetAllProperties()
                .FirstOrDefault(e => e.Name == propertyNode.Text && e.Getter != null);

            if (sourceProperty == null)
            {
                var sourceField = rootClrType.GetAllFields()
                    .FirstOrDefault(e => e.Name == propertyNode.Text);

                if (sourceField == null)
                {
                    throw new XamlLoadException(
                        $"No property or field named '{propertyNode.Text}' was not found in code-behind class '{rootClrType.FullName}'",
                        propertyNode);
                }

                if (bindingMode != BindingMode.OneWay)
                {
                    throw new XamlLoadException("Field assignments can only be done on one-way binding mode!", propertyNode);
                }

                TransformerHelpers.EnsureAssignability(propertyNode, targetProperty, sourceField.Name, sourceField.FieldType);
                var sourceFieldReference = new XamlAstFieldReference(propertyNode, sourceField, propertyNode);
                return new XamlAstXamlPropertyValueNode(propertyNode, targetProperty, sourceFieldReference, false);
            }

            TransformerHelpers.EnsureAssignability(propertyNode, targetProperty, sourceProperty.Name, sourceProperty.PropertyType);
             
            var sourceClrProperty = new XamlAstClrProperty(targetProperty, sourceProperty, context.Configuration);
            var sourceValue = new XamlStaticOrTargetedReturnMethodCallNode(targetProperty, sourceClrProperty.Getter!, [context.RootObject]);

            var initialAssignment = new XamlPropertyAssignmentNode(
                valueNode,
                targetProperty,
                targetProperty.Setters,
                [sourceValue]);

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
