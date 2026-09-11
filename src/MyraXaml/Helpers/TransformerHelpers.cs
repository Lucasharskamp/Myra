using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Myra.Xaml.Compiler;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

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

            var contentAttribute = typeSystem.FindType("Myra.Attributes.ContentAttribute")
                ?? throw new InvalidOperationException("Cannot find ContentAttribute!");
            typeMappings.ContentAttributes.Add(contentAttribute); 

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
                diagnosticsHandler: new MyraDiagnosticHandler(XamlDiagnosticSeverity.Error));
        }


        public static IXamlType? GetGenericTypeArgument(this IXamlType type)
        {
            if(type.GenericTypeDefinition != null)
            {
                return type.GenericArguments.Last();
            }
            
            if (type.BaseType?.GenericTypeDefinition != null)
            {
                return type.BaseType.GenericArguments.Last();
            }

            return null;
        }

        public static IXamlProperty GetTypeProperty(this IXamlType type, IXamlLineInfo node, string name)
        {
            var result = type.GetAllProperties().FirstOrDefault(p => p.Name == name);
            if (result == null)
            {
                throw new XamlLoadException($"Property '{name}' does not exist on type '{type.FullName}'", node);
            }

            return result;
        }

        public static XamlAstXamlPropertyValueNode? GetProperty(this XamlAstObjectNode objectNode, string name)
        {
            return objectNode.Children.OfType<XamlAstXamlPropertyValueNode>().FirstOrDefault(c => c is XamlAstXamlPropertyValueNode propNode
                                && propNode.Property is XamlAstNamePropertyReference propRef
                                && propRef.Name == name);
        }

        public static XamlPropertyAssignmentNode? GetPropertyAssignment(this XamlAstObjectNode objectNode, string name)
        {
            return objectNode.Children.OfType<XamlPropertyAssignmentNode>().FirstOrDefault(c => c is XamlPropertyAssignmentNode propNode
                                && propNode.Property is XamlAstClrProperty propRef
                                && propRef.Name == name);
        }

        public static string GetTypeName(this IXamlAstTypeReference reference)
        {
            if (reference is XamlAstXmlTypeReference xmlReference)
            {
                return xmlReference.Name;
            }
            
            if (reference is XamlAstClrTypeReference typeReference)
            {
                return typeReference.Type.Name;
            }

            throw new InvalidOperationException("Unknown reference type");
        }

        public static string GetTypeName(this IXamlAstPropertyReference propertyReference)
        {
            if (propertyReference is XamlAstNamePropertyReference namePropertyReference)
            {
                return namePropertyReference.TargetType.GetTypeName();
            }

            if (propertyReference is XamlAstClrProperty clrProperty)
            {
                return clrProperty.DeclaringType.Name;
            }

            throw new InvalidOperationException("Unknown reference type");
        }

        public static XamlConstantNode ToConstantNode(this XamlAstTextNode text, AstTransformationContext context) 
            => new XamlConstantNode(text, context.Configuration.WellKnownTypes.String, text.Text);

        public static XamlConstantNode ToConstantNode(this XamlAstTextNode text, AstTransformationContext context, string overrideText)
            => new XamlConstantNode(text, context.Configuration.WellKnownTypes.String, overrideText);

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

            // this.InitializeComponent(IServiceProvider, this, Stylesheet);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldarg_0);
            // todo replace with actual values
            il.Emit(OpCodes.Ldstr, "default_ui_skin");
            il.Emit(OpCodes.Call, MyraBindingCompilationContext.GetStylesheetDefinition);
            il.Emit(OpCodes.Call, module.ImportReference(buildMethod));

            il.Emit(OpCodes.Ret);

            return; 
        }

        internal static string CurrentRelativePath = ""; 
        internal static void SetCurrentRelativePath(string targetPath, ITaskItem currentFile)
        { 
            var targetDir = Path.GetDirectoryName(targetPath);
            CurrentRelativePath = PathNetCore.GetRelativePath(targetDir, GetPath(targetDir, currentFile.ItemSpec));
        }

        private static string GetPath(string targetDir, string filePath)
        {
            // we cannot use the relative path of an asset compared to the asset it is referring to;
            // files are loaded from the perspective of the executing assembly, not the assets its using!
            if (!filePath.Contains("..\\", StringComparison.OrdinalIgnoreCase))
            {
                var attempt = Path.Combine(targetDir, filePath);
                if (File.Exists(attempt))
                {
                    return Path.GetDirectoryName(attempt);
                }
            }

            if (filePath.Contains("..\\", StringComparison.OrdinalIgnoreCase))
            {
                filePath = filePath.Replace("..\\", "");
            }

            // relative not found; try finding the file in the output directory.
            var combinedPath = Path.Combine(targetDir, Path.GetDirectoryName(filePath));

            while (!Directory.Exists(combinedPath))
            {
                // keep chopping off a part of the path of FilePath, until there is no more "relative directory" to be found.
                var nextSlash = filePath.IndexOf('\\');
                if (nextSlash == -1)
                    return targetDir; // work from directory root if relative folder not found.

                filePath = filePath.Substring(nextSlash+1);
                combinedPath = Path.Combine(targetDir, Path.GetDirectoryName(filePath));
            }

            return combinedPath;
        }

        internal static string GetRelativePathOfResource(string localPath)
        {  
            return Path.Combine(CurrentRelativePath, localPath);
        }
    }
}
