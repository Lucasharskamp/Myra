using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Xml.Linq;


namespace Myra.Analyzer
{ 
     
    /// <summary>
    /// Suppresses warnings in C# that are irrelevant because the .xaml generated IL takes care of them;
    /// These are: <br/>
    /// 1. "unused methods" tied to events
    /// 2. "unused ViewModel properties" tied to x:Bind 
    /// 3. "non-nullable property isn't set" tied to x:Name
    /// </summary>    

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MyraXamlUnusedMemberSuppressor : DiagnosticSuppressor
    {
        private static readonly SuppressionDescriptor UnusedMember =
            new(
                id: "MYRA001",
                suppressedDiagnosticId: "IDE0051",
                justification:
                    "The member is referenced from Myra XAML.");

        public override ImmutableArray<SuppressionDescriptor>
            SupportedSuppressions =>
                ImmutableArray.Create(UnusedMember);

        public override void ReportSuppressions(
            SuppressionAnalysisContext context)
        {
            var compilation = context.Compilation;

            var xamlHandlers = GetXamlEventHandlers(context);

            if (xamlHandlers.Count == 0)
                return;

            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                if (diagnostic.Id != "IDE0051")
                    continue;

                if (diagnostic.Location.SourceTree == null)
                    continue;

                var method = GetDeclaredMethod(
                    context,
                    diagnostic.Location);

                if (method == null)
                    continue;

                if (method.MethodKind != MethodKind.Ordinary)
                    continue;

                var containingType = method.ContainingType;

                if (containingType == null)
                    continue;

                var key = new MethodKey(
                    containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    method.Name);

                if (!xamlHandlers.Contains(key))
                    continue;

                context.ReportSuppression(
                    Suppression.Create(
                        UnusedMember,
                        diagnostic));
            }
        }

        private static HashSet<MethodKey> GetXamlEventHandlers(
            SuppressionAnalysisContext context)
        {
            var result = new HashSet<MethodKey>();

            foreach (var additionalFile in
                     context.Options.AdditionalFiles)
            {
                if (!additionalFile.Path.EndsWith(
                        ".xaml",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                } 

                var text = additionalFile.GetText(
                    context.CancellationToken);

                if (text == null)
                    continue;

                XDocument document;

                try
                {
                    document = XDocument.Parse(
                        text.ToString(),
                        LoadOptions.SetLineInfo);
                }
                catch
                {
                    // The XAML compiler/analyzer responsible for syntax
                    // diagnostics will deal with malformed XAML.
                    continue;
                }

                var codeBehindClass = GetClass(context, additionalFile, document);
                if (codeBehindClass == null)
                    continue;

                foreach (var element in document.Descendants())
                {
                    foreach (var attribute in element.Attributes())
                    {
                        // For now we deliberately don't try to understand
                        // every possible XAML event. We collect attributes
                        // that aren't namespaced directives and later resolve
                        // them against the compilation.
                        if (attribute.IsNamespaceDeclaration)
                            continue;

                        if (attribute.Name.Namespace != XNamespace.None)
                            continue;

                        var value = attribute.Value.Trim();

                        if (value.Length == 0)
                            continue;

                        // We cannot yet know whether this attribute is an
                        // event merely from XML. Store the potential handler.
                        result.Add(new MethodKey(
                                codeBehindClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                value));
                    }
                }
            }

            return result;
        }

        private static INamedTypeSymbol? GetClass(SuppressionAnalysisContext context, AdditionalText additionalFile, XDocument document)
        {
            var xClass = document.Root.Attribute(XName.Get("Class"));
            if (xClass != null)
                return context.Compilation.GetTypeByMetadataName(xClass.Value);

            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.rootnamespace", out var rootNamespace))
                return null;

            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir))
                return null;

            var relativeDirectory = PathNetCore.GetRelativePath(projectDir, additionalFile.Path);
            var directory = Path.GetDirectoryName(relativeDirectory).Replace('\\', '.');

            var name = Path.GetFileNameWithoutExtension(additionalFile.Path);

            if (string.IsNullOrWhiteSpace(name))
                return null;

            return context.Compilation.GetTypeByMetadataName(rootNamespace + "." + directory + "." + name);
        }

        private static IMethodSymbol? GetDeclaredMethod(SuppressionAnalysisContext context, Location location)
        {
            var tree = location.SourceTree;

            if (tree == null)
                return null;

            var root = tree.GetRoot();

            var node = root.FindNode(
                location.SourceSpan,
                getInnermostNodeForTie: true);

            var model = context.GetSemanticModel(tree);

            var symbol = model.GetDeclaredSymbol(node);

            return symbol as IMethodSymbol;
        }

        private readonly struct MethodKey : IEquatable<MethodKey>
        {
            public MethodKey(string? containingType, string methodName)
            {
                ContainingType = containingType;
                MethodName = methodName;
            }

            public string? ContainingType { get; }

            public string MethodName { get; }

            public bool Equals(MethodKey other)
            {
                return
                    string.Equals(
                        ContainingType,
                        other.ContainingType,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        MethodName,
                        other.MethodName,
                        StringComparison.Ordinal);
            }

            public override bool Equals(object? obj)
            {
                return obj is MethodKey other &&
                       Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 31 + (ContainingType?.GetHashCode() ?? 0);
                    hash = hash * 31 + MethodName.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
