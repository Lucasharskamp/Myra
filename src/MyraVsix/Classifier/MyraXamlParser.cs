using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace Myra.Classifier
{
    internal sealed class MyraXamlParser
    {
        private const string MyraNamespace = "https://github.com/MyraUI/Myra";
        private const string ClrNamespacePrefix = "clr-namespace:";

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

        private static readonly Regex NamespaceRegex = new(
            """
            ^
            (?<namespace>[^;]+)
            (?:;\s*assembly=(?<assembly>.+))?
            $
            """,
            RegexOptions.Compiled |
            RegexOptions.IgnorePatternWhitespace);

        private readonly Dictionary<string, Type> _widgetTypes;

        private readonly Dictionary<string, Type?> _typeCache =
            new(StringComparer.Ordinal);

        private readonly object _typeCacheLock = new();

        public MyraXamlParser()
        {
            _widgetTypes = new Dictionary<string, Type>(
                StringComparer.Ordinal);

            var widgetAssembly =
                typeof(Myra.Graphics2D.UI.Widget).Assembly;

            foreach (var type in GetLoadableTypes(widgetAssembly))
            {
                if (!type.IsClass ||
                    type.IsAbstract ||
                    !typeof(Myra.Graphics2D.UI.Widget)
                        .IsAssignableFrom(type))
                {
                    continue;
                }

                if (!string.Equals(
                        type.Namespace,
                        "Myra.Graphics2D.UI",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (!_widgetTypes.ContainsKey(type.Name))
                {
                    _widgetTypes.Add(type.Name, type);
                }
            }
        }

        public MyraXamlDocumentModel Parse(
            ITextSnapshot snapshot,
            CancellationToken cancellationToken)
        {
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

                ParseAttributes(
                    snapshot,
                    text,
                    tagStart,
                    tagEnd,
                    element,
                    namespaces,
                    cancellationToken);

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
            if (prefix == null)
            {
                var dot = rawName.IndexOf('.');

                if (dot > 0 &&
                    dot < rawName.Length - 1
                    && parent?.Type != null)
                {
                    var ownerName = rawName.Substring(0, dot);
                    var memberName = rawName.Substring(dot + 1); 

                    var ownerType =
                        ResolveType(null, ownerName, namespaces);

                    if (ownerType != null && IsPropertyElement(parent.Type, ownerType, memberName))
                    {
                        var ownerStart =  nameGroup.Index;
                        var memberStart =  nameGroup.Index + dot +  1;

                        return new MyraXamlElement
                        {
                            Snapshot = snapshot,
                            Name = rawName,
                            Prefix = null,
                            Type = ownerType,

                            IsPropertyElement = true,

                            NameSpan = new SnapshotSpan(snapshot,  snapshot.Length > 0 ? ownerStart : 0,  rawName.Length),
                            OwnerNameSpan =  new SnapshotSpan(snapshot, ownerStart,  ownerName.Length),
                            MemberNameSpan = new SnapshotSpan(snapshot, memberStart, memberName.Length),
                            Parent = parent,
                            TagSpan = new SnapshotSpan(snapshot, tagStart, Math.Max(0,tagEnd - tagStart + 1))
                        };
                    }
                }
            }

            var type = ResolveType(prefix, rawName, namespaces);

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

            return new MyraXamlElement
            {
                Snapshot = snapshot,
                Name = rawName,
                Prefix = prefix,
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
                    var ownerType =
                        ResolveType(
                            prefix,
                            owner,
                            namespaces);

                    var isAttached =
                        ownerType != null &&
                        IsAttachedProperty(
                            ownerType,
                            member);

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
                        new MyraXamlAttribute
                        {
                            Name = member,
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
                            OwnerType = ownerType,
                            IsAttachedProperty = isAttached,
                            IsValid = isAttached
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
                    new MyraXamlAttribute
                    {
                        Name =
                            propertyName,

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

                        IsValid =
                            valid
                    };

                element.Attributes.Add(
                    attribute);
            }
        }

        private bool IsPropertyElement(
            Type parentType,
            Type ownerType,
            string memberName)
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

        private static bool IsInstanceProperty(
            Type type,
            string propertyName)
        {
            return type.GetProperty(
                       propertyName,
                       BindingFlags.Instance |
                       BindingFlags.Public |
                       BindingFlags.FlattenHierarchy)
                   != null;
        }

        private static bool IsAttachedProperty(
            Type ownerType,
            string propertyName)
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
            var flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy;

            var getter =ownerType.GetMethod("Get" + propertyName, flags);

            if (getter != null && getter.ReturnType != typeof(void))
            {
                return true;
            }

            var setter = ownerType.GetMethod("Set" + propertyName, flags);

            if (setter != null &&
                setter.ReturnType == typeof(void))
            {
                return true;
            }

            return false;
        }

        private Type? ResolveType(string? prefix, string name, IReadOnlyDictionary<string, string> namespaces)
        {
            var key = (prefix ?? string.Empty) + ":" + name;

            lock (_typeCacheLock)
            {
                if (_typeCache.TryGetValue(
                        key,
                        out var cached))
                {
                    return cached;
                }
            }

            Type? result;

            if (prefix == null)
            {
                result =
                    ResolveUnprefixedType(name);
            }
            else if (namespaces.TryGetValue(prefix, out var uri))
            {
                result = ResolvePrefixedType(uri, name);
            }
            else
            {
                result = null;
            }

            lock (_typeCacheLock)
            {
                _typeCache[key] = result;
            }

            return result;
        }

        private Type? ResolveUnprefixedType(string name)
        {
            if (_widgetTypes.TryGetValue(name, out var type))
            {
                return type;
            }

            return null;
        }

        private static Type? ResolvePrefixedType(
            string uri,
            string name)
        {
            /*
             * Myra's own namespace.
             */
            if (string.Equals(uri, MyraNamespace,  StringComparison.Ordinal))
            {
                return ResolveMyraType(name);
            }

            /*
             * CLR namespace:
             *
             *     xmlns:local="clr-namespace:MyGame"
             *
             * or:
             *
             *     xmlns:local=
             *         "clr-namespace:MyGame;assembly=MyGame"
             */
            if (!uri.StartsWith(
                    ClrNamespacePrefix,
                    StringComparison.Ordinal))
            {
                return null;
            }

            var value =
                uri.Substring(
                    ClrNamespacePrefix.Length);

            var match =
                NamespaceRegex.Match(value);

            if (!match.Success)
                return null;

            var clrNamespace =
                match.Groups["namespace"].Value;

            var assemblyName =
                match.Groups["assembly"].Success
                    ? match.Groups["assembly"].Value
                    : null;

            IEnumerable<Assembly> assemblies = !string.IsNullOrWhiteSpace(assemblyName)
                ? AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Where(a =>
                            string.Equals(
                                a.GetName().Name,
                                assemblyName,
                                StringComparison.Ordinal))
                : AppDomain.CurrentDomain
                        .GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in GetLoadableTypes(assembly))
                {
                    if (!string.Equals(
                            type.Namespace,
                            clrNamespace,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!string.Equals(
                            type.Name,
                            name,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    return type;
                }
            }

            return null;
        }

        private static Type? ResolveMyraType(
            string name)
        {
            var assembly =
                typeof(Myra.Graphics2D.UI.Widget)
                    .Assembly;

            foreach (var type in GetLoadableTypes(assembly))
            {
                if (string.Equals(
                        type.Namespace,
                        "Myra.Graphics2D.UI",
                        StringComparison.Ordinal) &&
                    string.Equals(
                        type.Name,
                        name,
                        StringComparison.Ordinal))
                {
                    return type;
                }
            }

            return null;
        }

        private static IEnumerable<Type> GetLoadableTypes(
            Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types
                    .Where(t => t != null)!
                    .Cast<Type>();
            }
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
 