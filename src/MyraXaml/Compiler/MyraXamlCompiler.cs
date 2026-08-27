using Mono.Cecil;
using Mono.Cecil.Cil;
using Myra.Xaml.Helpers;
using Myra.Xaml.Transformers;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq; 
using XamlX;
using XamlX.Ast; 
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem; 

namespace Myra.Xaml.Compiler
{
      
    public sealed class MyraXamlCompiler : IDisposable
    {
        public MyraBindingCompilationContext BindingContext { get; }  

        public CecilTypeSystem TypeSystem { get; }

        public TransformerConfiguration Configuration { get; }

        public XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> EmitMappings { get; }

        private readonly XamlILCompiler _compiler;

        public MyraXamlCompiler(CecilTypeSystem typeSystem)
        { 
            BindingContext = new(typeSystem);
            TypeSystem = typeSystem;
            Configuration = CreateConfiguration(TypeSystem);
            EmitMappings = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>();

            _compiler = new XamlILCompiler(Configuration, EmitMappings, true);
            _compiler.Transformers.Insert(7, new CodeBehindReferenceTransformer(BindingContext));
            _compiler.Transformers.Insert(7, new XamlViewModelAssignmentTransformer());
            _compiler.Transformers.Insert(7, new XamlXDirectivesTransformer(BindingContext));

            TypesContainer.INotifyPropertyChanged = TypeSystem.FindType(typeof(INotifyPropertyChanged).FullName)!;
            TypesContainer.Byte = TypeSystem.FindType(typeof(byte).FullName)!;
            TypesContainer.Int16 = TypeSystem.FindType(typeof(short).FullName)!;
            TypesContainer.UInt16 = TypeSystem.FindType(typeof(ushort).FullName)!;
            TypesContainer.UInt32 = TypeSystem.FindType(typeof(uint).FullName)!;
            TypesContainer.Int64 = TypeSystem.FindType(typeof(long).FullName)!;
            TypesContainer.UInt64 = TypeSystem.FindType(typeof(ulong).FullName)!;
            TypesContainer.PropertyChangedEventArgs = TypeSystem.FindType(typeof(PropertyChangedEventArgs).FullName)!;
            TypesContainer.PropertyChangedEventHandler = TypeSystem.FindType(typeof(PropertyChangedEventHandler).FullName)!;
            TypesContainer.Color = TypeSystem.FindType("Microsoft.Xna.Framework.Color")!;
            TypesContainer.Thickness = TypeSystem.FindType("Myra.Graphics2D.Thickness")!;
            TypesContainer.Proportion = TypeSystem.FindType("Myra.Graphics2D.UI.Proportion")!;
            TypesContainer.Widget = TypeSystem.FindType("Myra.Graphics2D.UI.Widget")!;
        }

        private const string buildMethodName = "InitializeComponent";

        public void CompileInto(
            XamlDocument document,  
            TypeDefinition currentClassDefinition,
            string fileName,
            string fileContents)
        {
            var typeBuilder = TypeSystem.CreateTypeBuilder(currentClassDefinition, false);
            BindingContext.Setup(typeBuilder);

            // transform the document into an AST tree for compilation
            _compiler.Transform(document);

            // compile the AST into IL. The IL will be written to the DLL once all files have been compiled.
            var populate = _compiler.DefinePopulateMethod(typeBuilder, document, buildMethodName, XamlVisibility.Private);

            _compiler.Compile(
                document, 
                _compiler.CreateContextType(typeBuilder),
                populate,
                typeBuilder,
                null,
                null,
                null,
                Path.GetDirectoryName(fileName),
                new XamlFileSource(fileName, fileContents));

            typeBuilder.CreateType();
            EnsureBuildMethodCalled(currentClassDefinition);
        }

        public const string MyraMappings = "https://github.com/MyraUI/Myra";


        public static TransformerConfiguration CreateConfiguration(CecilTypeSystem typeSystem)
        {
            var typeMappings = new XamlLanguageTypeMappings(typeSystem);

            var contentProperty = typeSystem.FindType("Myra.Attributes.ContentAttribute")
                ?? throw new InvalidOperationException("Cannot find ContentAttribute!");
            typeMappings.ContentAttributes.Add(contentProperty);

            var mappings = new XamlXmlnsMappings();

            var myraAssembly = typeSystem.FindAssembly("Myra")
                                ?? throw new InvalidOperationException("Could not find Myra assembly.");

            // get default components, brushes and styles
            mappings.Namespaces.Add(
                MyraMappings,
                new List<(IXamlAssembly asm, string ns)>
                {
                    (myraAssembly, "Myra.Graphics2D.UI"),
                    (myraAssembly, "Myra.Graphics2D.Brushes"),
                    (myraAssembly, "Myra.Graphics2D.UI.Styles")
                });

            // get x:(action) types
            mappings.Namespaces.Add(
                  XamlNamespaces.Xaml2006,
                  new List<(IXamlAssembly, string ns)>
                  {
                      (myraAssembly, "Myra.Markup")
                  });

            return new TransformerConfiguration(
                typeSystem,
                myraAssembly,
                typeMappings,
                xmlnsMappings: mappings,
                customValueConverter: MyraValueConverters,
                identifierGenerator: null,
                diagnosticsHandler: null);
        }

