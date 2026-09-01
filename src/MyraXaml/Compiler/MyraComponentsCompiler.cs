using Mono.Cecil;
using Mono.Cecil.Rocks;
using Myra.Xaml.Helpers;
using Myra.Xaml.Transformers;
using Myra.Xaml.Types; 
using System.IO;
using System.Linq;
using XamlX.Ast; 
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem; 

namespace Myra.Xaml.Compiler
{
      
    /// <summary>
    /// Compiler for XAML components with .cs code-behinds
    /// </summary>
    public sealed class MyraComponentsCompiler
    {
        public MyraBindingCompilationContext BindingContext { get; }  

        public CecilTypeSystem TypeSystem { get; }

        public TransformerConfiguration Configuration { get; }

        public XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> EmitMappings { get; }

        private readonly XamlILCompiler _compiler;

        public MyraComponentsCompiler(CecilTypeSystem typeSystem)
        { 
            BindingContext = new(typeSystem);
            TypeSystem = typeSystem;
            Configuration = TransformerHelpers.CreateConfiguration(TypeSystem);
            EmitMappings = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>();

            _compiler = new XamlILCompiler(Configuration, EmitMappings, true);
            _compiler.Transformers.Insert(7, new CodeBehindReferenceTransformer(BindingContext));
            _compiler.Transformers.Insert(7, new XamlRootDirectivesTransformer());
            _compiler.Transformers.Insert(7, new XamlXDirectivesTransformer(BindingContext));
            _compiler.Transformers.Insert(18, new ConstructableWidgetTransformer());
        }
          
        public void CompileInto(
            XamlDocument document,  
            TypeDefinition currentClassDefinition, 
            XamlFileSource fileSource)
        {
            var typeBuilder = TypeSystem.CreateTypeBuilder(currentClassDefinition, false);
            BindingContext.Setup(typeBuilder);

            // transform the document into an AST tree for compilation
            _compiler.Transform(document);

            // compile the AST into IL. The IL will be written to the DLL once all files have been compiled.
            var populate = _compiler.DefinePopulateMethod(typeBuilder, document, TransformerHelpers.BuildMethodName, XamlVisibility.Private);

            _compiler.Compile(
                document, 
                _compiler.CreateContextType(typeBuilder),
                populate,
                typeBuilder,
                null,
                null,
                null,
                Path.GetDirectoryName(fileSource.FilePath),
                fileSource);

            typeBuilder.CreateType();
            TransformerHelpers.EnsureBuildMethodCalled(currentClassDefinition);
        }  
    }
}
