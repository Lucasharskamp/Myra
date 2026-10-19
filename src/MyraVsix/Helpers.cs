using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Myra.Classifier;
using System;

namespace Myra
{
    internal static class Helpers
    {
        /// <summary>
        /// Checks if the file is a .xaml file and has csproj attribute MyraEditor assigned with
        /// value 'true' or '1'.
        /// </summary>
        /// <param name="buffer">Text buffer of the file to check</param>
        /// <param name="documents">Document repository to receive metadata from <paramref name="buffer"/></param>
        /// <returns>Whether is a Myra .xaml file</returns>
        public static bool IsMyraXamlFile(ITextBuffer buffer, ITextDocumentFactoryService documents)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!documents.TryGetTextDocument(buffer, out var document))
                return false;

            var filePath = document.FilePath;

            if (filePath == null ||
                !filePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (Package.GetGlobalService(typeof(DTE)) is not DTE dte)
            {
                return false;
            }

            var projectItem = dte.Solution.FindProjectItem(filePath);

            var project = projectItem.ContainingProject;

            if (Package.GetGlobalService(typeof(SVsSolution)) is not IVsSolution solution)
            {
                return false;
            } 

            ErrorHandler.ThrowOnFailure(solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy));
            ErrorHandler.ThrowOnFailure(hierarchy.ParseCanonicalName(filePath, out var itemId));

            if (hierarchy is not IVsBuildPropertyStorage storage)
            {
                return false;
            }

            ErrorHandler.ThrowOnFailure(storage.GetItemAttribute(itemId, "MyraEditor", out var myraEditor));

            if (String.IsNullOrWhiteSpace(myraEditor))
            {
                return false;
            }

            var trim = myraEditor.Trim();

            return (String.Equals(trim, "1", StringComparison.OrdinalIgnoreCase)
                || String.Equals(trim, "true", StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsAssignableFrom(this MyraXamlType target, MyraXamlType source)
        {
            return SymbolEqualityComparer.Default.Equals(target.Type, source.Type)
                || source.Type.AllInterfaces.Contains(target.Type, SymbolEqualityComparer.Default)
                || IsBaseTypeOf(target, source);
        }

        static bool IsBaseTypeOf(MyraXamlType target, MyraXamlType source)
        {
            for (var type = source.Type.BaseType; type != null; type = type.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(target.Type, type))
                    return true;
            }

            return false;
        }

        public static string? GetFileName(this ITextBuffer buffer)
        {
            buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument? document);
            return document?.FilePath;
        }
    }
}
