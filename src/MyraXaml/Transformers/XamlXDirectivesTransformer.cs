using Myra.Xaml.Compiler;
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
    /// This transformer resolves x:Name directives, binding the element to the code-behind property/field 
    /// the x:Name property is targeting.
    /// </summary>
    public sealed class XamlXDirectivesTransformer(MyraBindingCompilationContext bindings) : IXamlAstTransformer
    {
        private readonly MyraBindingCompilationContext _bindings = bindings;

        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {  
            if (node is not XamlAstObjectNode valueNode)
                return node;

            // get "x:Uid" directive from the element.  
            if (valueNode.ExtractXDirectiveAsText("Uid", out var uidDirective, out var uid))
            {
                _bindings.RegisterNodeIdentity(uid, valueNode);
            }

            // get "x:FieldModifier" directive from the element.  
            if (valueNode.ExtractXDirectiveAsText("FieldModifier", out var fieldDirective, out var fieldModifier))
            {
                if(!Enum.TryParse<XamlVisibility>(fieldModifier, out var xamlVisibility))
                {
                    throw new XamlLoadException($"FieldModifier was assigned incorrect value '{fieldModifier}'", valueNode);
                }
                _bindings.RegisterNodeFieldModifier($"{valueNode.Line}-{valueNode.Position}", xamlVisibility);
            }

            // get "x:Name" directive from the element.  
            if (!valueNode.ExtractXDirectiveAsText("Name", out var directive, out var text))
                return node;

            var rootClrType = context.CodeBehindClrType();
             
            // get the target property to aim at.
            var targetProperty = rootClrType
                                .GetAllProperties()
                                .FirstOrDefault(p => p.Name == text);

            if (targetProperty == null)
            {
                context.ReportDiagnostic(new XamlDiagnostic("MYRA001",
                    XamlDiagnosticSeverity.Error,
                    $"property '{text}' not found in type '{rootClrType.FullName}'",
                    directive));
                return node;
            }

            if (targetProperty.Setter == null)
            {
                context.ReportDiagnostic(new XamlDiagnostic("MYRA002",
                   XamlDiagnosticSeverity.Error,
                   $"property '{text}' in type '{rootClrType.FullName}' does not have a setter!",
                   directive));
                return node;
            } 

            var assignment = new XamlAssignPropertyValueNode(
                directive,
                targetProperty.Setter, 
                null);

            return new XamlValueWithManipulationNode(
                directive,
                valueNode,
                assignment);
        }
    }
}
