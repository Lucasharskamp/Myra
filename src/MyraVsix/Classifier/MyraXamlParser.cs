using Microsoft.CodeAnalysis; 
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
                (?<prefix>[A-Za-z_][A-Za-z0-9_.-]*):
            )?
            (?<owner>[A-Za-z_][A-Za-z0-9_.-]*)
            (?:
                \.
                (?<member>[A-Za-z_][A-Za-z0-9_.-]*)
            )?
            \s*=
            """,
            RegexOptions.Compiled |
            RegexOptions.IgnorePatternWhitespace);

        private readonly Dictionary<string, MyraXamlType> _widgetTypes = new(StringComparer.Ordinal);

        private readonly Dictionary<string, MyraXamlType?> _typeCache = new(StringComparer.Ordinal);
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
                await LoadTypesAsync(buffer);
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

                var closingGroup = match.Groups["closing"];
                var prefixGroup = match.Groups["prefix"];
                var nameGroup = match.Groups["name"];

                if (!nameGroup.Success)
                    continue; 

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
                    CreateElement(snapshot, text, match, tagStart, tagEnd, prefix,
                    rawName, parent, namespaces, cancellationToken);

                elements.Add(element);

                if (parent != null)
                    parent.Children.Add(element);

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

        private async Task LoadTypesAsync(ITextBuffer buffer)
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

            var compilation = await project.GetCompilationAsync();
             
            if (compilation is null)
                return;

            currentCompilation = compilation;

            LoadWidgetType(MyraShared.WidgetAddress); 

            _isLoaded = true;
        }

        private MyraXamlType? LoadType(string name)
        {
            if (currentCompilation == null)
                throw new InvalidOperationException();

            var type = currentCompilation.GetTypeByMetadataName(name);
            if (type == null)
            { 
                return null;
            }
            return new MyraXamlType(type);
        }

        private void LoadWidgetType(string name)
        {
            var type = LoadType(name);
            if (type != null) 
            {
                _widgetTypes[name] = type;
            }
        }

        private MyraXamlType? ResolveType(string? prefix, string name, IReadOnlyDictionary<string, string> namespaces)
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
                return null;
            }

            var result = LoadType(typeNamespace + "." + name);
            if (result == null)
            {
                return null;
            }    

            lock (_typeCacheLock)
            {
                _typeCache[key] = result;
            }

            return result;
        }



        private MyraXamlElement CreateElement(
            ITextSnapshot snapshot,
            string text,
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

            var nameGroup =
                tagMatch.Groups["name"];

            var prefixGroup =
                tagMatch.Groups["prefix"];

            /*
             * Property element:
             *
             *     <Grid.ColumnsProportions>
             *
             * The lexical name is split into:
             *
             *     owner   = Grid
             *     member  = ColumnsProportions
             *
             * It is only a property element if:
             *
             *   1. there is no namespace prefix;
             *   2. the owner resolves to a type;
             *   3. the member is a property on the parent element's type.
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

                return new MyraXamlElement(snapshot, rawName, null)
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

            var nameSpan = new SnapshotSpan(
                        snapshot,
                        elementStart,
                        elementLength);
            var type = ResolveType(prefix, rawName, namespaces);

            return new MyraXamlElement(snapshot, rawName, prefix)
            {
                Type = type,
                IsPropertyElement = false,
                NameSpan = nameSpan,
                Parent = parent,
                TagSpan =
                    new SnapshotSpan(
                        snapshot,
                        tagStart,
                        Math.Max(0, tagEnd - tagStart + 1))
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

            var tagText =
                text.Substring(
                    tagStart,
                    length);

            foreach (Match match in AttributeRegex.Matches(tagText))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!match.Success)
                    continue;

                var prefixGroup =
                    match.Groups["prefix"];

                var ownerGroup =
                    match.Groups["owner"];

                var memberGroup =
                    match.Groups["member"];

                if (!ownerGroup.Success)
                    continue;

                var prefix =
                    prefixGroup.Success
                        ? prefixGroup.Value
                        : null;

                var owner =
                    ownerGroup.Value;

                var member =
                    memberGroup.Success
                        ? memberGroup.Value
                        : null;

                /*
                 * XML namespace declarations are attributes lexically,
                 * but are not Myra properties.
                 */
                if (string.Equals(
                        owner,
                        "xmlns",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                /*
                 * Attached property:
                 *
                 *     Grid.Column="1"
                 *
                 * The owner is the type declaring the attached
                 * property, not necessarily the current element.
                 */
                if (member != null)
                {
                    var ownerType =  ResolveType(prefix, owner, namespaces);

                    var ownerStart =
                        tagStart +
                        (prefixGroup.Success
                            ? prefixGroup.Index
                            : ownerGroup.Index);

                    var ownerLength =
                        prefixGroup.Success
                            ? prefixGroup.Length +
                              ownerGroup.Length -
                              prefixGroup.Length
                            : owner.Length;

                    // For a qualified owner, classify the entire
                    // "Grid" or "prefix:Grid" as the owner.
                    var actualOwnerStart =
                        tagStart +
                        ownerGroup.Index;

                    var memberStart =
                        tagStart +
                        memberGroup.Index;

                    var attr =
                        new MyraXamlAttribute(member)
                        { 
                            Prefix = prefix,
                            OwnerNameSpan =
                                new SnapshotSpan(
                                    snapshot,
                                    actualOwnerStart,
                                    owner.Length),
                            MemberNameSpan =
                                new SnapshotSpan(
                                    snapshot,
                                    memberStart,
                                    member.Length),
                            NameSpan =
                                new SnapshotSpan(
                                    snapshot,
                                    memberStart,
                                    member.Length),
                            OwnerType = ownerType
                        };

                    element.Attributes.Add(attr);

                    continue;
                }

                /*
                 * Ordinary property:
                 *
                 *     Width="100"
                 *     Text="Hello"
                 *
                 * The property must exist on the current element
                 * type.
                 */
                var propertyName =
                    owner;

                var valid =
                                    element.Type != null &&
                                    IsInstanceProperty(
                                        element.Type,
                                        propertyName);

                var attributeStart =
                    tagStart +
                    ownerGroup.Index;

                var attribute =
                    new MyraXamlAttribute(propertyName)
                    {
                        Prefix =
                            prefix,

                        NameSpan =
                            new SnapshotSpan(
                                snapshot,
                                attributeStart,
                                propertyName.Length),

                        OwnerType =
                            element.Type,

                        IsAttachedProperty =
                            false,

                        IsValid = valid
                    };

                element.Attributes.Add(
                    attribute);
            }
        }

        private bool IsPropertyElement(MyraXamlType parentType, MyraXamlType ownerType, string memberName)
        {
            /*
             * The owner in:
             *
             *     <Grid.ColumnsProportions>
             *
             * must describe the parent element's type.
             *
             * This permits base classes as well:
             *
             *     <SomeDerivedWidget.SomeBaseProperty>
             */
            if (!ownerType.IsAssignableFrom(parentType) &&
                !parentType.IsAssignableFrom(ownerType))
            {
                return false;
            }

            return IsInstanceProperty(
                parentType,
                memberName);
        }

        private static bool IsInstanceProperty(MyraXamlType type,string propertyName)
        {
            return type.Type.GetMembers(propertyName).OfType<IPropertySymbol>().Any(p => !p.IsStatic);
        }

        private static bool IsAttachedProperty(MyraXamlType ownerType,   string propertyName)
        {
            /*
             * XAML attached-property convention:
             *
             *     public static T GetColumn(...)
             *     public static void SetColumn(...)
             *
             * We accept either accessor, since some frameworks
             * expose only one side.
             */ 
            var getter = ownerType.Type.GetMembers("Get" + propertyName);

            if (getter != null)
            {
                return true;
            }

            var setter = ownerType.Type.GetMembers("Set" + propertyName);

            if (setter != null)
            {
                return true;
            }

            return false;
        }


        private static Dictionary<string, string> ParseNamespaces(
            string text)
        {
            var result =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

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

        private static bool IsSelfClosing(
            string text,
            int start,
            int end)
        {
            if (end <= start)
                return false;

            var i =
                end - 1;

            while (i > start &&
                   char.IsWhiteSpace(text[i]))
            {
                i--;
            }

            return text[i] == '/';
        }

        private static int FindTagEnd(
            string text,
            int start)
        {
            char quote = '\0';

            for (var i = start;
                 i < text.Length;
                 i++)
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
             * The user may currently be typing:
             *
             *     <Grid Width="100
             *
             * There is no '>' yet. Treat the rest of the
             * document as the current tag.
             */
            return text.Length - 1;
        }
    }
}
 