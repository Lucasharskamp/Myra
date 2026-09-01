using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Myra.Xaml.Compiler;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;
using static XamlX.Transform.TransformerConfiguration;

namespace Myra.Xaml.Helpers
{
    internal static class TransformerHelpers
    {
        public const string MyraMappings = "https://github.com/MyraUI/Myra";

        public static void EnsureAssignability(IXamlLineInfo lineInfo, XamlAstClrProperty targetProperty, string sourceFieldName, IXamlType sourceType)
        {
            if (!targetProperty.Getter!.ReturnType.IsAssignableFrom(sourceType))
            {
                throw new XamlLoadException(
                    $"Property '{targetProperty.Name}' from '{targetProperty.DeclaringType.FullName}' is not " +
                    $"assignable to '{sourceFieldName}' from '{targetProperty.DeclaringType.FullName}'",
                    lineInfo);
            }
        }


        public static IXamlAstValueNode GetStylesheet(AstTransformationContext context, IXamlAstValueNode node)
        {

            if (!context.TryGetItem<XamlStylesheetContainer>(out var stylesheetContainer))
            {
                stylesheetContainer = new XamlStylesheetContainer(node, context.Configuration.WellKnownTypes, MyraBindingCompilationContext.GetStylesheet, "default_ui_skin");
                context.SetItem(stylesheetContainer);
            }

            return stylesheetContainer.Node;
        }

        public static IXamlAstValueNode GetStyleName(AstTransformationContext context, XamlAstObjectNode node)
        { 

            if (!context.TryGetItem<XamlStyleContainer>(out var styleContainer))
            {
                styleContainer = new XamlStyleContainer(new XamlConstantNode(node, context.Configuration.WellKnownTypes.String, ""));
                context.SetItem(styleContainer);
            }

            return styleContainer.Node;
        }

        /// <summary>
        /// Retrieves the code-behind's CLR type the transformers are currently working on.
        /// </summary> 
        public static IXamlType CodeBehindClrType(this AstTransformationContext context)
        {
            return context.RootObject.Type.GetClrType();
        }

        public static bool FindXDirectiveAsAny(this XamlAstObjectNode valueNode, string xDirectiveName,
             [NotNullWhen(true)] out XamlAstXmlDirective? directive,
             [NotNullWhen(true)] out IXamlAstValueNode? foundValue)
        {
            var allDirectives = valueNode.Children
                .OfType<XamlAstXmlDirective>()
                .Where(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == xDirectiveName)
                .ToArray();

            if (allDirectives.Length == 0)
            {
                directive = null;
                foundValue = null;
                return false;
            }

            if (allDirectives.Length > 1)
                throw new XamlLoadException($"x:{xDirectiveName} can only exists once on a type!", valueNode);

            directive = allDirectives[0];
            if (directive.Values.Count == 0)
            {
                throw new XamlLoadException(
                    $"x:{xDirectiveName} must have a single value at least.", directive);
            }
            foundValue = directive.Values[0];
            return true;
        }

        /// <summary>
        /// Extracts a x: directive from a node in text form.
        /// </summary>
        /// <param name="valueNode">Node to extract from</param>
        /// <param name="xDirectiveName">Directive to find</param>
        /// <param name="foundValue">Found value (if present, otherwise null)</param>
        /// <returns>If the directive was present</returns>
        /// <exception cref="XamlLoadException">THrown if multiple nodes are found or if a node has multiple values.</exception>
        public static bool ExtractXDirectiveAsText(this XamlAstObjectNode valueNode, string xDirectiveName,
            [NotNullWhen(true)] out XamlAstXmlDirective? directive,
            [NotNullWhen(true)] out string? foundValue)
        {
            if (!FindXDirectiveAsAny(valueNode, xDirectiveName, out directive, out var foundNode))
            {
                foundValue = null;
                return false;
            }

            // There should be at least 1 value, namely the main value
            // other optional values can be retrieved by this method's caller.
            if (foundNode is not XamlAstTextNode text)
            {
                throw new XamlLoadException(
                    $"x:{xDirectiveName} must have a single string value.", directive);
            }

            // Remove x:directive, as we no longer need it.
            valueNode.Children.Remove(directive);

            foundValue = text.Text; 
            return true;
        }

