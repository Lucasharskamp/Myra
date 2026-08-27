using Microsoft.Build.Framework; 
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Myra.Xaml.Compiler;
using Myra.Xaml.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;
using XamlX.TypeSystem;

namespace Myra.Xaml
{ 

    public sealed class MyraXamlCompileTask : Microsoft.Build.Utilities.Task
    { 

        [Required]
        public string TargetPath { get; set; } = default!;

        public ITaskItem[] XamlFiles { get; set; } = [];

        [Required]
        public string RootNamespace { get; set; } = null!;

        [Required]
        public string ProjectDirectory { get; set; } = null!;

        [Required]
        public ITaskItem[] ReferenceAssemblies { get; set; } = [];

        public bool Debug { get; set; }

        public CecilTypeSystem? TypeSystem { get; set; }

        public override bool Execute()
        { 
            AssemblyDefinition? assembly = null;
            try
            {
                if (!File.Exists(TargetPath))
                {
                    Log.LogError("Myra XAML: target assembly '{0}' does not exist.", TargetPath);

                    return false;
                }

                if (XamlFiles.Length == 0)
                {
                    Log.LogMessage(MessageImportance.Low, "Myra XAML: no XAML files found.");

                    return true;
                }

                Log.LogMessage(MessageImportance.Normal, "Myra XAML: weaving '{0}'.", TargetPath);
                var assemblies = ReferenceAssemblies
                    .Select(x => x.ItemSpec)
                    .Where(File.Exists)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                TypeSystem = new CecilTypeSystem(assemblies, TargetPath);
                using var compiler = new MyraXamlCompiler(TypeSystem);

                assembly = compiler.TypeSystem.GetAssembly(compiler.TypeSystem.FindAssembly(Path.GetFileNameWithoutExtension(TargetPath)!)!);

                foreach (var item in XamlFiles)
                {
                    CompileXamlFile(compiler, assembly, item);
                }

                var writerParameters = new WriterParameters
                {
                    WriteSymbols = Debug,
                    SymbolWriterProvider = new PortablePdbWriterProvider()
                };

                assembly.Write(TargetPath, writerParameters);

                Log.LogMessage(MessageImportance.Normal, "Myra XAML: finished weaving '{0}'.", TargetPath);

                return !Log.HasLoggedErrors;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: true);
                return false;
            }
            finally
            {
                assembly?.Dispose();
            }
        }

        private void CompileXamlFile(MyraXamlCompiler compiler,  AssemblyDefinition assembly, ITaskItem item)
        {
            var xamlPath = item.GetMetadata("FullPath");

            var text = File.ReadAllText(xamlPath);
            var document = XDocumentXamlParser.Parse(text); 

            if (string.IsNullOrWhiteSpace(xamlPath))
                xamlPath = item.ItemSpec;

            if (!Path.IsPathRooted(xamlPath))
                xamlPath = Path.GetFullPath(xamlPath);

            Log.LogMessage(MessageImportance.Normal, "Myra XAML: compiling '{0}'.",  xamlPath);

            if (!File.Exists(xamlPath))
            {
                Log.LogError("Myra XAML: XAML file '{0}' does not exist.", xamlPath);
                return;
            } 

            var className = GetClassName((document.Root as XamlAstObjectNode)!, item, xamlPath);
            if (string.IsNullOrWhiteSpace(className))
            {
                Log.LogError("Myra XAML: could not determine the code-behind type for '{0}'. ", xamlPath);
                return;
            }

            var currentClassDefinition = FindTypeRecursive(assembly.MainModule.Types, className!);
            if (currentClassDefinition == null)
            {  
                // Note: the code-behind type must also be compiled before this MSBuild is invoked!
                Log.LogError("Myra XAML: code-behind type '{0}' was not found for XAML file '{1}'. ",
                    className,
                    TargetPath);

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
                Log.LogError("Myra XAML: code-behind type '{0}' must derive from 'Myra.Graphics2D.UI.Widget'. ",
                  className,
                  TargetPath);
            }

            var assemblyMappings = compiler.Configuration.XmlnsMappings.Namespaces[MyraXamlCompiler.MyraMappings];
            if (assemblyMappings.Any(a => a.ns != currentClass.Namespace && a.asm != currentClass.Assembly))
            {
                assemblyMappings.Add((currentClass.Assembly!, currentClass.Namespace!));
            } 

            Log.LogMessage(MessageImportance.Low, "Myra XAML: code-behind type is '{0}'.", currentClass.FullName);
             
            compiler.CompileInto(document, currentClassDefinition, xamlPath, text);
        }  

        private static TypeDefinition? FindTypeRecursive(Collection<TypeDefinition> types, string fullName)
        {
            foreach (var type in types)
            {
                if (type.FullName == fullName)
                    return type;

                var nested = FindTypeRecursive(type.NestedTypes, fullName);

                if (nested != null)
                    return nested;
            }

            return null;
        }

        private string? GetClassName(XamlAstObjectNode node, ITaskItem item, string xamlPath)
        {
            // get "x:Class" directive from the element, in case there is an override
            var documentDirectives = node.Children
                .OfType<XamlAstXmlDirective>()
                .Where(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Class")
                .ToArray();

            if (documentDirectives.Length > 1)
            {
                throw new XamlLoadException("x:Class can only be defined once on a document!", node);
            }

            if (documentDirectives.Length == 1)
            {
                var classDirective = documentDirectives[0];
                node.Children.Remove(classDirective);
                // There should only be 1 value in there, namely the full specification of the class
                // (and nothing else)
                if (classDirective.Values.Count != 1 ||
                    classDirective.Values[0] is not XamlAstTextNode text)
                {
                    throw new XamlLoadException(
                        "x:Class must have a single string value.", classDirective);
                }
                return text.Text;
            }

            var explicitClass = item.GetMetadata("XamlClass");

            if (!string.IsNullOrWhiteSpace(explicitClass))
                return explicitClass;

            var fullXamlPath = Path.GetFullPath(xamlPath);
            var fullProjectDirectory = Directory.GetParent(Path.GetFullPath(ProjectDirectory)); 
            var relativePath = PathNetCore.GetRelativePath(fullProjectDirectory.FullName, fullXamlPath);
            var directory = Path.GetDirectoryName(relativePath).Replace('\\', '.');

            var name = Path.GetFileNameWithoutExtension(relativePath);

            if (string.IsNullOrWhiteSpace(name))
                return null;

            return directory + "." + name;
        }
    }
}