        private static bool MyraValueConverters(AstTransformationContext context, IXamlAstValueNode node, IReadOnlyList<IXamlCustomAttribute>? customAttributes, IXamlType type, out IXamlAstValueNode result)
        {
            result = null!;

            // handle proportions
            if (type == TypesContainer.Proportion)
            {
                if (!GetText(node, out var text))
                    return false;

                // check if value is one of the static readonly properties.
                var field = type.GetAllFields().FirstOrDefault(t => t.Name == text);
                if (field != null && field.IsStatic)
                {
                    result = new XamlStaticFieldNode(node, field);
                    return true;
                }

                return false;
            }
              
            if (type == TypesContainer.Thickness)
            {
                if (!GetText(node, out var text))
                    return false;

                // check if value is one of the static readonly properties.
                var field = type.GetAllFields().FirstOrDefault(t => t.Name == text);
                if (field != null && field.IsStatic)
                {
                    result = new XamlStaticFieldNode(node, field);
                    return true;
                }

                // conversion time!
                var values = text.Split(',');
                var arguments = new List<int>();
                foreach(var value in values)
                {
                    if (!Int32.TryParse(value.Trim(), out int r))
                    {
                        return false;
                    }
                    arguments.Add(r);
                }
                var constructor = TypesContainer.Thickness.FindConstructor(arguments.Select(a => context.Configuration.WellKnownTypes.Int32).ToList());
                if (constructor == null)
                {
                    context.ReportDiagnostic(new XamlDiagnostic("MYRA003", XamlDiagnosticSeverity.Fatal, $"No constructor for Thickness has {arguments.Count} parameters!", node));
                    return false;
                }

                result = new XamlAstNewClrObjectNode(node,
                    new XamlAstClrTypeReference(node, TypesContainer.Thickness, false),
                    constructor,
                    [.. arguments.Select(a => (IXamlAstValueNode)new XamlConstantNode(node, context.Configuration.WellKnownTypes.Int32, a))]);
                return true;
            }

            if (type == TypesContainer.Color)
            {
                if (!GetText(node, out var text))
                    return false;

                // check if value is one of the static readonly properties.
                var property = type.GetAllProperties().FirstOrDefault(t => t.Name == text);
                if (property != null && property.Getter!.IsStatic)
                {
                    result = new XamlStaticOrTargetedReturnMethodCallNode(node, property.Getter, null);
                    return true;
                }

                // parse value
                if (ParseHex(text, out var hex))
                {
                    if (hex.Length != 6 && hex.Length != 8)
                    {
                        context.ReportDiagnostic(new XamlDiagnostic("MYRA004", XamlDiagnosticSeverity.Fatal, "Hex number must have 6 or 8 hexadecimal characters!", node));
                        return false;
                    }

                    if (!uint.TryParse(hex,
                                       NumberStyles.HexNumber,
                                       CultureInfo.CurrentCulture,
                                       out var color))
                    {
                        context.ReportDiagnostic(new XamlDiagnostic("MYRA005", XamlDiagnosticSeverity.Fatal, "Hex number is not valid!", node));
                        return false;
                    }

                    result = new XamlAstNewClrObjectNode(node,
                         new XamlAstClrTypeReference(node, TypesContainer.Color, false),
                         TypesContainer.Color.FindConstructor([TypesContainer.UInt32])!,
                         [(new XamlConstantNode(node, TypesContainer.UInt32, color))]);
                    return true;
                }
            }

            return false;
        }

        private static bool ParseHex(string input, [NotNullWhen(true)] out string? hex)
        {
            if (input[0] == '#')
            {
                hex = input.Substring(1);
                return true;
            }

            if (input[0] == '0' && input[1] == 'x'
                || input[0] == '&' & input[1] == 'H')
            {
                hex = input.Substring(2);
                return true;
            }
            hex = null;
            return false;
        }

        private static bool GetText(IXamlAstValueNode node, [NotNullWhen(true)] out string? text)
        {
            text = null;
            if (node is not XamlAstTextNode textNode)
                return false;

            if (string.IsNullOrWhiteSpace(textNode.Text))
                return false;

            text = textNode.Text;
            return true;
        }


        /// <summary>
        /// Ensure that the constructor of the code-behind type has "InitializeComponent()" called
        /// at the tail end of the constructor. <br/>
        /// If no constructor yet exists, one willm be created.
        /// </summary> 
        private void EnsureBuildMethodCalled(TypeDefinition type)
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
            var buildMethod = type.Methods.First(m => m.Name == buildMethodName);

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

                var il = constructor.Body.GetILProcessor();

                // base()
                var baseConstructor = type.BaseType?
                    .Resolve()?
                    .Methods
                    .FirstOrDefault(m =>
                        m.IsConstructor &&
                        !m.IsStatic &&
                        m.Parameters.Count == 0);

                if (baseConstructor == null)
                    throw new InvalidOperationException(
                        $"Unable to find parameterless base constructor for '{type.FullName}'.");
                 
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, module.ImportReference(baseConstructor));

                // this.InitializeComponent(IServiceProvider, this);
                il.Emit(OpCodes.Ldnull);  
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, module.ImportReference(buildMethod));

                il.Emit(OpCodes.Ret);

                return;
            }

            // Existing constructor:
            // insert this.InitializeComponent() immediately before ret.
            var processor = constructor.Body.GetILProcessor();

            var ret = constructor.Body.Instructions
                .FirstOrDefault(i => i.OpCode == OpCodes.Ret);

            if (ret == null)
                throw new InvalidOperationException(
                    $"Constructor '{constructor.FullName}' has no ret instruction.");

            processor.InsertBefore(ret, processor.Create(OpCodes.Ldnull));
            processor.InsertBefore(ret, processor.Create(OpCodes.Ldarg_0));

            processor.InsertBefore(
                ret,
                processor.Create(
                    OpCodes.Call,
                    module.ImportReference(buildMethod)));
        }
         

        public void Dispose()
        {
            TypeSystem.Dispose();
        } 
    }
}
