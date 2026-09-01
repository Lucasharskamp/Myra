using Mono.Cecil;
using Myra.Xaml.Helpers;
using System.Collections.Generic;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Compiler
{
    public sealed class MyraResourcesBuilder
    {
        private IXamlMethodBuilder<IXamlILEmitter> RegisterMethod { get; } 
        private IXamlTypeBuilder<IXamlILEmitter> ResourcesTypeBuilder { get; }
        public IXamlMethod GetMethod { get; }
        private XamlTypeWellKnownTypes WellKnownTypes { get; }
        private IXamlField StylesheetsContainer { get; }

        public MyraResourcesBuilder(CecilTypeSystem typeSystem, TypeDefinition resourceType, XamlTypeWellKnownTypes wellKnownTypes)
        { 
            ResourcesTypeBuilder = typeSystem.CreateTypeBuilder(resourceType, true);

            var funcStylesheetType = wellKnownTypes.GetFuncOfT(1).MakeGenericType(TypesContainer.StyleSheet); 
            var lazyStylesheetType = TypesContainer.LazyOfT1.MakeGenericType(TypesContainer.StyleSheet);
            var stylesheetsContainerType = wellKnownTypes.DictionaryOfT2.MakeGenericType(
                                            wellKnownTypes.String,
                                            lazyStylesheetType
                                        );

            var stylesheetsAddMethod = stylesheetsContainerType.GetMethod(m => m.Name == "Add");
            var stylesheetsGetMethod = stylesheetsContainerType.GetMethod(m => m.Name == "get_Item");
            StylesheetsContainer = ResourcesTypeBuilder.DefineField(stylesheetsContainerType, "_stylesheets", XamlVisibility.Private, true);

            var lazyGetValue = lazyStylesheetType.GetMethod(m => m.Name == "get_Value");
            var lazyConstructor = lazyStylesheetType.GetConstructor([funcStylesheetType]);
             
            /*
             * private static void Register(string name, Func<Stylesheet> factory)
             * {
             *   _stylesheets.Add(name, new Lazy<Stylesheet>(factory));
             * }
             */
            RegisterMethod = ResourcesTypeBuilder.DefineMethod(wellKnownTypes.Void,
                                    [wellKnownTypes.String, funcStylesheetType],
                                    "Register",
                                    XamlVisibility.Assembly,
                                    true,
                                    false);
            var registerMethodGen = RegisterMethod.Generator;
            registerMethodGen.Ldsfld(StylesheetsContainer);
            registerMethodGen.Ldarg(0);
            registerMethodGen.Ldarg(1);
            registerMethodGen.Newobj(lazyConstructor);
            registerMethodGen.EmitCall(stylesheetsAddMethod);
            registerMethodGen.Ret();

            /*
             *  internal static Stylesheet Get(string name)
             *   => _stylesheets[name].Value;
             */
            var getMethodBuilder = ResourcesTypeBuilder.DefineMethod(TypesContainer.StyleSheet,
                                                        [wellKnownTypes.String],
                                                        "Get",
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
            *      Register(typename, stylesheet)
            *      
            *      Stylesheet.Current = Get("default_ui_skin.xaml)
            *   }
            */
            var funcStylesheetType = WellKnownTypes.GetFuncOfT(1).MakeGenericType(TypesContainer.StyleSheet);
            var funcConstructor = funcStylesheetType.GetConstructor([WellKnownTypes.Object, WellKnownTypes.IntPtr]);
            var stylesheetsCurrentSetMethod = TypesContainer.StyleSheet.GetMethod(m => m.Name == "set_Current");
            var initializeMethod = ResourcesTypeBuilder.DefineConstructor(true, []);

            var initializeMethodGen = initializeMethod.Generator;

            // _stylesheets = new();

            // Register(typename, stylesheet)
            initializeMethodGen.Newobj(StylesheetsContainer.FieldType.GetConstructor([]));
            initializeMethodGen.Stsfld(StylesheetsContainer);
  
            foreach (var stylesheetType in stylesheetTypes)
            {
                initializeMethodGen.Ldstr(stylesheetType.Item1);
                initializeMethodGen.Ldnull();
                initializeMethodGen.Ldftn(stylesheetType.Item2);
                initializeMethodGen.Newobj(funcConstructor);
                initializeMethodGen.EmitCall(RegisterMethod);
            }

            // Stylesheet.Current = Get("default_ui_skin");
            initializeMethodGen.Ldstr("default_ui_skin");
            initializeMethodGen.EmitCall(GetMethod);
            initializeMethodGen.EmitCall(stylesheetsCurrentSetMethod);

            initializeMethodGen.Ret();
        }
    }
}
