using Microsoft.Build.Framework;
using Mono.Cecil;
using Myra.Attributes;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
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

        public MyraXamlCompiler(string? targetPath, ITaskItem[] referenceAssemblies)
        {
            var assemblies = referenceAssemblies
                .Select(x => x.ItemSpec)
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(); 

            TypeSystem = new CecilTypeSystem(assemblies, targetPath);
 
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
                null,
                namespaceInfoClassName,
                baseUri,
                fileSource); 
        }

        public void CompileInto(
            XamlDocument document, 
            TypeDefinition targetType,
            string fileName,
            string fileContents)
        {
            var typeBuilder = TypeSystem.CreateTypeBuilder(targetType, false);

            var contextTypeBuilder =
                typeBuilder.DefineSubType(
                    Configuration.WellKnownTypes.Object,
                    "Context",
                    XamlVisibility.Private);

            var contextType =
                _compiler.CreateContextType(contextTypeBuilder);

            var populate =
                _compiler.DefinePopulateMethod(
                    typeBuilder,
                    document,
                    "InitializeComponent",
                    XamlVisibility.Public);

            _compiler.Compile(
                document,
                contextType,
                populate,
                typeBuilder,
                buildMethod: null,
                buildDeclaringType: null,
                namespaceInfoBuilder: null,
                baseUri: Path.GetDirectoryName(fileName),
                fileSource: new MyraFileSource(fileName, Encoding.UTF8.GetBytes(fileContents)));

            typeBuilder.CreateType();
        }


        public static TransformerConfiguration CreateConfiguration(CecilTypeSystem typeSystem)
        {
            var typeMappings = new XamlLanguageTypeMappings(typeSystem);

            var contentProperty = typeSystem.FindType(typeof(ContentAttribute).FullName!)
                ?? throw new InvalidOperationException("Cannot find ContentAttribute!");
            typeMappings.ContentAttributes.Add(contentProperty);

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
                diagnosticsHandler: null)
            {
                IncludeServiceProvider = false
            };
        }

        private string GetModule<T>()
        {
            return typeof(T).Assembly.GetModules()[0].FullyQualifiedName;
        }
    }
}
