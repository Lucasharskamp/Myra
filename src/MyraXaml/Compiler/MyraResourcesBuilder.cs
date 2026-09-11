using Mono.Cecil;
using Myra.Xaml.Helpers;
using System.Collections.Generic;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Compiler
{
    public sealed class MyraResourcesBuilder
    {
        private IXamlMethod StylesheetAddMethod { get; }
        private IXamlTypeBuilder<IXamlILEmitter> ResourcesTypeBuilder { get; }
        public IXamlMethod GetMethod { get; }
        private XamlTypeWellKnownTypes WellKnownTypes { get; }
        private IXamlField StylesheetsContainer { get; }
        public const string GetStylesheetMethodName = "GetStylesheet";

        public MyraResourcesBuilder(CecilTypeSystem typeSystem, TypeDefinition resourceType, XamlTypeWellKnownTypes wellKnownTypes)
        {
            WellKnownTypes = wellKnownTypes;
            ResourcesTypeBuilder = typeSystem.CreateTypeBuilder(resourceType, true);


            var atlasContainerType = wellKnownTypes.DictionaryOfT2.MakeGenericType(wellKnownTypes.String,
                                                TypesContainer.TextureRegionAtlas);


            var funcStylesheetType = wellKnownTypes.GetFuncOfT(1).MakeGenericType(TypesContainer.Stylesheet); 
            var lazyStylesheetType = TypesContainer.LazyOfT1.MakeGenericType(TypesContainer.Stylesheet);
            var stylesheetsContainerType = wellKnownTypes.DictionaryOfT2.MakeGenericType(
                                            wellKnownTypes.String,
                                            lazyStylesheetType
                                        );

            StylesheetAddMethod = stylesheetsContainerType.GetMethod(m => m.Name == "Add");
            var stylesheetsGetMethod = stylesheetsContainerType.GetMethod(m => m.Name == "get_Item");
            StylesheetsContainer = ResourcesTypeBuilder.DefineField(stylesheetsContainerType, "_stylesheets", XamlVisibility.Private, true);

            var lazyGetValue = lazyStylesheetType.GetMethod(m => m.Name == "get_Value");
            var lazyConstructor = lazyStylesheetType.GetConstructor([funcStylesheetType]);
              
            /*
             *  internal static Stylesheet Get(string name)
             *   => _stylesheets[name].Value;
             */
            var getMethodBuilder = ResourcesTypeBuilder.DefineMethod(TypesContainer.Stylesheet,
                                                        [wellKnownTypes.String],
                                                        GetStylesheetMethodName,
                                                        XamlVisibility.Assembly,
                                                        true,
                                                        false);
            var getMethodGen = getMethodBuilder.Generator;
            getMethodGen.Ldsfld(StylesheetsContainer);
            getMethodGen.Ldarg(0);
            getMethodGen.EmitCall(stylesheetsGetMethod);
            getMethodGen.EmitCall(lazyGetValue);
            getMethodGen.Ret();
            GetMethod = getMethodBuilder;
            WellKnownTypes = wellKnownTypes;

            MyraBindingCompilationContext.GetStylesheet = GetMethod;
        }

        public void BuildStaticConstructor(List<(string, IXamlMethod)> stylesheetTypes)
        {
            /*
            *   static __MyraXamlResources()
            *   {                   
            *      _stylesheets = new();
            *      // for each stylesheet
            *      _stylesheets.Add(typename, stylesheet)
            *      
            *      Stylesheet.Current = Get("default_ui_skin.xmms)
            *   }
            */
            var funcStylesheetType = WellKnownTypes.GetFuncOfT(1).MakeGenericType(TypesContainer.Stylesheet);
            var funcStylesheetConstructor = funcStylesheetType.GetConstructor([WellKnownTypes.Object, WellKnownTypes.IntPtr]);
            var lazyStylesheetConstructor = TypesContainer
                .LazyOfT1
                .MakeGenericType(TypesContainer.Stylesheet)
                .GetConstructor([funcStylesheetType]);
            var stylesheetsCurrentSetMethod = TypesContainer.Stylesheet.GetMethod(m => m.Name == "set_Current");
            var initializeMethod = ResourcesTypeBuilder.DefineConstructor(true, []);

            var initializeMethodGen = initializeMethod.Generator;

            // _stylesheets = new();
            initializeMethodGen.Newobj(StylesheetsContainer.FieldType.GetConstructor([]));
            initializeMethodGen.Stsfld(StylesheetsContainer);

            // _stylesheets.Add(typename, stylesheet)
            foreach (var stylesheetType in stylesheetTypes)
            {
                initializeMethodGen.Ldsfld(StylesheetsContainer);
                initializeMethodGen.Ldstr(stylesheetType.Item1);
                initializeMethodGen.Ldnull();
                initializeMethodGen.Ldftn(stylesheetType.Item2);
                initializeMethodGen.Newobj(funcStylesheetConstructor);
                initializeMethodGen.Newobj(lazyStylesheetConstructor);
                initializeMethodGen.EmitCall(StylesheetAddMethod);
            }

            // Stylesheet.Current = Get("default_ui_skin");
            initializeMethodGen.Ldstr("default_ui_skin");
            initializeMethodGen.EmitCall(GetMethod);
            initializeMethodGen.EmitCall(stylesheetsCurrentSetMethod);

            initializeMethodGen.Ret();
        }
    }
}
