using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Myra.Classifier
{

    internal sealed class MyraXamlClassifier : IClassifier
    {
        private static readonly Regex XmlnsRegex = new(
            """
            \bxmlns
            (?:\:(?<prefix>[A-Za-z_][A-Za-z0-9_.-]*))?
            \s*=\s*
            (?:"(?<uri>[^"]*)"|'(?<uri>[^']*)')
            """,
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex ElementRegex = new(
            """
            <
            \s*
            (?<closing>/)?
            (?:
                (?<prefix>[A-Za-z_][A-Za-z0-9_.-]*):
            )?
            (?<name>[A-Za-z_][A-Za-z0-9_.-]*)
            """,
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

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
            """,
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private readonly IClassificationType _elementClassification;
        private readonly IClassificationType _attributeClassification;

        private readonly Assembly _myraAssembly;
        private readonly Type _widgetType;

        private Dictionary<string, Type?> _typeCache;

        public MyraXamlClassifier(
            IClassificationType elementClassification,
            IClassificationType attributeClassification)
        {
            _elementClassification = elementClassification;
            _attributeClassification = attributeClassification;

            _widgetType = typeof(Myra.Graphics2D.UI.Widget);
            _myraAssembly = _widgetType.Assembly;

            _typeCache = new Dictionary<string, Type?>(
                StringComparer.Ordinal);
            var widgetTypes = _myraAssembly.GetTypes().Where(t => t.Namespace != null && t.Namespace.Equals("Myra.Graphics2D.UI"));
            foreach (var widgetType in widgetTypes)
            {
                _typeCache.Add(widgetType.Name, widgetType);
            }
        }

        public event EventHandler<ClassificationChangedEventArgs>? ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;
            var text = snapshot.GetText(span);

            var namespaces = GetNamespaces(text);

            var result = new List<ClassificationSpan>();

            foreach (Match match in ElementRegex.Matches(text))
            {
                if (!match.Success)
                    continue;

                var nameGroup = match.Groups["name"].Success
                    ? match.Groups["name"]
                    : match.Groups["name2"];

                var prefixGroup = match.Groups["prefix"];

                if (!nameGroup.Success)
                    continue;

                var prefix = prefixGroup.Success
                    ? prefixGroup.Value
                    : null;

                var name = nameGroup.Value; 

                var dot = name.IndexOf('.');

                if (dot >= 0 && prefix == null)
                {
                    var owner = name.Substring(0, dot);
                    var member = name.Substring(dot);

                    if (IsWidget(owner))
                    {
                        ClassifyQualifiedName(
                            snapshot,
                            span,
                            span.Start + nameGroup.Index,
                            owner,
                            member,
                            result);
                    }

                    // It is a property element, not an ordinary element.
                    continue;
                }

                if (!IsUsableElement(prefix, name, namespaces))
                    continue;

                // Classify the element itself.
                var start = nameGroup.Index;

                // Include the prefix in the classification.
                if (prefix != null)
                    start = prefixGroup.Index;

                var length =
                    nameGroup.Index +
                    nameGroup.Length -
                    start;

                var classificationSpan = new SnapshotSpan(
                    snapshot,
                    span.Start + start,
                    length);

                if (classificationSpan.IntersectsWith(span))
                {
                    result.Add(
                        new ClassificationSpan(
                            classificationSpan,
                            _elementClassification));
                }

                // Find the end of this start tag so that attributes can be
                // classified only inside this element's opening tag.
                var tagEnd = FindTagEnd(
                    text,
                    match.Index);

                ClassifyAttributes(
                    text,
                    snapshot,
                    span,
                    match.Index,
                    tagEnd,
                    namespaces,
                    result);
            }

            return result;
        }

        private bool IsUsableElement(
            string? prefix,
            string name,
            Dictionary<string, string> namespaces)
        {
            if (prefix != null)
            {
                // Any explicitly declared XAML namespace is a namespace
                // containing potentially usable elements.
                //
                // e.g.:
                //   xmlns:local="clr-namespace:MyGame"
                //   xmlns:controls="clr-namespace:MyControls"
                //
                // We deliberately don't require the actual CLR type to be
                // loadable here; the user's project assembly may not be
                // loaded into the VSIX process.
                return namespaces.ContainsKey(prefix);
            }

            // Unprefixed elements are interesting when they are Myra Widgets.
            return IsWidget(name);
        }
         

        private bool IsWidget(string name)
        {
            return _typeCache.ContainsKey(name);
        }

        private static Dictionary<string, string> GetNamespaces(string text)
        {
            var result = new Dictionary<string, string>(
                StringComparer.Ordinal);

            foreach (Match match in XmlnsRegex.Matches(text))
            {
                var prefix = match.Groups["prefix"].Success
                    ? match.Groups["prefix"].Value
                    : string.Empty;

                var uri = match.Groups["uri"].Value;

                result[prefix] = uri;
            }

            return result;
        }

        private void ClassifyAttributes(
             string text,
             ITextSnapshot snapshot,
             SnapshotSpan requestedSpan,
             int tagStart,
             int tagEnd,
             Dictionary<string, string> namespaces,
             List<ClassificationSpan> result)
        {

            var tagText = text.Substring(
                tagStart,
                tagEnd - tagStart + 1);

            foreach (Match match in AttributeRegex.Matches(tagText))
            {
                var prefixGroup = match.Groups["prefix"];
                var ownerGroup = match.Groups["owner"];
                var memberGroup = match.Groups["member"];

                if (!ownerGroup.Success)
                    continue;

                var prefix = prefixGroup.Success
                    ? prefixGroup.Value
                    : null;

                var owner = ownerGroup.Value;

                if (memberGroup.Success)
                {
                    var member = memberGroup.Value;

                    if (!IsWidget(owner))
                        continue;

                    var qualifiedStart =
                        tagStart +
                        match.Index +
                        (prefixGroup.Success
                            ? prefixGroup.Index
                            : ownerGroup.Index);

                    var ownerStart = requestedSpan.Start + tagStart + ownerGroup.Index;

                    var ownerSpan = new SnapshotSpan(
                        snapshot,
                        ownerStart,
                        owner.Length);

                        result.Add(
                            new ClassificationSpan(
                                ownerSpan,
                                _elementClassification));

                    var memberStart =  requestedSpan.Start + tagStart + memberGroup.Index;

                    var memberSpan = new SnapshotSpan(
                        snapshot,
                        memberStart,
                        member.Length);

                        result.Add(
                            new ClassificationSpan(
                                memberSpan,
                                _attributeClassification));

                    continue;
                }

                var attributeStart =
                    requestedSpan.Start +
                    tagStart +
                    ownerGroup.Index;

                var attributeSpan = new SnapshotSpan(
                    snapshot,
                    attributeStart,
                    owner.Length);

                if (attributeSpan.IntersectsWith(requestedSpan))
                {
                    result.Add(
                        new ClassificationSpan(
                            attributeSpan,
                            _attributeClassification));
                }
            }
        }

        private void ClassifyQualifiedName(
            ITextSnapshot snapshot,
            SnapshotSpan requestedSpan,
            int absoluteStart,
            string owner,
            string member,
            List<ClassificationSpan> result)
        {
            var ownerLength = owner.Length;

            var ownerSpan = new SnapshotSpan(
                snapshot,
                absoluteStart,
                ownerLength);

            if (ownerSpan.IntersectsWith(requestedSpan))
            {
                result.Add(
                    new ClassificationSpan(
                        ownerSpan,
                        _elementClassification));
            }

            var memberStart =
                absoluteStart +
                owner.Length +
                1; // '.'

            var memberSpan = new SnapshotSpan(
                snapshot,
                memberStart,
                member.Length);

            if (memberSpan.IntersectsWith(requestedSpan))
            {
                result.Add(
                    new ClassificationSpan(
                        memberSpan,
                        _attributeClassification));
            }
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

            return text.Length - 1;
        }
    }
}
