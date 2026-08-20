using Mono.Cecil;
using Mono.Cecil.Cil;
using Myra.Xaml.Transformers;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Text;
using XamlX;
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

        public MyraXamlCompiler(CecilTypeSystem typeSystem)
        {
            TypeSystem = typeSystem;
 
            Configuration = CreateConfiguration(TypeSystem);

            EmitMappings = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>();

            _compiler = new XamlILCompiler(Configuration, EmitMappings, true);
            _compiler.Transformers.Insert(8, new CodeBehindReferenceTransformer());
        }


        public void Transform(XamlDocument document)
        {
            _compiler.Transform(document); 
        }

        private const string buildMethodName = "InitializeComponent";

        public void CompileInto(
            XamlDocument document, 
            TypeDefinition targetType,
            string fileName,
            string fileContents)
        {

            var typeBuilder = TypeSystem.CreateTypeBuilder(targetType, false);  
            var populate = _compiler.DefinePopulateMethod(typeBuilder, document, buildMethodName, XamlVisibility.Public, false, false);

            _compiler.Compile(
                document,
                null,
                populate,
                typeBuilder,
                buildMethod: null,
                buildDeclaringType: null,
                namespaceInfoBuilder: null,
                baseUri: Path.GetDirectoryName(fileName),
                fileSource: new MyraFileSource(fileName, Encoding.UTF8.GetBytes(fileContents)));

            typeBuilder.CreateType();
            EnsureBuildMethodCalled(targetType);
        }


        public static TransformerConfiguration CreateConfiguration(CecilTypeSystem typeSystem)
        {
            var typeMappings = new XamlLanguageTypeMappings(typeSystem);

            var contentProperty = typeSystem.FindType("Myra.Attributes.ContentAttribute")
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

            mappings.Namespaces.Add(
                  XamlNamespaces.Xaml2006,
                  new List<(IXamlAssembly, string ns)>
                  {
                      (myraAssembly, "Myra.Markup")
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


        private static void EnsureBuildMethodCalled(TypeDefinition type)
        {
            var module = type.Module;

            var constructor = type.Methods.FirstOrDefault(m =>
                m.IsConstructor &&
                !m.IsStatic &&
                m.Parameters.Count == 0);

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

                il.Append(il.Create(OpCodes.Ldarg_0));
                il.Append(il.Create(OpCodes.Call, module.ImportReference(baseConstructor)));

                // this.InitializeComponent();
                il.Append(il.Create(OpCodes.Ldarg_0));
                il.Append(il.Create(OpCodes.Call, module.ImportReference(buildMethod)));

                il.Append(il.Create(OpCodes.Ret));

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

            processor.InsertBefore(
                ret,
                processor.Create(OpCodes.Ldarg_0));

            processor.InsertBefore(
                ret,
                processor.Create(
                    OpCodes.Call,
                    module.ImportReference(buildMethod)));
        }
    }
}
