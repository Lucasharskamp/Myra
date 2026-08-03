using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;

namespace Myra.Xaml
{
    public class CompileMyraXamlTask : ITask
    {
        public const string MyraCompileOutputMetadataName = "MyraCompileOutput";

        public bool Execute()
        {
            Enum.TryParse(ReportImportance, true, out MessageImportance outputImportance);

            var outputPath = AssemblyFile.GetMetadata(MyraCompileOutputMetadataName);
            var refOutputPath = RefAssemblyFile?.GetMetadata(MyraCompileOutputMetadataName);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            if (!string.IsNullOrEmpty(refOutputPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(refOutputPath));
            }

            var msg = $"CompileMyraXamlTask -> AssemblyFile:{AssemblyFile}, ProjectDirectory:{ProjectDirectory}, OutputPath:{outputPath}";
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(msg, "Setup", "CompileMyraXamlTask",  outputImportance < MessageImportance.Low ? MessageImportance.High : outputImportance));

            var res = XamlCompilerTaskExecutor.Compile(BuildEngine,
                AssemblyFile.ItemSpec, outputPath,
                RefAssemblyFile?.ItemSpec, refOutputPath,
                References?.Select(i => i.ItemSpec).ToArray() ?? Array.Empty<string>(),
                ProjectDirectory, VerifyIl, DefaultCompileBindings, outputImportance,
                new XamlCompilerDiagnosticsFilter(AnalyzerConfigFiles),
                (SignAssembly && !DelaySign) ? AssemblyOriginatorKeyFile : null,
                SkipXamlCompilation, DebuggerLaunch, VerboseExceptions, CreateSourceInfo);

            if (res.Success && !res.WrittenFile)
            {
                // To simplify incremental build checks, copy the input files to the expected output locations even if the Xaml compiler didn't do anything.
                CopyAndTouch(AssemblyFile.ItemSpec, outputPath);
                CopyAndTouch(Path.ChangeExtension(AssemblyFile.ItemSpec, ".pdb"), Path.ChangeExtension(outputPath, ".pdb"), false);

                if (!string.IsNullOrEmpty(refOutputPath) && RefAssemblyFile != null)
                {
                    CopyAndTouch(RefAssemblyFile.ItemSpec, refOutputPath!);
                }
            }

            return res.Success;
        }

        private static void CopyAndTouch(string source, string destination, bool shouldExist = true)
        {
            var normalizedSource = Path.GetFullPath(source);
            var normalizedDestination = Path.GetFullPath(destination);

            if (!File.Exists(normalizedSource))
            {
                if (shouldExist)
                {
                    throw new FileNotFoundException($"Could not copy file '{normalizedSource}'. File does not exist.");
                }

                return;
            }

            if (normalizedSource != normalizedDestination)
            {
                File.Copy(normalizedSource, normalizedDestination, overwrite: true);
            }

            File.SetLastWriteTimeUtc(normalizedDestination, DateTime.UtcNow);
        }

        [Required]
        public string ProjectDirectory { get; set; } = default!;

        [Required]
        public ITaskItem AssemblyFile { get; set; } = default!;

        public ITaskItem? RefAssemblyFile { get; set; }

        public ITaskItem[]? References { get; set; }

        public bool VerifyIl { get; set; }

        public bool DefaultCompileBindings { get; set; }

        public bool SkipXamlCompilation { get; set; }

        public string AssemblyOriginatorKeyFile { get; set; } = default!;
        public bool SignAssembly { get; set; }
        public bool DelaySign { get; set; }

        public string ReportImportance { get; set; } = default!;

        public IBuildEngine BuildEngine { get; set; } = default!;
        public ITaskHost HostObject { get; set; } = default!;

        public bool DebuggerLaunch { get; set; }

        public bool CreateSourceInfo { get; set; }

        public bool VerboseExceptions { get; set; }

        public ITaskItem[] AnalyzerConfigFiles { get; set; } = default!;
    }
}