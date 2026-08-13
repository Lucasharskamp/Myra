using Mono.Cecil;
using Myra.Attributes; 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using XamlX.Ast; 
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem; 

namespace Myra.Xaml.Compiler
{
      
    public sealed class MyraXamlCompiler
    {
        public CecilTypeSystem TypeSystem { get; }

        public TransformerConfiguration Configuration { get; }

        public XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> EmitMappings { get; }

        private readonly XamlILCompiler _compiler;

        public MyraXamlCompiler()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetModules()[0].FullyQualifiedName)
                .ToList();

            assemblies.AddRange([typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).Assembly.GetModules()[0].FullyQualifiedName,
                    typeof(ITypeDescriptorContext).Assembly.GetModules()[0].FullyQualifiedName,
                    typeof(TypeConverterAttribute).Assembly.GetModules()[0].FullyQualifiedName,
                    typeof(ContentAttribute).Assembly.GetModules()[0].FullyQualifiedName]);

            TypeSystem = new CecilTypeSystem(assemblies, null);

            Configuration = CreateConfiguration(TypeSystem);

            EmitMappings = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>();

            _compiler = new XamlILCompiler(Configuration, EmitMappings, true);
        }
         

        public void Transform(XamlDocument document)
        {
            _compiler.Transform(document); 
        }

        public void Compile(
            XamlDocument document, 
            TypeDefinition type,
            string namespaceInfoClassName = "__XamlNamespaceInfo",
            string? baseUri = null,
            IFileSource? fileSource = null)
        {
            // The XamlIL compiler expects the document to already have been
            // transformed into its imperative representation. 
            var typeBuilder = TypeSystem.CreateTypeBuilder(type, false);
            var contextType = _compiler.CreateContextType(typeBuilder);

            _compiler.Compile(
                document,
                typeBuilder,
                contextType,
                "InitializeComponent",
                "Build",
                namespaceInfoClassName,
                baseUri,
                fileSource); 
        }


        public AstTransformationContext CreateTransformationContext(XamlDocument document)
        {
            return _compiler.CreateTransformationContext(document);
        }

        public static TransformerConfiguration CreateConfiguration(CecilTypeSystem typeSystem)
        {
            var typeMappings = new XamlLanguageTypeMappings(typeSystem);

            var contentProperty = typeSystem.FindType(typeof(ContentAttribute).FullName)
                ?? throw new InvalidOperationException("Cannot find ContentAttribute!");
            typeMappings.ContentAttributes.Add(contentProperty);

            var typeConverter = typeSystem.FindType(typeof(TypeConverterAttribute).FullName)
                ?? throw new InvalidOperationException("Cannot find TypeConverterAttribute!");

            typeMappings.TypeConverterAttributes.Add(typeConverter);

            var mappings = new XamlXmlnsMappings();

            var myraAssembly = typeSystem.FindAssembly("Myra")
                                ?? throw new InvalidOperationException("Could not find Myra assembly.");

            mappings.Namespaces.Add(
                "https://github.com/MyraUI/Myra",
                new List<(IXamlAssembly asm, string ns)>
                {
                    (myraAssembly, "Myra.Graphics2D.UI"),
                    (myraAssembly, "Myra.Graphics2D.Brushes"),
                    (myraAssembly, "Myra.Graphics2D.UI.Styles")
                });

            return new TransformerConfiguration(
                typeSystem,
                defaultAssembly: myraAssembly,
                typeMappings,
                xmlnsMappings: mappings,
                customValueConverter: null,
                identifierGenerator: null,
                diagnosticsHandler: null);
        }
    }
}