        /// <summary>
        /// Finds a x: directive with a x:Static value from a node.
        /// </summary>
        /// <param name="valueNode">Node to extract from</param>
        /// <param name="xDirectiveName">Directive to find</param>
        /// <param name="foundValue">Found value (if present, otherwise null)</param>
        /// <returns>If the directive was present</returns>
        /// <exception cref="XamlLoadException">THrown if multiple nodes are found or if a node has multiple values.</exception>
        public static bool FindXDirectiveAsStatic(this XamlAstObjectNode valueNode, string xDirectiveName,
            [NotNullWhen(true)] out XamlAstXmlDirective? directive,
            [NotNullWhen(true)] out XamlStaticExtensionNode? foundValue)
        {
            if (!FindXDirectiveAsAny(valueNode, xDirectiveName, out directive, out var foundNode))
            {
                foundValue = null;
                return false;
            }

            // There should be at least 1 value, namely the main value
            // other optional values can be retrieved by this method's caller.
            if (foundNode is not XamlStaticExtensionNode xStatic)
            {
                throw new XamlLoadException(
                    $"x:{xDirectiveName} must have a single x:Static value.", directive);
            }
              
            foundValue = xStatic; 
            return true;
        }

        public static TransformerConfiguration CreateConfiguration(IXamlTypeSystem typeSystem)
        {
            var typeMappings = new XamlLanguageTypeMappings(typeSystem);

            var contentProperty = typeSystem.FindType("Myra.Attributes.ContentAttribute")
                ?? throw new InvalidOperationException("Cannot find ContentAttribute!");
            typeMappings.ContentAttributes.Add(contentProperty);

            var mappings = new XamlXmlnsMappings();

            var xna = typeSystem.FindAssembly("MonoGame.Framework")
                            ?? throw new InvalidOperationException("Could not find MonoGame.Framework assembly.");
            var myraAssembly = typeSystem.FindAssembly("Myra")
                                ?? throw new InvalidOperationException("Could not find Myra assembly.");

            // get default components, brushes and styles
            mappings.Namespaces.Add(
                MyraMappings,
                [
                    (myraAssembly, "Myra.Graphics2D.Brushes"),
                    (myraAssembly, "Myra.Graphics2D.UI"),
                    (myraAssembly, "Myra.Graphics2D.UI.Data"),
                    (myraAssembly, "Myra.Graphics2D.UI.Properties"),
                    (myraAssembly, "Myra.Graphics2D.UI.Styles"),
                    (myraAssembly, "Myra.Graphics2D.TextureAtlases"),
                    (xna, "Microsoft.Xna.Framework")
                ]);

            // get x:(action) types
            mappings.Namespaces.Add(
                  XamlNamespaces.Xaml2006,
                  [
                      (myraAssembly, "Myra.Markup")
                  ]);

            return new TransformerConfiguration(
                typeSystem,
                myraAssembly,
                typeMappings,
                xmlnsMappings: mappings,
                customValueConverter: ConverterHelper.MyraValueConverters,
                identifierGenerator: null,
                diagnosticsHandler: null);
        }


        public const string BuildMethodName = "InitializeComponent";

        /// <summary>
        /// Ensure that the constructor of the code-behind type has "InitializeComponent()" called
        /// at the tail end of the constructor. <br/>
        /// If no constructor yet exists, one willm be created.
        /// </summary> 
        public static void EnsureBuildMethodCalled(TypeDefinition type)
        {
            var module = type.Module;

            var constructors = type.Methods.Where(m =>
                m.IsConstructor &&
                !m.IsStatic).ToArray();

            if (constructors.Length > 1)
            {
                throw new InvalidOperationException($"Code-behind type '{type.FullName}' can only at most have 1 constructor!");
            }

            var constructor = constructors.FirstOrDefault();
            var buildMethod = type.Methods.First(m => m.Name == BuildMethodName);

            var baseConstructorDef = type.BaseType.Resolve().Methods
                .FirstOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 2);
            var baseConstructor = baseConstructorDef == null ? null : type.BaseType.Module.ImportReference(baseConstructorDef);

            if (constructor == null)
            {
                constructor = new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName,
                    module.TypeSystem.Void);

                type.Methods.Add(constructor);
            }
            else
            {
                constructor.Body = new MethodBody(constructor);
            } 

            var il = constructor.Body.GetILProcessor();

            // base(Stylesheet, string) 
            if (baseConstructor != null)
            {
                il.Emit(OpCodes.Ldarg_0);
                // todo replace with actual values
                il.Emit(OpCodes.Ldstr, "default_ui_skin");
                il.Emit(OpCodes.Call, MyraBindingCompilationContext.GetStylesheetDefinition);
                il.Emit(OpCodes.Ldstr, "");
                il.Emit(OpCodes.Call, baseConstructor);
            } 

            // this.InitializeComponent(IServiceProvider, this);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, module.ImportReference(buildMethod));

            il.Emit(OpCodes.Ret);

            return; 
    } 
    }
}
