using Mono.Cecil;
using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System.IO;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Compiler
{
    /// <summary>
    /// Compiler for Stylesheets
    /// </summary>
    public sealed class MyraStylesheetsCompiler
    {
        public CecilTypeSystem TypeSystem { get; }
        public TransformerConfiguration Configuration { get; }

        private readonly XamlILCompiler _compiler;

        public MyraStylesheetsCompiler(CecilTypeSystem typeSystem)
        {
            TypeSystem = typeSystem;
            Configuration = TransformerHelpers.CreateConfiguration(TypeSystem);
            var EmitMappings = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>();

            _compiler = new XamlILCompiler(Configuration, EmitMappings, true);
        }
        private const string initializeMethod = "InitializeStylesheet";
        private const string buildMethod = "Create";

        public (string, IXamlMethod) Compile(XamlDocument document, XamlFileSource fileSource, TypeDefinition styleClassDefinition)
        { 
            var typeBuilder = TypeSystem.CreateTypeBuilder(styleClassDefinition, false);
            // transform the document into an AST tree for compilation
            _compiler.Transform(document);

            // compile the AST into IL. The IL will be written to the DLL once all files have been compiled.
            var populate = _compiler.DefinePopulateMethod(typeBuilder, document, initializeMethod, XamlVisibility.Private);
            var build = _compiler.DefineBuildMethod(typeBuilder, document, "_create", XamlVisibility.Private);

            _compiler.Compile(
                document,
                _compiler.CreateContextType(typeBuilder),
                populate,
                typeBuilder,
                build,
                typeBuilder,
                null,
                Path.GetDirectoryName(fileSource.FilePath),
                fileSource);

            var createWrapper = typeBuilder.DefineMethod(TypesContainer.StyleSheet, [], buildMethod, XamlVisibility.Assembly, true, false);

            // Create() => _create(null);
            var wrapperCodeGen = createWrapper.Generator;
            wrapperCodeGen.Ldnull();
            wrapperCodeGen.EmitCall(build);
            wrapperCodeGen.Ret();

            typeBuilder.CreateType();
            return (Path.GetFileNameWithoutExtension(fileSource.FilePath), createWrapper);
        }
    }
}
