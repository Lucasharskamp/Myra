using Microsoft.Build.Framework;
using System;

namespace Myra.Xaml
{
    internal class XamlCompilerTaskExecutor
    {
        private const string CompiledMyraXamlNamespace = "CompiledMyraXaml";

        static bool CheckXamlName(IResource r)
        {
            return r.Name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase);
        }

        internal static CompileResult Compile(IBuildEngine engine,
            string input, string output,
            string? refInput, string? refOutput,
            string[] references, string projectDirectory,
            bool verifyIl, bool defaultCompileBindings, MessageImportance logImportance,
            XamlCompilerDiagnosticsFilter diagnosticsFilter, string? strongNameKey,
            bool skipXamlCompilation, bool debuggerLaunch, bool verboseExceptions, bool createSourceInfo)
        {
            throw new NotImplementedException();
        }
    }
}
