using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Transformers
{
    /// <summary>
    /// Converts event attributes such as <c>&lt;Button Click="OnClick" /&gt;</c>
    /// into an XamlX delegate value targeting the event's add method. <br/>
    /// The resulting AST is emitted by the normal XamlILCompiler.
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

            if (value is not XamlAstTextNode text)
                return node;

            var invokedValue = text.Text?.Trim();
            if (string.IsNullOrEmpty(invokedValue))
            {
                throw new XamlLoadException(
                    $"Property '{property.Name}' requires a value.",
                    property);
            }
            // check if we're dealing with a hard-coded value.
            // If so, we can return and let other transformers handle that.
            if (IsHardCoded(context.Configuration.WellKnownTypes, property, invokedValue!))
                return node;

            if (MyraXamlCompileTask.CurrentClass == null)
            {
                throw new XamlLoadException("This should never happen", property);
            }

            var rootType = context.Configuration.TypeSystem.FindType(MyraXamlCompileTask.CurrentClass.FullName);
            if (rootType == null)
            {
                throw new XamlLoadException("Cannot find code-behind in build!", property);
            }

            var thisNode = new XamlAstThisNode(property, rootType);

            // check if we're dealing with a code-behind property.
            var codeBehindProperty = rootType
                .GetAllProperties()
                .FirstOrDefault(e => e.Name == invokedValue && e.Getter != null);

            if (codeBehindProperty != null)
            {
                var uiProperty = property.DeclaringType.GetAllProperties()
                                    .FirstOrDefault(p => p.Name == property.Name);

                if (uiProperty == null)
                    return node;

                var clrUiProperty = new XamlAstClrProperty(property, uiProperty, context.Configuration);
                var clrCodeBehindProperty = new XamlAstClrProperty(property, codeBehindProperty, context.Configuration);
                var codeBehindValue = new XamlStaticOrTargetedReturnMethodCallNode(
                                           property,
                                           clrCodeBehindProperty.Getter!,
                                           [thisNode]);

                return new XamlPropertyAssignmentNode(
                    property,
                    clrCodeBehindProperty,
                    clrUiProperty.Setters,
                    [codeBehindValue]);
            }


            // Find an event with the same name as the "property".
            var codeBehindEvent = property.DeclaringType
                .GetAllEvents()
                .FirstOrDefault(e => e.Name == property.Name);

            if (codeBehindEvent == null || codeBehindEvent.Add == null)
                return node;

            var delegateType = context.Configuration.TypeSystem.FindType("Myra.Events.MyraEventHandler")
                ?? throw new XamlLoadException(
                    $"Unable to determine the delegate type for event '{codeBehindEvent.Name}'.",
                    valueNode);

            var invoke = delegateType
                .FindMethod(m => m.Name == "Invoke");

            if (invoke == null)
            {
                throw new XamlLoadException(
                    $"Unable to find Invoke method on event delegate '{delegateType.GetFqn()}'.",
                    valueNode);
            }

            var handler = FindEventHandler(rootType, invokedValue!, invoke);

            if (handler == null)
            {
                throw new XamlLoadException(
                    $"Unable to find event handler '{invokedValue}' on " +
                    $"'{rootType.GetFqn()}' compatible with event " +
                    $"'{codeBehindEvent.Name}'.",
                    valueNode);
            }

            var delegateNode = new XamlLoadMethodDelegateNode(valueNode, thisNode, delegateType, handler);
            return new XamlPropertyAssignmentNode(valueNode,
                new XamlAstClrProperty(text, property.Name, rootType, handler), 
                [new XamlDirectCallPropertySetter(codeBehindEvent.Add)],
                [delegateNode]);
        }

        private bool IsHardCoded(XamlTypeWellKnownTypes wellKnownTypes, XamlAstClrProperty property, string text)
        {

            if (property.Getter == null)
                return false;

            // if the text starts with characters that make it ineligible for property/method names, return.
            var firstChar = text[0];
            if (Char.IsDigit(firstChar) || (Char.IsSymbol(firstChar) && firstChar != '_'))
                return false;

            var propertyType = property.Getter.ReturnType;

            if (!propertyType.IsValueType || propertyType.IsEnum)
                return false;

            if (propertyType.IsNullable())
                propertyType = propertyType.GenericArguments.FirstOrDefault() 
                    ?? throw new XamlLoadException("Nullable type must have underlying main type!", property);
             
            if (propertyType == wellKnownTypes.Boolean)
            {
                return Boolean.TryParse(text, out _);
            }

            if (propertyType == wellKnownTypes.Int32)
            {
                return Int64.TryParse(text, out _);
            }

            if (propertyType == wellKnownTypes.Double)
            {
                return Double.TryParse(text, out _);
            }
             
            return false;
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

    sealed class XamlAstThisNode :
        XamlAstNode,
        IXamlAstValueNode,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public XamlAstThisNode(
            IXamlLineInfo lineInfo,
            IXamlType type)
            : base(lineInfo)
        {
            Type = new XamlAstClrTypeReference(this, type, false);
        }

        public IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter codeGen)
        {
            codeGen.Ldarg_0();
            return XamlILNodeEmitResult.Type(0, Type.GetClrType());
        }
    }
}
