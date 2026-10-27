using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq; 
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Myra.Classifier
{
    internal sealed class MyraXamlParser
    { 
        private static readonly Regex XmlnsRegex = new(
            """
            \bxmlns
            (?:\:(?<prefix>[A-Za-z_][A-Za-z0-9_.-]*))?
            \s*=\s*
            (?:"(?<uri>[^"]*)"|'(?<uri>[^']*)')
            """,
            RegexOptions.Compiled |
            RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex TagRegex = new(
            """
            <
            \s*
            (?<closing>/)?
            (?:
                (?<prefix>[A-Za-z_][A-Za-z0-9_.-]*):
            )?
            (?<name>[A-Za-z_][A-Za-z0-9_.-]*)
            """,
            RegexOptions.Compiled |
            RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex AttributeRegex = new(
            """
            (?<![A-Za-z0-9_.:-])
            (?:
                (?<prefix>[A-Za-z_][A-Za-z0-9_-]*):
            )?
            (?<owner>[A-Za-z_][A-Za-z0-9_-]*)
            (?:
                \.
                (?<member>[A-Za-z_][A-Za-z0-9_-]*)
            )?
            \s*=
            "(?:
                (?<value>[A-Za-z0-9_.:\-\*\[\]\(\)\^\%\$\#\@\!]*)
            )?"
            """,
            RegexOptions.Compiled |
            RegexOptions.IgnorePatternWhitespace);

        private readonly Dictionary<string, INamedTypeSymbol?> _typeCache = new(StringComparer.Ordinal);
        private readonly object _typeCacheLock = new();
        private readonly VisualStudioWorkspace _workspace; 

        private Compilation? currentCompilation;
        private bool _isLoaded = false;

        public MyraXamlParser(VisualStudioWorkspace workspace)
        { 
            _workspace = workspace;
        }

        public async Task<MyraXamlDocumentModel?> ParseAsync(ITextBuffer buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_isLoaded)
            {
                await LoadTypesAsync(buffer, cancellationToken);
            }

            if (currentCompilation == null)
            {
                return null;
            }

            var snapshot = buffer.CurrentSnapshot;
            var text = snapshot.GetText();

            var namespaces = ParseNamespaces(text);

            var elements = new List<MyraXamlElement>();

            var stack = new Stack<MyraXamlElement>();

            foreach (Match match in TagRegex.Matches(text))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!match.Success)
                    continue;

                var tagStart = match.Index;
                var nameGroup = match.Groups["name"];
                  
                if (!nameGroup.Success)
                    continue;


                var closingGroup = match.Groups["closing"];
                var prefixGroup = match.Groups["prefix"];
 
                var rawName = nameGroup.Value;

                // Closing tags don't create semantic elements.
                if (closingGroup.Success)
                {
                    CloseElement(snapshot, rawName, closingGroup, stack);

                    continue;
                }

                var tagEnd = FindTagEnd(text, tagStart);

                var selfClosing = IsSelfClosing(text, tagStart, tagEnd);

                var parent =
                    stack.Count > 0
                        ? stack.Peek()
                        : null;

                var prefix = prefixGroup.Success
                   ? prefixGroup.Value
                   : null;

                var element =
                    CreateElement(snapshot, match, tagStart, tagEnd, prefix,
                    rawName, parent, namespaces, cancellationToken);

                elements.Add(element);

                parent?.Children.Add(element);

                ParseAttributes(snapshot, text, tagStart,  tagEnd,  element,  namespaces,  cancellationToken);

                if (!selfClosing)
                    stack.Push(element);
            }

            return new MyraXamlDocumentModel
            {
                Snapshot = snapshot,
                Elements = elements,
                Namespaces = namespaces
            };
        }

        private async Task LoadTypesAsync(ITextBuffer buffer, CancellationToken cancellationToken)
        {
            var snapshot = buffer.CurrentSnapshot;
            var filePath = buffer.GetFileName();
            if (filePath == null)
                return;

            var project = _workspace.CurrentSolution.Projects.FirstOrDefault(p =>
            {
                if (p.FilePath == null)
                    return false;
                 
                return p.AdditionalDocuments.Any(f => String.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            });
             
            if (project is null)
            { 
                return;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
                return;

            currentCompilation = compilation;

            // get default Widget type. 
            _typeCache[":Widget"] = LoadType(MyraShared.WidgetAddress);

            // get code-behind type
            var codeBehindDocument = project.Documents.FirstOrDefault(d => String.Equals(d.FilePath, filePath + ".cs", StringComparison.OrdinalIgnoreCase));
            if (codeBehindDocument != null)
            {
                var typesInDocument = await GetTypesAsync(codeBehindDocument, cancellationToken);
                var start = filePath.LastIndexOf('\\'); 
                var typeName = filePath.Substring(start+1, filePath.LastIndexOf('.') - start - 1);
                var type = typesInDocument.FirstOrDefault(t => String.Equals(t.Name, typeName, StringComparison.Ordinal));
                if (type != null)
                {
                    lock (_typeCacheLock)
                    {
                        _typeCache[":" + type.Name] = type;
                    }
                }
            } 
 
            _isLoaded = true;
        }

        public static async Task<IReadOnlyList<INamedTypeSymbol>> GetTypesAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            if (root is null || semanticModel is null)
                return [];

            return [.. root.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .Select(node => semanticModel.GetDeclaredSymbol(node, cancellationToken))
                .OfType<INamedTypeSymbol>()];
        }


        private INamedTypeSymbol? LoadType(string name)
        {
            if (currentCompilation == null)
                throw new InvalidOperationException();

            var type = currentCompilation.GetTypeByMetadataName(name);
            if (type == null)
            { 
                return null;
            }
            return type;
        }
         

        private INamedTypeSymbol? ResolveType(string? prefix, string name, IReadOnlyDictionary<string, string> namespaces)
        {
            var key = (prefix ?? string.Empty) + ":" + name;

            lock (_typeCacheLock)
            {
                if (_typeCache.TryGetValue(key, out var cached))
                {
                    return cached;
                }
            }

            string typeNamespace;

            if (prefix == null)
            {
                typeNamespace = MyraShared.DefaultWidgetsNamespace;
            }
            else if (namespaces.TryGetValue(prefix, out var uri))
            {
                typeNamespace = uri;
            }
            else
            {
                lock (_typeCacheLock)
                {
                    _typeCache[key] = null;
                }
                return null;
            }

            var result = LoadType(typeNamespace + "." + name);

            lock (_typeCacheLock)
            {
                _typeCache[key] = result;
            }

            return result;
        }



        private MyraXamlElement CreateElement(
            ITextSnapshot snapshot,
            Match tagMatch,
            int tagStart,
            int tagEnd,
            string? prefix,
            string rawName,
            MyraXamlElement? parent,
            IReadOnlyDictionary<string, string> namespaces,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nameGroup = tagMatch.Groups["name"]; 
            var prefixGroup = tagMatch.Groups["prefix"];

            /*
             * Check if it is a property element, like <Grid.ColumnsProportions>
             * If so, the name is split:
             *     owner   = Grid
             *     member  = ColumnsProportions 
             * It is only a property element if:  
             *   1. the owner resolves to a type;
             *   2. the member is a property on the parent element's type.
             */ 
            var dot = rawName.IndexOf('.');

            if (dot > 0 &&
                dot < rawName.Length - 1
                && parent?.Type != null)
            {
                var ownerName = rawName.Substring(0, dot);
                var memberName = rawName.Substring(dot + 1); 
                var ownerStart =  nameGroup.Index;
                var memberStart =  nameGroup.Index + dot +  1;
                var ownerType = ResolveType(prefix, ownerName, namespaces);
                var isValid = ownerType != null && IsInstancePropertyOrEvent(ownerType, memberName);

                return new MyraXamlElement(snapshot, rawName, null, isValid)
                { 
                    Type = ownerType,

                    IsPropertyElement = true,

                    NameSpan = new SnapshotSpan(snapshot,  snapshot.Length > 0 ? ownerStart : 0,  rawName.Length),
                    OwnerNameSpan =  new SnapshotSpan(snapshot, ownerStart,  ownerName.Length),
                    MemberNameSpan = new SnapshotSpan(snapshot, memberStart, memberName.Length),
                    Parent = parent,
                    TagSpan = new SnapshotSpan(snapshot, tagStart, Math.Max(0,tagEnd - tagStart + 1))
                };
                   
            } 
             
            var elementStart =
                prefixGroup.Success
                    ? prefixGroup.Index
                    : nameGroup.Index;

            var elementLength =
                nameGroup.Index +
                nameGroup.Length -
                elementStart;

            var nameSpan = new SnapshotSpan(snapshot, elementStart, elementLength);
            var type = ResolveType(prefix, rawName, namespaces);

            return new MyraXamlElement(snapshot, rawName, prefix, type != null)
            {
                Type = type,
                IsPropertyElement = false,
                NameSpan = nameSpan,
                Parent = parent,
                TagSpan = new SnapshotSpan(snapshot, tagStart, Math.Max(0, tagEnd - tagStart + 1))
            };
        }

        private void ParseAttributes(
            ITextSnapshot snapshot,
            string text,
            int tagStart,
            int tagEnd,
            MyraXamlElement element,
            IReadOnlyDictionary<string, string> namespaces,
            CancellationToken cancellationToken)
        {
            if (tagEnd < tagStart)
                return;

            var length =
                tagEnd - tagStart + 1;

            if (length <= 0)
                return;

            var tagText = text.Substring(tagStart, length);

            foreach (Match match in AttributeRegex.Matches(tagText))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!match.Success)
                    continue;

                var prefixGroup = match.Groups["prefix"];
                var ownerGroup = match.Groups["owner"];
                var memberGroup = match.Groups["member"];
                var valueGroup = match.Groups["value"];

                if (!ownerGroup.Success || !valueGroup.Success)
                    continue;

                var prefix = prefixGroup.Success ? prefixGroup.Value : null;
                var owner = ownerGroup.Value; 
                var valueSpan = new SnapshotSpan(snapshot, tagStart + valueGroup.Index, valueGroup.Length);
                var ownerSpan = new SnapshotSpan(snapshot, tagStart + ownerGroup.Index, owner.Length);
                var member = memberGroup.Success ? memberGroup.Value : null;
 
                /*
                 * XML namespace declarations are attributes lexically,
                 * but are not Myra properties.
                 */
                if (string.Equals(owner, "xmlns",  StringComparison.Ordinal)
                    || string.Equals(prefix, "xmlns", StringComparison.Ordinal))
                {
                    continue;
                }

                // if member is not null, it means it's a member-based attribute, like Grid.Row
                if (member != null)
                {
                    var ownerType = ResolveType(prefix, owner, namespaces);

                    // For a qualified owner, classify the entire
                    // "Grid" or "prefix:Grid" as the owner. 
                    var isValid = ownerType != null && IsStaticAttachedProperty(ownerType, member);
                    var memberSpan = new SnapshotSpan(snapshot, tagStart + memberGroup.Index, member.Length);

                    var attr =
                        new MyraXamlAttribute(member, prefix, memberSpan, valueSpan, isValid, false)
                        {  
                            IsAttachedProperty = true,
                            OwnerNameSpan = ownerSpan,
                            OwnerType = ownerType,
                            IsOwnerValid = ownerType != null
                        };

                    element.Attributes.Add(attr);

                    continue;
                } 

                bool valid, isFeature;
                // Check for "x:Name" (code-behind reference)
                if (prefix != null && namespaces.TryGetValue(prefix, out var behindNamespace)
                    && String.Equals(behindNamespace, MyraShared.Xaml2006Schema, StringComparison.Ordinal))
                {
                    valid = String.Equals(owner, "Name", StringComparison.Ordinal);
                    isFeature = true;
                }
                else  // regular property, say Width="100" or Text="Hello world!"
                {
                    valid = element.Type != null && IsInstancePropertyOrEvent(element.Type, owner);
                    isFeature = false;
                }
                 
                var attributeStart = tagStart + ownerGroup.Index;

                var attribute =
                    new MyraXamlAttribute(owner, prefix, ownerSpan, valueSpan, valid, isFeature)
                    {  
                        OwnerType = element.Type
                    };

                element.Attributes.Add(attribute);
            }
        }
         

        public static bool IsStaticAttachedProperty(INamedTypeSymbol type, string name)
        {
            var innerName = "Get" + name;
            for (var current = type; current is not null; current = current.BaseType)
            {
                foreach (var member in current.GetMembers(innerName))
                {
                    if (member is IMethodSymbol
                        && member.IsStatic
                        && member.DeclaredAccessibility == Accessibility.Public)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public static  bool IsInstancePropertyOrEvent(INamedTypeSymbol type, string name)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                foreach (var member in current.GetMembers(name))
                {
                    if (member is IPropertySymbol or IEventSymbol
                        && !member.IsStatic
                        && member.DeclaredAccessibility == Accessibility.Public)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static Dictionary<string, string> ParseNamespaces(string text)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (Match match in XmlnsRegex.Matches(text))
            {
                var prefix =
                    match.Groups["prefix"].Success
                        ? match.Groups["prefix"].Value
                        : string.Empty;

                var uri =
                    match.Groups["uri"].Value;

                result[prefix] = uri;
            }

            return result;
        }

        private static void CloseElement(ITextSnapshot snapshot, string name, Group closingGroup, Stack<MyraXamlElement> stack)
        {
            /*
             * For well-formed XML this is simply the top element.
             *
             * During editing, however, users frequently have:
             *
             *     <Grid>
             *         <StackPanel>
             *     </Grid>
             *
             * Recover by looking for the closest matching element.
             */
            if (stack.Count == 0)
                return;

            var elements =
                stack.ToArray();

            foreach (var element in elements)
            {
                if (!string.Equals(
                        element.Name,
                        name,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                while (stack.Count > 0)
                {
                    var popped =
                        stack.Pop();

                    if (ReferenceEquals(popped,element))
                    {
                        int start = closingGroup.Index + 1;
                        element.ClosingNameSpan = new SnapshotSpan(snapshot, start, element.NameSpan.Length);

                        if (element.OwnerNameSpan.HasValue)
                        {
                            element.ClosingOwnerNameSpan = new SnapshotSpan(snapshot, start, element.OwnerNameSpan.Value.Length);
                            start += element.OwnerNameSpan.Value.Length + 1;
                        }

                        if (element.MemberNameSpan.HasValue)
                        {
                            element.ClosingMemberNameSpan = new SnapshotSpan(snapshot, start, element.MemberNameSpan.Value.Length);
                        }
                        
                        break;
                    }
                }

                return;
            }
        }

        private static bool IsSelfClosing(ReadOnlySpan<char> text, int start, int end)
        {
            if (end <= start)
                return false;

            var i = end - 1;

            while (i > start && char.IsWhiteSpace(text[i]))
            {
                i--;
            }

            return text[i] == '/';
        }

        private static int FindTagEnd(string text, int start)
        {
            char quote = '\0';

            for (var i = start; i < text.Length; i++)
            {
                var c = text[i];

                if (quote != '\0')
                {
                    if (c == quote)
                        quote = '\0';

                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    quote = c;
                    continue;
                }

                if (c == '>')
                    return i;
            }

            /*
             * The user may currently be half-way through typing an element out. 
             * If so, there is no '>' yet. In that case we treat the rest of the
             * document as the current tag.
             */
            return text.Length - 1;
        }
    }
}
 