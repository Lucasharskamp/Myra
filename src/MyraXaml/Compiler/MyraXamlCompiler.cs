using Myra.Attributes;
using Myra.Xaml.TypeSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using XamlX.Ast;
using XamlX.Compiler;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem; 

namespace Myra.Xaml.Compiler
{
      
    public sealed class MyraXamlCompiler
    {
        public MyraTypeSystem TypeSystem { get; }

        public TransformerConfiguration Configuration { get; }

        public XamlLanguageEmitMappings<MyraCecilILEmitter, XamlILNodeEmitResult> EmitMappings { get; }

        private readonly CompilerImpl _compiler;

        public MyraXamlCompiler()
        {
            TypeSystem = new MyraTypeSystem();

            Configuration = CreateConfiguration(TypeSystem);

            EmitMappings = new XamlLanguageEmitMappings<MyraCecilILEmitter, XamlILNodeEmitResult>();

            _compiler = new CompilerImpl(Configuration, EmitMappings);
        }

        public void Transform(XamlDocument document)
        {
            _compiler.Transform(document);
        }

        public AstTransformationContext CreateTransformationContext(XamlDocument document)
        {
            return _compiler.CreateTransformationContext(document);
        }

        private static TransformerConfiguration CreateConfiguration(MyraTypeSystem typeSystem)
        {
            var typeMappings = new XamlLanguageTypeMappings(typeSystem);

            var contentProperty = typeSystem.FindType(nameof(ContentAttribute));
            typeMappings.ContentAttributes.Add(contentProperty);

            var typeConverter = typeSystem.FindType(nameof(TypeConverterAttribute));
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
         

        private sealed class CompilerImpl : XamlCompiler<MyraCecilILEmitter, XamlILNodeEmitResult>
        {
            public CompilerImpl(
                TransformerConfiguration configuration,
                XamlLanguageEmitMappings<MyraCecilILEmitter, XamlILNodeEmitResult> emitMappings)
                : base(configuration, emitMappings, fillWithDefaults: true)
            {
            }

            protected override XamlEmitContext<MyraCecilILEmitter, XamlILNodeEmitResult>
                InitCodeGen(
                    IFileSource file,
                    IXamlTypeBuilder<MyraCecilILEmitter> declaringType,
                    MyraCecilILEmitter emitter,
                    XamlRuntimeContext<MyraCecilILEmitter, XamlILNodeEmitResult> runtimeContext,
                    bool needContextLocal)
            {
                IXamlLocal? contextLocal = null;

                if (needContextLocal)
                {
                    contextLocal = emitter.GetLocal(
                        _configuration.TypeSystem.FindType("XamlX.XamlRuntimeContext")!);
                }

                return new MyraXamlEmitContext(
                    emitter,
                    _configuration,
                    _emitMappings,
                    runtimeContext,
                    contextLocal,
                    declaringType,
                    file,
                    Emitters);
            }
        }
    }
}
