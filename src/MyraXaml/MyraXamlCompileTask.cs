using Microsoft.Build.Framework; 
using Mono.Cecil;
using Mono.Cecil.Cil;
using Myra.Xaml.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public override bool Execute()
        {
            try
            {
                if (!File.Exists(TargetPath))
                {
                    Log.LogError(
                        "Myra XAML: target assembly '{0}' does not exist.",
                        TargetPath);

                    return false;
                }

                if (XamlFiles.Length == 0)
                {
                    Log.LogMessage(
                        MessageImportance.Low,
                        "Myra XAML: no XAML files found.");

                    return true;
                }

                CompileAssembly();

                return !Log.HasLoggedErrors;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: true);
                return false;
            }
        }

        private void CompileAssembly()
        {
            Log.LogMessage(
                MessageImportance.Normal,
                "Myra XAML: weaving '{0}'.",
                TargetPath);

            var resolver = new DefaultAssemblyResolver();

            foreach (var asm in ReferenceAssemblies)
            {
                var path = asm.ItemSpec;

                if (!File.Exists(path))
                    continue;

                var directory = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(directory))
                    resolver.AddSearchDirectory(directory);
            }
              

            var compiler = new MyraXamlCompiler(TargetPath, ReferenceAssemblies);


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

            Log.LogMessage(
                MessageImportance.Normal,
                "Myra XAML: finished weaving '{0}'.",
                TargetPath);
        }

        private void CompileXamlFile(MyraXamlCompiler compiler,  AssemblyDefinition assembly, ITaskItem item)
        {
            var xamlPath = item.GetMetadata("FullPath");

            if (string.IsNullOrWhiteSpace(xamlPath))
                xamlPath = item.ItemSpec;

            if (!Path.IsPathRooted(xamlPath))
                xamlPath = Path.GetFullPath(xamlPath);

            Log.LogMessage(
                MessageImportance.Normal,
                "Myra XAML: compiling '{0}'.",
                xamlPath);

            if (!File.Exists(xamlPath))
            {
                Log.LogError(
                    "Myra XAML: XAML file '{0}' does not exist.",
                    xamlPath);

                return;
            }

            var className = GetClassName(item, xamlPath);

            if (string.IsNullOrWhiteSpace(className))
            {
                Log.LogError(
                    "Myra XAML: could not determine the code-behind type for '{0}'. " +
                    "Specify x:Class or the Myra XAML MSBuild metadata.",
                    xamlPath);

                return;
            }

            var targetType = FindType(assembly.MainModule, className!);

            if (targetType == null)
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
                targetType.FullName);

            var text = File.ReadAllText(xamlPath);

            CompileIntoType(
                compiler,
                targetType,
                text,
                xamlPath);
        }

        private void CompileIntoType(
            MyraXamlCompiler compiler,
            TypeDefinition targetType,
            string fileContents,
            string fileName)
        { 
            var document = new MyraXamlParser(compiler.Configuration).Parse(fileContents);

            compiler.Transform(document);

            compiler.CompileInto(document, targetType, fileName, fileContents);
        }

        private static TypeDefinition? FindType(
            ModuleDefinition module,
            string fullName)
        {
            // Cecil uses '/' for nested types.
            fullName = fullName.Replace('+', '/');

            return FindTypeRecursive(module.Types, fullName);
        }

        private static TypeDefinition? FindTypeRecursive(
            IEnumerable<TypeDefinition> types,
            string fullName)
        {
            foreach (var type in types)
            {
                if (type.FullName == fullName)
                    return type;

                var nested = FindTypeRecursive(
                    type.NestedTypes,
                    fullName);

                if (nested != null)
                    return nested;
            }

            return null;
        }

        private string? GetClassName(
            ITaskItem item,
            string xamlPath)
        {
            var explicitClass = item.GetMetadata("XamlClass");

            if (!string.IsNullOrWhiteSpace(explicitClass))
                return explicitClass;

            var fullXamlPath = Path.GetFullPath(xamlPath);
            var fullProjectDirectory = Path.GetFullPath(ProjectDirectory);
#if !NET6_0_OR_GREATER
            var relativePath = PathNetCore.GetRelativePath(fullProjectDirectory, fullXamlPath);
#else
            var relativePath = Path.GetRelativePath(fullProjectDirectory, fullXamlPath);
#endif
            var directory = Path.GetDirectoryName(relativePath);

            var name = Path.GetFileNameWithoutExtension(relativePath);

            if (string.IsNullOrWhiteSpace(name))
                return null;

            var namespaceParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(RootNamespace))
                namespaceParts.Add(RootNamespace);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                namespaceParts.AddRange(
                    directory
                        .Split(
                            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                            StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => x != "."));
            }

            namespaceParts.Add(name);

            return string.Join(".", namespaceParts);
        }
    }
}
