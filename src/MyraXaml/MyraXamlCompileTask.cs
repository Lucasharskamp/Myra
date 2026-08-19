using Microsoft.Build.Framework; 
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Myra.Xaml.Compiler;
using System;
using System.IO;
using System.Linq;
using XamlX.Parsers;
using XamlX.TypeSystem;

namespace Myra.Xaml
{ 

    public sealed class MyraXamlCompileTask : Microsoft.Build.Utilities.Task
    { 
        public static TypeDefinition? CurrentClass { get; set; }

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
                var compiler = new MyraXamlCompiler(TypeSystem);

                var assembly = compiler.TypeSystem.GetAssembly(compiler.TypeSystem.FindAssembly(Path.GetFileNameWithoutExtension(TargetPath)!)!);

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
        }

        private void CompileXamlFile(MyraXamlCompiler compiler,  AssemblyDefinition assembly, ITaskItem item)
        {
            var xamlPath = item.GetMetadata("FullPath");

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

            var className = GetClassName(item, xamlPath);

            if (string.IsNullOrWhiteSpace(className))
            {
                Log.LogError(
                    "Myra XAML: could not determine the code-behind type for '{0}'. ",
                    xamlPath);

                return;
            }

            CurrentClass = FindType(assembly.MainModule, className!);

            if (CurrentClass == null)
            {
                Log.LogError(
                    "Myra XAML: code-behind type '{0}' was not found in '{1}'. " +
                    "The C# project must be compiled before Myra XAML compilation.",
                    className,
                    TargetPath);

                return;
            }

            Log.LogMessage(
                MessageImportance.Low,
                "Myra XAML: code-behind type is '{0}'.",
                CurrentClass.FullName);

            var text = File.ReadAllText(xamlPath); 
            var document = XDocumentXamlParser.Parse(text);

            compiler.Transform(document);

            compiler.CompileInto(document, CurrentClass, xamlPath, text);
        } 

        private static TypeDefinition? FindType(ModuleDefinition module, string fullName)
        {
            if (fullName.Contains('+'))
            {

            }
            // Cecil uses '/' for nested types.
            fullName = fullName.Replace('+', '/');

            return FindTypeRecursive(module.Types, fullName);
        }

        private static TypeDefinition? FindTypeRecursive(
            Collection<TypeDefinition> types,
            string fullName)
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

        private string? GetClassName(ITaskItem item, string xamlPath)
        {
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
