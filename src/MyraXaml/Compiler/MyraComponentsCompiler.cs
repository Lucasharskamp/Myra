using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Myra.Xaml.Helpers;
using Myra.Xaml.Transformers;
using Myra.Xaml.Types;
using System;
using System.IO;
using System.Linq;
using XamlX.Ast; 
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
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

        public void CompileXamlFile(TaskLoggingHelper log,
                                     string targetPath,
                                     string projectDirectory,
                                     AssemblyDefinition assembly,
                                     ITaskItem item)
        {
            var xamlPath = item.GetMetadata("FullPath");

            var xamlFileContents = File.ReadAllText(xamlPath);
            var document = XDocumentXamlParser.Parse(xamlFileContents);

            if (string.IsNullOrWhiteSpace(xamlPath))
                xamlPath = item.ItemSpec;

            if (!Path.IsPathRooted(xamlPath))
                xamlPath = Path.GetFullPath(xamlPath);

            log.LogMessage(MessageImportance.Normal, "Myra XAML: compiling '{0}'.", xamlPath);

            if (!File.Exists(xamlPath))
            {
                log.LogError("Myra XAML: XAML file '{0}' does not exist.", xamlPath);
                return;
            }

            var fileSource = new XmlFileSource(xamlPath, xamlFileContents);

            // we now handle it as a custom component or page.
            var className = ClassHelper.GetClassType((document.Root as XamlAstObjectNode)!, item, xamlPath, projectDirectory);
            if (string.IsNullOrWhiteSpace(className))
            {
                log.LogError("Myra XAML: could not determine the code-behind type for '{0}'. ", xamlPath);
                return;
            }

            var currentClassDefinition = assembly.MainModule.Types.FindTypeRecursive(className!);
            if (currentClassDefinition == null)
            {
                // Note: the code-behind type must also be compiled before this MSBuild is invoked!
                log.LogError("Myra XAML: code-behind type '{0}' was not found for XAML file '{1}'. ",
                    className,
                    targetPath);

                return;
            }

            var currentClass = TypeSystem!.FindType(currentClassDefinition.FullName);
            if (currentClass == null)
            {
                throw new InvalidOperationException("This should never happen");
            }

            // ensure code-behind class derives from Widget.
            if (!TypesContainer.Widget.IsAssignableFrom(currentClass))
            {
                log.LogError("Myra XAML: code-behind type '{0}' must derive from 'Myra.Graphics2D.UI.Widget'. ",
                  className,
                  targetPath);
            }

            // ensure the Myra assembly is included
            var assemblyMappings = Configuration.XmlnsMappings.Namespaces[TransformerHelpers.MyraMappings];
            if (assemblyMappings.Any(a => a.ns != currentClass.Namespace && a.asm != currentClass.Assembly))
            {
                assemblyMappings.Add((currentClass.Assembly!, currentClass.Namespace!));
            }

            log.LogMessage(MessageImportance.Low, "Myra XAML: code-behind type is '{0}'.", currentClass.FullName);

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
